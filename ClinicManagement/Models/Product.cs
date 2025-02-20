using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? Manufacturer { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
