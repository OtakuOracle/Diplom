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

namespace Elbrus;

public partial class AddEditInventory : Window
{

    private Inventory _inventory;
    private string ImageName;
    private string _currentPhotoPath;

    /// <summary>
    /// добавление
    /// </summary>
    public AddEditInventory()
    {
        InitializeComponent();

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

        _inventory = inventory; 
        DataContext = _inventory; 

        LoadStatuses();

        AddBut.IsVisible = false;
        EditBut.IsVisible = true;
        DeleteBut.IsVisible = true;

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

            if (!ValidateInventory(newInventory))
                return;

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
            else
                newInventory.Photo = null;

            context.Inventories.Add(newInventory);
            await context.SaveChangesAsync();

            var newItem = new InventoryItem
            {
                InventoryId = newInventory.InventoryId,
                InventoryNumber = InventoryNumberBox.Text,
                Size = InventorySizeBox.Text,
                InventoryStatusId = selectedStatusObject.InventoryStatusId
            };

            context.InventoryItems.Add(newItem);
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
            var inventoryToUpdate = DataContext as Inventory;

            if (inventoryToUpdate == null)
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Не удалось получить данные инвентаря",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await errorMessage.ShowAsync();
                return;
            }

            var attachedInventory = await context.Inventories.FindAsync(inventoryToUpdate.InventoryId);

            if (attachedInventory == null)
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Запись инвентаря не найдена",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await errorMessage.ShowAsync();
                return;
            }

            var inventoryItem = context.InventoryItems
                .FirstOrDefault(x => x.InventoryId == inventoryToUpdate.InventoryId);

            if (inventoryItem == null)
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Экземпляр инвентаря не найден",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await errorMessage.ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(inventoryToUpdate.InventoryName) ||
                string.IsNullOrWhiteSpace(inventoryToUpdate.InventoryModel) ||
                inventoryToUpdate.RentalCostPerHour == null)
            {
                var validationError = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Все поля должны быть заполнены",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await validationError.ShowAsync();
                return;
            }

            attachedInventory.InventoryName = inventoryToUpdate.InventoryName;
            attachedInventory.InventoryModel = inventoryToUpdate.InventoryModel;
            attachedInventory.RentalCostPerHour = inventoryToUpdate.RentalCostPerHour;

            inventoryItem.InventoryNumber = InventoryNumberBox.Text;
            inventoryItem.Size = InventorySizeBox.Text;

            var selectedStatusObject = InventoryStatus.SelectedItem as InventoryStatus;

            if (selectedStatusObject == null)
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Не выбран статус",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);

                await errorMessage.ShowAsync();
                return;
            }

            inventoryItem.InventoryStatusId = selectedStatusObject.InventoryStatusId;

            if (!string.IsNullOrEmpty(ImageName))
            {
                attachedInventory.Photo = "inv/" + ImageName;
            }

            if (!ValidateInventory(attachedInventory))
                return;

            await context.SaveChangesAsync();

            var successMessage = MessageBoxManager.GetMessageBoxStandard(
                "Успех",
                "Данные обновлены успешно",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Success);

            await successMessage.ShowAsync();
        }
        catch (Exception ex)
        {
            var error = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                ex.ToString(),
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);

            await error.ShowAsync();
        }
    }
}



