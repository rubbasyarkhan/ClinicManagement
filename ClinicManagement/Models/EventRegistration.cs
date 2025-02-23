using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class EventRegistration
{
    public int RegistrationId { get; set; }

    public int? EventId { get; set; }

    public int? UserId { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public virtual EducationEvent? Event { get; set; }

    public virtual User? User { get; set; }
}
