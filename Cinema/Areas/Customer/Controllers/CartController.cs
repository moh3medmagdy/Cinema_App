
using Cinema.Models;
using Cinema.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace Cinema.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private const string PromoKey = "PromoCode";

        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ICartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _userManager = userManager;
        }

        private string UserId => _userManager.GetUserId(User)!;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync(
                UserId,
                HttpContext.Session.GetString(PromoKey)
            );

            if (cart.PromoCode == null)
                HttpContext.Session.Remove(PromoKey);

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int movieId, int count = 1)
        {
            await _cartService.AddAsync(UserId, movieId, count);

            TempData["Success"] = "Added to cart.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int movieId, int count)
        {
            await _cartService.UpdateAsync(UserId, movieId, count);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int movieId)
        {
            await _cartService.RemoveAsync(UserId, movieId);

            TempData["Success"] = "Item removed.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromo(string code)
        {
            var promo = await _cartService.GetValidPromoAsync(code);

            if (promo == null)
            {
                TempData["Error"] = "Invalid or expired promo code.";
            }
            else
            {
                HttpContext.Session.SetString(PromoKey, promo.Code);

                TempData["Success"] =
                    $"Promo applied: {promo.DiscountPercent}% off.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay()
        {
            var cart = await _cartService.GetCartAsync(
                UserId,
                HttpContext.Session.GetString(PromoKey)
            );

            if (!cart.Items.Any() || cart.Total <= 0)
            {
                TempData["Error"] = "Your cart is empty or invalid.";

                return RedirectToAction(nameof(Index));
            }

            var options = new SessionCreateOptions
            {
                AllowedPaymentMethodTypes = new List<string>
                {
                    "card"
                },

                LineItems = new List<SessionLineItemOptions>(),

                Mode = "payment",

                SuccessUrl =
                    $"{Request.Scheme}://{Request.Host}/Customer/Cart/Success",

                CancelUrl =
                    $"{Request.Scheme}://{Request.Host}/Customer/Cart/Checkout"
            };

            // Calculate discounted total
            decimal discountPercent = cart.DiscountPercent;

            if (discountPercent < 0 || discountPercent > 100)
                discountPercent = 0;

            foreach (var item in cart.Items)
            {
                decimal discountedPrice =
                    item.UnitPrice * (1 - discountPercent / 100m);

                long unitAmount = (long)Math.Round(
                    discountedPrice * 100,
                    MidpointRounding.AwayFromZero
                );

                if (unitAmount <= 0)
                    continue;

                options.LineItems.Add(
                    new SessionLineItemOptions
                    {
                        PriceData =
                            new SessionLineItemPriceDataOptions
                            {
                                Currency = "egp",

                                ProductData =
                                    new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = item.Name
                                    },

                                UnitAmount = unitAmount
                            },

                        Quantity = item.Count
                    }
                );
            }

            if (!options.LineItems.Any())
            {
                TempData["Error"] = "Invalid payment amount.";

                return RedirectToAction(nameof(Index));
            }

            var service = new SessionService();

            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePromo()
        {
            HttpContext.Session.Remove(PromoKey);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = await _cartService.GetCartAsync(
                UserId,
                HttpContext.Session.GetString(PromoKey)
            );

            if (!cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";

                return RedirectToAction(nameof(Index));
            }

            return View(cart);
        }
    }
}
