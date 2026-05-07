using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Elbrus.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Elbrus;

public partial class EmployeeWindow : Window
{
    private readonly TimeSpan _sessionDuration = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _warningTime = TimeSpan.FromMinutes(5);
    private DateTime _sessionStartTime;
    private bool _warningShown;

    public Employee CurrentEmployee { get; set; }
    public ObservableCollection<Service> ServicesList { get; set; } = new ObservableCollection<Service>();
    public ObservableCollection<Client> ClientsList { get; set; } = new ObservableCollection<Client>();

    public EmployeeWindow()
    {
        InitializeComponent();
    }

    public EmployeeWindow(Employee employee) : this()
    {
        if (employee == null) return;

        using (DiplomContext db = new DiplomContext())
        {
            this.CurrentEmployee = employee;

            var fioBlock = this.FindControl<TextBlock>("FullName");
            if (fioBlock != null)
            {
                fioBlock.Text = employee.FullName;
            }
            var roleBlock = this.FindControl<TextBlock>("RoleName");
            var userRole = db.Roles.FirstOrDefault(r => r.RoleId == employee.RoleId);
            if (roleBlock != null)
            {
                roleBlock.Text = userRole?.RoleName ?? "Должность не указана";
            }

            LoadUserImage();

            var servicesFromDb = db.Services.ToList();
            ServicesList = new ObservableCollection<Service>(servicesFromDb);
            var servicesListBox = this.FindControl<ListBox>("ListServices");
            if (servicesListBox != null)
            {
                servicesListBox.ItemsSource = ServicesList;
            }

            var clientsFromDb = db.Clients.ToList();
            ClientsList = new ObservableCollection<Client>(clientsFromDb);
            var clientsListBox = this.FindControl<ListBox>("ListClients");
            if (clientsListBox != null)
            {
                clientsListBox.ItemsSource = ClientsList;
            }
        }

        _sessionStartTime = DateTime.Now;
        _ = StartSessionTimerAsync();
    }

    private void LoadUserImage()
    {
        try
        {
            if (!string.IsNullOrEmpty(CurrentEmployee?.Photo))
            {
                var path = Path.Combine(AppContext.BaseDirectory, CurrentEmployee.Photo);
                if (File.Exists(path))
                {
                    UserImage.Source = new Bitmap(path);
                    return;
                }
            }
            UserImage.Source = null;
        }
        catch (Exception)
        {
            UserImage.Source = null;
        }
    }


    private async Task StartSessionTimerAsync()
    {
        while (true)
        {
            var elapsed = DateTime.Now - _sessionStartTime;
            var remaining = _sessionDuration - elapsed;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var timerTextBlock = this.FindControl<TextBlock>("SessionTimerText");
                if (timerTextBlock != null)
                {
                    timerTextBlock.Text = remaining > TimeSpan.Zero
                        ? $"Осталось: {remaining:mm\\:ss}"
                        : "Время вышло";
                }

                var warningTextBlock = this.FindControl<TextBlock>("SessionWarningText");
                if (warningTextBlock != null && !_warningShown && remaining <= _warningTime && remaining > TimeSpan.Zero)
                {
                    _warningShown = true;
                    warningTextBlock.Text = "Внимание, до окончания сеанса осталось меньше 5 минут!";
                }
            });

            if (remaining <= TimeSpan.Zero)
            {
                await Dispatcher.UIThread.InvokeAsync(CloseAndReturnToMain);
                break;
            }

            await Task.Delay(1000);
        }
    }

    private async void OnClientDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Client selectedClient)
        {
            var editClientWindow = new AddEditClient(selectedClient);
            await editClientWindow.ShowDialog(this);
            RefreshClientsList();
        }
    }

    private async void OnServiceDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Service selectedService)
        {
            var editServiceWindow = new AddEditService(selectedService); 
            await editServiceWindow.ShowDialog(this);
            RefreshServicesList();
        }
    }

    private void RefreshClientsList()
    {
        using (var db = new DiplomContext())
        {
            var clientsFromDb = db.Clients.ToList();
            ClientsList = new ObservableCollection<Client>(clientsFromDb);
            var listBox = this.FindControl<ListBox>("ListClients");
            if (listBox != null)
            {
                listBox.ItemsSource = ClientsList;
            }
        }
    }

    private void RefreshServicesList()
    {
        using (var db = new DiplomContext())
        {
            var servicesFromDb = db.Services.ToList();
            ServicesList = new ObservableCollection<Service>(servicesFromDb);
            var listBox = this.FindControl<ListBox>("ListServices");
            if (listBox != null)
            {
                listBox.ItemsSource = ServicesList;
            }
        }
    }

    public void AddServiceClick(object? sender, RoutedEventArgs e)
    {
        var addServiceWindow = new AddEditService();
        addServiceWindow.Closed += (_, _) => RefreshServicesList();
        addServiceWindow.Show();
    }

    private void CloseAndReturnToMain()
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }

    private void AddClientClick(object? sender, RoutedEventArgs e)
    {
        var addClientWindow = new AddEditClient();
        addClientWindow.Closed += (_, _) => RefreshClientsList();
        addClientWindow.Show();
    }



    private async void DeleteServiceClick(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("ListServices");
        if (listBox?.SelectedItem is Service selectedService)
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение удаления",
                $"Вы уверены, что хотите удалить услугу \"{selectedService.ServiceName}\"?",
                ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Question)
                .ShowAsync();

            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                try
                {
                    using (var context = new DiplomContext())
                    {
                        var serviceToRemove = context.Services.FirstOrDefault(c => c.ServiceId == selectedService.ServiceId);
                        if (serviceToRemove != null)
                        {
                            context.Services.Remove(serviceToRemove);
                            await context.SaveChangesAsync();
                        }
                    }

                    await MessageBoxManager.GetMessageBoxStandard(
                        "Успех",
                        "Услуга успешно удалена!",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Success)
                        .ShowAsync();

                    await LoadServicesAsync();
                }
                catch (Exception ex)
                {
                    await MessageBoxManager.GetMessageBoxStandard(
                     "Ошибка",
                     "Произошла ошибка при удалении услуги",
                     ButtonEnum.Ok,
                     MsBox.Avalonia.Enums.Icon.Error)
                     .ShowAsync();
                }
            }
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Внимание",
                "Пожалуйста, выберите услугу для удаления",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Warning)
                .ShowAsync();
        }
    }

    private async Task LoadServicesAsync()
    {
        using (var context = new DiplomContext())
        {
            var services = await context.Services.ToListAsync();
            var listBox = this.FindControl<ListBox>("ListServices");
            if (listBox != null)
            {
                listBox.ItemsSource = services;
            }
        }
    }



    private async void DeleteClientClick(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("ListClients");
        if (listBox?.SelectedItem is Client selectedClient)
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение удаления",
                $"Вы уверены, что хотите удалить клиента \"{selectedClient.FullName}\"?",
                ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Question)
                .ShowAsync();

            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                try
                {
                    using (var context = new DiplomContext())
                    {
                        var clientToRemove = context.Clients.FirstOrDefault(c => c.ClientId == selectedClient.ClientId);
                        if (clientToRemove != null)
                        {
                            context.Clients.Remove(clientToRemove);
                            await context.SaveChangesAsync();
                        }
                    }

                    await MessageBoxManager.GetMessageBoxStandard(
                        "Успех",
                        "Клиент успешно удален!",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Success)
                        .ShowAsync();

                    await LoadClientsAsync();
                }
                catch (Exception ex)
                {
                    await MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка",
                        $"Произошла ошибка при удалении клиента: {ex.Message}",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error)
                        .ShowAsync();
                }
            }
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Внимание",
                "Пожалуйста, выберите клиента для удаления",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Warning)
                .ShowAsync();
        }
    }


    private async Task LoadClientsAsync()
    {
        using (var context = new DiplomContext())
        {
            var clients = await context.Clients.ToListAsync();
            var listBox = this.FindControl<ListBox>("ListClients");
            if (listBox != null)
            {
                listBox.ItemsSource = clients;
            }
        }
    }


    private void CreateOrderClick(object? sender, RoutedEventArgs e)
    {
        var createOrder = new CreateOrder();
        createOrder.Show();
    }



    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }
}

