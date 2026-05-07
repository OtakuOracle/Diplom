using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class OrderStatus
{
    public int OrderStatusId { get; set; }

    public string? OrderStatusName { get; set; }

    public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
