using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public class InventoryItem
{
    public int InventoryItemId { get; set; }

    public int InventoryId { get; set; }

    public string? InventoryNumber { get; set; }

    public string? Size { get; set; }

    public int? InventoryStatusId { get; set; }

    public virtual Inventory? Inventory { get; set; }

    public virtual InventoryStatus? InventoryStatus { get; set; }

    public virtual ICollection<OrderInventory> OrderInventories { get; set; } = new List<OrderInventory>();
}

