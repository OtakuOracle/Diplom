using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class InventoryStatus
{
    public int InventoryStatusId { get; set; }

    public string? InventoryStatusName { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
