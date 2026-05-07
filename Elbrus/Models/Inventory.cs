using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Elbrus.Models;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public string? InventoryName { get; set; }

    public string? InventoryNumber { get; set; }

    public string? InventoryModel { get; set; }

    public string? InventorySize { get; set; }

    public int? RentalCostPerHour { get; set; }

    public string? Photo { get; set; }

    public Bitmap GetPhoto
    {
        get
        {
            if (Photo != null && Photo != "")
            {
                return new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/" + Photo);
            }
            else
            {
                return new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/inv/no.png");
            }
        }
    }

    public int? InventoryStatusId { get; set; }

    public virtual InventoryStatus? InventoryStatus { get; set; }

    public virtual ICollection<OrderInventory> OrderInventories { get; set; } = new List<OrderInventory>();
}
