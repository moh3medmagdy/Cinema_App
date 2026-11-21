using Cinema.Data;
using Cinema.Models;
using Cinema.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Services
{
    public interface ICartService
    {
        Task<CartVM> GetCartAsync(string userId, string? promoCode);
        Task AddAsync(string userId, int movieId, int count);
        Task UpdateAsync(string userId, int movieId, int count);
        Task RemoveAsync(string userId, int movieId);
        Task ClearAsync(string userId);
        Task<PromoCode?> GetValidPromoAsync(string? code);
    }

    public class CartService : ICartService
    {
        private const int MaxPerItem = 10;
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartVM> GetCartAsync(string userId, string? promoCode)
        {
            var items = await _context.CartItems
                .Where(c => c.ApplicationUserId == userId)
                .Include(c => c.Movie)
                .Select(c => new CartItemVM
                {
                    MovieId = c.MovieId,
                    Name = c.Movie.Name,
                    Image = c.Movie.MainImg,
                    UnitPrice = c.Movie.Price,
                    Count = c.Count
                })
                .ToListAsync();

            var cart = new CartVM { Items = items };

            var promo = await GetValidPromoAsync(promoCode);
            if (promo != null)
            {
                cart.PromoCode = promo.Code;
                cart.DiscountPercent = promo.DiscountPercent;
            }

            return cart;
        }

        public async Task AddAsync(string userId, int movieId, int count)
        {
            if (count <= 0) count = 1;

            var movieExists = await _context.Movies.AnyAsync(m => m.Id == movieId);
            if (!movieExists) return;

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.MovieId == movieId);

            if (item == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    ApplicationUserId = userId,
                    MovieId = movieId,
                    Count = Math.Min(count, MaxPerItem)
                });
            }
            else
            {
                item.Count = Math.Min(item.Count + count, MaxPerItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(string userId, int movieId, int count)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.MovieId == movieId);
            if (item == null) return;

            if (count <= 0)
                _context.CartItems.Remove(item);
            else
                item.Count = Math.Min(count, MaxPerItem);

            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(string userId, int movieId)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.MovieId == movieId);
            if (item == null) return;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task ClearAsync(string userId)
        {
            var items = _context.CartItems.Where(c => c.ApplicationUserId == userId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task<PromoCode?> GetValidPromoAsync(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var normalized = code.Trim().ToUpper();

            return await _context.PromoCodes.FirstOrDefaultAsync(p =>
                p.Code.ToUpper() == normalized &&
                p.IsActive &&
                (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));
        }
    }
}