using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string? OrderCode { get; set; }

    public DateOnly? DateCreate { get; set; }

    public TimeOnly? TimeCreate { get; set; }

    public int? ClientId { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Client? Client { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
