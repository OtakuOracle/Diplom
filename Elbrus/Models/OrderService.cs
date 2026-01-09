using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class OrderService
{
    public int OrderServiceId { get; set; }

    public int OrderId { get; set; }

    public int ServiceId { get; set; }

    public int RentTime { get; set; }

    public string? Status { get; set; }

    public TimeOnly? TimeIn { get; set; }

    public TimeOnly? TimeOut { get; set; }

    public DateOnly? Date { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderInventory> OrderInventories { get; set; } = new List<OrderInventory>();

    public virtual Service Service { get; set; } = null!;
}
