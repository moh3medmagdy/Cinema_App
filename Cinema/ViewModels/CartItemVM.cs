namespace Cinema.ViewModels
{
    public class CartItemVM
    {
        public int MovieId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Image { get; set; }
        public decimal UnitPrice { get; set; }
        public int Count { get; set; }
        public decimal LineTotal => UnitPrice * Count;
    }

    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new();
        public string? PromoCode { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal SubTotal => Items.Sum(i => i.LineTotal);
        public decimal DiscountAmount => Math.Round(SubTotal * DiscountPercent / 100, 2);
        public decimal Total => SubTotal - DiscountAmount;
        public int ItemsCount => Items.Sum(i => i.Count);
    }
}