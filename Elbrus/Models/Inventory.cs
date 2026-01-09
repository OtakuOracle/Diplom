using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public partial class Inventory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Number { get; set; }

    public string? Model { get; set; }

    public string? Size { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<OrderInventory> OrderInventories { get; set; } = new List<OrderInventory>();
}
