using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Elbrus.Models;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public string? InventoryName { get; set; }

    public string? InventoryModel { get; set; }

    public int? RentalCostPerHour { get; set; }

    public string? Photo { get; set; }

    public Bitmap GetPhoto
    {
        get
        {
            if (!string.IsNullOrEmpty(Photo))
            {
                return new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/" + Photo);
            }
            else
            {
                return new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/inv/no.png");
            }
        }
    }

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
