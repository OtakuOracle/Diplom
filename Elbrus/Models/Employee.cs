using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Employee
{
    public int Id { get; set; }

    public int Role { get; set; }

    public string? Fio { get; set; }

    public string? Login { get; set; }

    public string? Password { get; set; }

    public DateTime? LastEnter { get; set; }

    public string? EnterStatus { get; set; }

    public string? Photo { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role RoleNavigation { get; set; } = null!;
}
