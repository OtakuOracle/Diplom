using System;
using System.Collections.Generic;

namespace Elbrus.Models;

public class OrderItem
{
    public string Client { get; set; }
    public string Employee { get; set; }
    public string Service { get; set; }
    public string Inventory { get; set; }
    public int TotalPrice { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? TimeStart { get; set; }
    public TimeOnly? TimeEnd { get; set; }
    public string Size { get; set; }

}
