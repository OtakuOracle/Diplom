using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class EnterStatus
{
    public int EnterStatusId { get; set; }

    public string? EnterStatusName { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
