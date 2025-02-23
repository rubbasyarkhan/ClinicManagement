using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public string Comment { get; set; } = null!;

    public int Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
