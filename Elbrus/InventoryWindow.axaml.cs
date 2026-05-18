using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Elbrus.Models;
using Microsoft.EntityFrameworkCore;

namespace Elbrus;

public partial class InventoryWindow : Window
{
    public InventoryWindow()
    {
        InitializeComponent();
        LoadBox(); 
        Get(); 
    }

    private void Get()
    {
        using var context = new DiplomContext();

        var allInventories = context.Inventories
            .Include(x => x.InventoryItems)
            .ThenInclude(i => i.InventoryStatus)
            .ToList();

        switch (Sort.SelectedIndex)
        {
            case 0:
                allInventories = allInventories.OrderBy(x => x.RentalCostPerHour).ToList();
                break;
            case 1:
                allInventories = allInventories.OrderByDescending(x => x.RentalCostPerHour).ToList();
                break;
            default:
                allInventories = allInventories.OrderBy(x => x.RentalCostPerHour).ToList();
                break;
        }

        if (Filter.SelectedItem != null && Filter.SelectedItem.ToString() != "Все статусы")
        {
            allInventories = allInventories
                .Where(x => x.InventoryItems.Any(i =>
                    i.InventoryStatus.InventoryStatusName == Filter.SelectedItem.ToString()))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var search = SearchBox.Text.ToLower();

            allInventories = allInventories.Where(x =>
                (x.InventoryName != null && x.InventoryName.ToLower().Contains(search)) ||
                (x.InventoryModel != null && x.InventoryModel.ToLower().Contains(search)) ||
                x.InventoryItems.Any(i =>
                    (i.InventoryNumber != null && i.InventoryNumber.ToLower().Contains(search)) ||
                    (i.Size != null && i.Size.ToLower().Contains(search))
                )
            ).ToList();
        }

        InventoriesBox.ItemsSource = allInventories;
    }

    private void SearchBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        
        Get(); 
    }

    private void Sort_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Get(); 
    }

    private void Filter_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Get(); 
    }


    private void LoadBox() 
    {
        using var context = new DiplomContext();

        var inv = context.InventoryStatuses.Select(x => x.InventoryStatusName).ToList();

        inv.Add("Все статусы");

        Filter.ItemsSource = inv.OrderByDescending(x => x == "Все статусы");

        Filter.SelectedIndex = 0;


    }
    private async void Back_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

 

    private void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var addedit = new AddEditInventory();
        addedit.Show();
        this.Close();
    }

    private void InventoriesBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) 
    {
        if (InventoriesBox.SelectedItem is Inventory inventory)
        {
            var addedit = new AddEditInventory(inventory);
            addedit.Show();
            this.Close();
        }
    }

}