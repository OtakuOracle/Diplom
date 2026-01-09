using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string? Fio { get; set; }

    public int? ClientCode { get; set; }

    public string? Passport { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public int? Role { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role? RoleNavigation { get; set; }
}
