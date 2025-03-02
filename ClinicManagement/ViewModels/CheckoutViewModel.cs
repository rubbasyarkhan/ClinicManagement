using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ClinicManagement.Models; // Add this to reference Cart model

namespace ClinicManagement.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Shipping address cannot be longer than 255 characters.")]
        public string ShippingAddress { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash on Delivery"; // Default payment method

        [Required]
        public List<OrderDetailViewModel> OrderDetails { get; set; }

        // Add this if you need to display cart items during checkout
        public List<Cart> CartItems { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
