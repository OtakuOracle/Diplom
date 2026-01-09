using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class OrderInventory
{
    public int Id { get; set; }

    public int InventoryId { get; set; }

    public int OrderServiceId { get; set; }

    public string Model { get; set; } = null!;

    public string Size { get; set; } = null!;

    public virtual Inventory Inventory { get; set; } = null!;

    public virtual OrderService OrderService { get; set; } = null!;
}
