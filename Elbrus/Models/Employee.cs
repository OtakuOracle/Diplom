using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int? RoleId { get; set; }

    public string? FullName { get; set; }

    public string? Login { get; set; }

    public string? Passwrd { get; set; }

    public DateTime? LastEnter { get; set; }

    public string? Photo { get; set; }

    public int? EnterStatus { get; set; }

    public virtual EnterStatus? EnterStatusNavigation { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role? Role { get; set; }
}
