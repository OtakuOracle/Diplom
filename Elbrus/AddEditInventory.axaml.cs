using System;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Elbrus.Helpers;
using Elbrus.Models;
using MsBox.Avalonia;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;


namespace Elbrus;

public partial class AddEditInventory : Window
{
    public ObservableCollection<InventoryItem> InventoryItemsList { get; set; } = new();
    private Inventory _inventory;
    private string ImageName;
    private string _currentPhotoPath;

    /// <summary>
    /// добавление
    /// </summary>
    public AddEditInventory()
    {
        InitializeComponent();
        SizesList.ItemsSource = InventoryItemsList;

        _inventory = new Inventory(); 
        DataContext = _inventory;

        LoadStatuses(); 

        AddBut.IsVisible = true;
        EditBut.IsVisible = false;
        DeleteBut.IsVisible = false;
    }


    /// <summary>
    /// редактирование
    /// </summary>
    /// <param name="inventory"></param>

    public AddEditInventory(Inventory inventory)
    {
        InitializeComponent();
        SizesList.ItemsSource = InventoryItemsList;

        _inventory = inventory;
        DataContext = _inventory;

        LoadStatuses();

        AddBut.IsVisible = false;
        EditBut.IsVisible = true;
        DeleteBut.IsVisible = true;

        LoadInventoryItems(_inventory.InventoryId);

        if (_inventory.GetPhoto != null)
        {
            ImageBox.Source = _inventory.GetPhoto;
        }
    }





    private bool ValidateInventory(Inventory i)
    {
        if (i.RentalCostPerHour.HasValue && i.RentalCostPerHour < 0)
        {
            var errorPrice = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                "Цена не должна быть отрицательной",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);
            errorPrice.ShowAsync();
            return false;
        }

  

        return true;
    }


    private async void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            using var context = new DiplomContext();
            var newInventory = DataContext as Inventory;

            if (newInventory == null)
            {
                newInventory = new Inventory();
                DataContext = newInventory;
            }

            if (string.IsNullOrWhiteSpace(newInventory.InventoryName) ||
                string.IsNullOrWhiteSpace(newInventory.InventoryModel) ||
                newInventory.RentalCostPerHour == null)
            {
                var validationError = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Все поля должны быть заполнены",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await validationError.ShowAsync();
                return;
            }

            var selectedStatusObject = InventoryStatus.SelectedItem as InventoryStatus;

            if (selectedStatusObject == null)
            {
                var error = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Пожалуйста, выберите статус инвентаря",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await error.ShowAsync();
                return;
            }

            if (!string.IsNullOrEmpty(ImageName))
                newInventory.Photo = "inv/" + ImageName;

            context.Inventories.Add(newInventory);
            await context.SaveChangesAsync();

            foreach (var item in InventoryItemsList)   // список всех добавленных размеров/номеров
            {
                var newItem = new InventoryItem
                {
                    InventoryId = newInventory.InventoryId,
                    InventoryNumber = item.InventoryNumber,
                    Size = item.Size,
                    InventoryStatusId = selectedStatusObject.InventoryStatusId
                };

                context.InventoryItems.Add(newItem);
            }

            await context.SaveChangesAsync();

            var nice = MessageBoxManager.GetMessageBoxStandard(
                "Успех",
                "Инвентарь создан",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Success);

            await nice.ShowAsync();

            var inventoryWindow = new InventoryWindow();
            inventoryWindow.Show();
            this.Close();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var error = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                ex.InnerException?.Message ?? ex.Message,
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);

            await error.ShowAsync();
        }
    }

    private void AddSize_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InventoryNumberBox.Text) ||
            string.IsNullOrWhiteSpace(InventorySizeBox.Text))
            return;

        InventoryItemsList.Add(new InventoryItem
        {
            InventoryNumber = InventoryNumberBox.Text,
            Size = InventorySizeBox.Text
        });

        InventoryNumberBox.Text = "";
        InventorySizeBox.Text = "";
    }






    private async void AddImage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Добавить изображение",
            FileTypeChoices = new[]
            {
                FilePickerFileTypes.All
            }
        });

        if (file != null)
        {
            ImageBox.Source = new Bitmap(file.Path.LocalPath);
            ImageName = Guid.NewGuid().ToString() + ".png";
            var targetPath = AppDomain.CurrentDomain.BaseDirectory + "/inv/" + ImageName;
            File.Copy(file.Path.LocalPath, targetPath);

        }
    }


    private async void Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var confirmationMessage = MessageBoxManager.GetMessageBoxStandard(
            "Подтверждение удаления", 
            "Вы уверены, что хотите удалить этот инвентарь?", 
            MsBox.Avalonia.Enums.ButtonEnum.YesNo, 
            MsBox.Avalonia.Enums.Icon.Question 
        );

        var result = await confirmationMessage.ShowAsync();
        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            using var context = new DiplomContext();

            var inventoryId = _inventory.InventoryId; 

            var inventoryToDelete = context.Inventories.FirstOrDefault(x => x.InventoryId == inventoryId);

            if (inventoryToDelete != null)
            {
                context.Remove(inventoryToDelete);
                await context.SaveChangesAsync(); 

                var successMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Успех",
                    "Инвентарь удален",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Success
                );
                await successMessage.ShowAsync();

                var inventoryWindow = new InventoryWindow();
                inventoryWindow.Show();
                this.Close();
            }
            else
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Инвентарь не найден",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error
                );
                await errorMessage.ShowAsync();
            }
        }
        
    }


    private void Back_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var inventoryWindow = new InventoryWindow();
        inventoryWindow.Show();
        this.Close();
    }

    private void LoadStatuses()
    {
        using var context = new DiplomContext();
        var allStatuses = context.InventoryStatuses.ToList();

        InventoryStatus.ItemsSource = allStatuses;

        var item = context.InventoryItems.FirstOrDefault(x => x.InventoryId == _inventory.InventoryId);

        if (item != null && item.InventoryStatusId != null)
        {
            InventoryStatus.SelectedItem = allStatuses
                .FirstOrDefault(x => x.InventoryStatusId == item.InventoryStatusId);
        }
    }


    private async void Edit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        using var context = new DiplomContext();

        try
        {
            var inventory = DataContext as Inventory;

            if (inventory == null)
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Не удалось получить данные инвентаря",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(inventory.InventoryName) ||
                string.IsNullOrWhiteSpace(inventory.InventoryModel) ||
                inventory.RentalCostPerHour == null ||
                !InventoryItemsList.Any())
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Все поля и хотя бы один размер должны быть заполнены",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }

            var selectedStatus = InventoryStatus.SelectedItem as InventoryStatus;

            if (selectedStatus == null)
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Выберите статус",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }

            var attachedInventory = await context.Inventories
                .FirstOrDefaultAsync(x => x.InventoryId == inventory.InventoryId);

            if (attachedInventory == null)
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Инвентарь не найден",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }

            attachedInventory.InventoryName = inventory.InventoryName;
            attachedInventory.InventoryModel = inventory.InventoryModel;
            attachedInventory.RentalCostPerHour = inventory.RentalCostPerHour;

            if (!string.IsNullOrEmpty(ImageName))
            {
                attachedInventory.Photo = "inv/" + ImageName;
            }

            var oldItems = context.InventoryItems
                .Where(x => x.InventoryId == inventory.InventoryId)
                .ToList();

            context.InventoryItems.RemoveRange(oldItems);

            var newItems = InventoryItemsList.Select(item => new InventoryItem
            {
                InventoryId = inventory.InventoryId,
                InventoryNumber = item.InventoryNumber,
                Size = item.Size,
                InventoryStatusId = selectedStatus.InventoryStatusId
            });

            await context.InventoryItems.AddRangeAsync(newItems);

            await context.SaveChangesAsync();

            await MessageBoxManager.GetMessageBoxStandard(
                "Успех",
                "Инвентарь успешно обновлён",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Success).ShowAsync();
            
            var inventoryWindow = new InventoryWindow();
            inventoryWindow.Show();

            this.Close();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                ex.Message,
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }


    private void LoadInventoryItems(int inventoryId)
    {
        using var context = new DiplomContext();

        var items = context.InventoryItems
            .Where(x => x.InventoryId == inventoryId)
            .ToList();

        InventoryItemsList.Clear(); 
        foreach (var item in items)
        {
            InventoryItemsList.Add(new InventoryItem
            {
                InventoryNumber = item.InventoryNumber,
                Size = item.Size
            });
        }
    }



    private void DeleteSize_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is InventoryItem item)
        {
            InventoryItemsList.Remove(item);
        }
    }

}



