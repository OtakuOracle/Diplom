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
                                .Include(x => x.InventoryStatus)
                                .ToList();
        switch (Sort.SelectedIndex)
        {

            case 0: // Сортировка по возрастанию цены
                allInventories = allInventories.OrderBy(x => x.RentalCostPerHour).ToList();
                break;
            case 1: // Сортировка по убыванию цены
                allInventories = allInventories.OrderByDescending(x => x.RentalCostPerHour).ToList();
                break;
            default: // Если не выбрано, сортируем по возрастанию
                allInventories = allInventories.OrderBy(x => x.RentalCostPerHour).ToList();
                break;
        }

        if (Filter.SelectedItem != null && Filter.SelectedItem.ToString() != "Все статусы")
        {
            allInventories = allInventories.Where(x => x.InventoryStatus.InventoryStatusName == Filter.SelectedItem.ToString()).ToList();
        }


        if (SearchBox != null && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var searchTerm = SearchBox.Text.ToLower();
            allInventories = allInventories.Where(x =>
                (x.InventoryName != null && !string.IsNullOrWhiteSpace(x.InventoryName) && x.InventoryName.ToLower().Contains(searchTerm)) ||
                (x.InventoryNumber != null && !string.IsNullOrWhiteSpace(x.InventoryNumber) && x.InventoryNumber.ToLower().Contains(searchTerm)) ||
                (x.InventoryModel != null && !string.IsNullOrWhiteSpace(x.InventoryModel) && x.InventoryModel.ToLower().Contains(searchTerm)) ||
                (x.InventorySize != null && !string.IsNullOrWhiteSpace(x.InventorySize) && x.InventorySize.ToLower().Contains(searchTerm))

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