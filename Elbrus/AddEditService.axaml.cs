using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Elbrus;

public partial class AddEditService : Window
{
    public Service? ExistingService { get; set; }

    public AddEditService()
    {
        InitializeComponent();
        this.Title = "Добавить новую услугу";
        AddOrUpdateButton.Content = "Добавить";
    }

    public AddEditService(Service serviceToEdit)
    {
        InitializeComponent();
        ExistingService = serviceToEdit;
        this.Title = "Редактировать услугу";
        AddOrUpdateButton.Content = "Сохранить";

        ServiceNameBox.Text = ExistingService.ServiceName;
        ServiceCodeBox.Text = ExistingService.ServiceCode;
        CostBox.Text = ExistingService.CostPerHour.ToString();
    }

    private async void AddServiceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        using var context = new DiplomContext();

        if (string.IsNullOrWhiteSpace(ServiceNameBox.Text) ||
            string.IsNullOrWhiteSpace(ServiceCodeBox.Text) ||
            string.IsNullOrWhiteSpace(CostBox.Text))
        {
            var message = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Пожалуйста, заполните все поля!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await message.ShowAsync();
            return;
        }

        try
        {
            var serviceName = ServiceNameBox.Text.Trim();
            var serviceCode = ServiceCodeBox.Text.Trim();
            if (!int.TryParse(CostBox.Text, out int costPerHour))
            {
                var m = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Цена за час должна быть числом!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await m.ShowAsync();
                return;
            }

            var proposedService = new Service 
            {
                ServiceName = serviceName,
                ServiceCode = serviceCode,
                CostPerHour = costPerHour
            };

            if (!ValidateService(proposedService))
            {
                return; 
            }

            if (ExistingService == null) 
            {
                if (context.Services.Any(s => s.ServiceCode == serviceCode))
                {
                    var m = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Услуга с кодом '{serviceCode}' уже существует!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await m.ShowAsync();
                    return;
                }

                var newService = new Service 
                {
                    ServiceName = proposedService.ServiceName,
                    ServiceCode = proposedService.ServiceCode,
                    CostPerHour = proposedService.CostPerHour
                };

                context.Services.Add(newService);
                await context.SaveChangesAsync();
                await ShowSuccessMessageBox("Услуга успешно добавлена!"); 
            }
            else 
            {
                var serviceToUpdate = context.Services.Find(ExistingService.ServiceId);

                if (serviceToUpdate == null)
                {
                    if (context.Services.Any(s => s.ServiceCode == serviceCode && s.ServiceId != ExistingService.ServiceId))
                    {
                        await ShowErrorMessageBox($"Услуга с кодом '{serviceCode}' уже существует!");
                        return;
                    }
                    ExistingService.ServiceName = proposedService.ServiceName;
                    ExistingService.ServiceCode = proposedService.ServiceCode;
                    ExistingService.CostPerHour = proposedService.CostPerHour;
                    context.Services.Add(ExistingService); 
                }
                else
                {
                    if (context.Services.Any(s => s.ServiceCode == serviceCode && s.ServiceId != serviceToUpdate.ServiceId))
                    {
                        var m = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Услуга с кодом '{serviceCode}' уже существует!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        await m.ShowAsync();
                        return;
                    }

                    serviceToUpdate.ServiceName = proposedService.ServiceName;
                    serviceToUpdate.ServiceCode = proposedService.ServiceCode;
                    serviceToUpdate.CostPerHour = proposedService.CostPerHour;
                }

                await context.SaveChangesAsync();
                await ShowSuccessMessageBox("Изменения сохранены!"); 
            }

            this.Close();
        }
        catch (Exception ex)
        {
            var message = MessageBoxManager.GetMessageBoxStandard(
            "Ошибка",
            $"Произошла непредвиденная ошибка: {ex.Message}", 
            MsBox.Avalonia.Enums.ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Error);
            await message.ShowAsync(); 
        }
    }

    private bool ValidateService(Service s)
    {
        if (s.CostPerHour < 0) 
        {
            var errorCost = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                "Цена за час не должна быть отрицательной",
                MsBox.Avalonia.Enums.ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);
            errorCost.ShowAsync(); 
            return false;
        }
        return true; 
    }


    private void BackOnOrder(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }


    private Task ShowSuccessMessageBox(string message)
    {
        return MessageBoxManager.GetMessageBoxStandard(
            "Успех",
            message,
            ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Success)
            .ShowAsync();
    }

    private Task ShowErrorMessageBox(string message)
    {
        return MessageBoxManager.GetMessageBoxStandard(
            "Ошибка",
            message,
            ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Error)
            .ShowAsync();
    }

}
