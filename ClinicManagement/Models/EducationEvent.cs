using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class EducationEvent
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    public TimeOnly EventTime { get; set; }

    public string Speaker { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
}
