using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class OrderInventory
{
    public int OrderInventoryId { get; set; }

    public int? InventoryId { get; set; }

    public int? OrderServiceId { get; set; }

    public int? RentTime { get; set; }

    public virtual Inventory? Inventory { get; set; }

    public virtual OrderService? OrderService { get; set; }
}
