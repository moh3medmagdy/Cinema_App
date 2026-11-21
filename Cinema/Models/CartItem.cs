using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Cinema.Models
{
    [Index(nameof(ApplicationUserId), nameof(MovieId), IsUnique = true)]
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public int Count { get; set; }
    }

    public class PromoCode
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Range(1, 100)]
        public decimal DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
    }
}