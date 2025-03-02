namespace ClinicManagement.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }  // If needed
        public decimal Price { get; set; }      // If needed
        public int Quantity { get; set; }
    }
}
