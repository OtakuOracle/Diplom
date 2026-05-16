using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class InventoryStatus
{
    public int InventoryStatusId { get; set; }

    public string? InventoryStatusName { get; set; }

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
