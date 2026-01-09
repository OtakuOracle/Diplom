using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string ServiceCode { get; set; } = null!;

    public int? CostPerHour { get; set; }

    public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
