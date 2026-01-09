using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Diagnostics;
using ReactiveUI;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using Elbrus.Helpers;

namespace Elbrus;

public partial class CreateOrder : Window, INotifyPropertyChanged, IReactiveObject, INotifyPropertyChanging
{
    private readonly ElbrusRegionContext _context = new();
    public ObservableCollection<Client> ClientList { get; } = new();
    public ObservableCollection<Service> ServiceList { get; } = new();
    public ObservableCollection<ServiceWithTime> BasketServices { get; } = new();

    public class ServiceWithTime : Service
    {
        public int TimeInMinutes { get; set; } = 30;
        public string CurrentStatus { get; set; } = "Новая услуга";
    }

    private Client _chosenClient;
    public Client ChosenClient
    {
        get => _chosenClient;
        set => this.RaiseAndSetIfChanged(ref _chosenClient, value);
    }

    private Service _chosenService;
    public Service ChosenService
    {
        get => _chosenService;
        set => this.RaiseAndSetIfChanged(ref _chosenService, value);
    }

    private int _minutesSelected = 30;
    public int MinutesSelected
    {
        get => _minutesSelected;
        set => this.RaiseAndSetIfChanged(ref _minutesSelected, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;

    void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
    void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);

    public CreateOrder()
    {
        InitializeComponent();
        DataContext = this;
        LoadInitialData();
        OrderNumberField.Text = "Генерация при оформлении";
    }

    private async void LoadInitialData()
    {
        try
        {
            await _context.Clients.LoadAsync();
            await _context.Services.LoadAsync();

            ClientList.Clear();
            ServiceList.Clear();

            foreach (var cl in _context.Clients.Local.ToList())
                ClientList.Add(cl);

            foreach (var svc in _context.Services.Local.ToList())
                ServiceList.Add(svc);
        }
        catch (Exception ex)
        {
            InfoText.Text = $"Ошибка при загрузке: {ex.Message}";
        }
    }





    //addclient

    private void AddServiceClick(object sender, RoutedEventArgs e) //переделать под себя
    {
        if (ChosenService != null)
        {
            if (BasketServices.All(s => s.ServiceId != ChosenService.ServiceId))
            {
                BasketServices.Add(new ServiceWithTime
                {
                    ServiceId = ChosenService.ServiceId,
                    ServiceName = ChosenService.ServiceName,
                    CostPerHour = ChosenService.CostPerHour,
                    TimeInMinutes = MinutesSelected,
                    CurrentStatus = "Новая услуга"
                });
            }
            else
            {
                InfoText.Text = "Такая услуга уже выбрана.";
            }
        }
    }

    private void RemoveServiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ServiceWithTime svc)
            BasketServices.Remove(svc);
    }

    private async void CompleteOrderClick(object sender, RoutedEventArgs e)
    {
        if (ChosenClient == null)
        {
            InfoText.Text = "Не выбран клиент.";
            return;
        }

        if (BasketServices.Count == 0)
        {
            InfoText.Text = "Не выбраны услуги.";
            return;
        }

        try
        {
            string generatedOrderNum = $"{new Random().Next(100, 999)}"; //есть
            var newOrder = new Models.Order
            {
                ClientId = ChosenClient.ClientId,
                DateCreate = DateOnly.FromDateTime(DateTime.Now),
                TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                EmployeeId = CurrentUser.EmployeeId,
                OrderCode = generatedOrderNum
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            foreach (var svc in BasketServices)
            {
                _context.OrderServices.Add(new OrderService
                {
                    OrderId = newOrder.OrderId,
                    ServiceId = svc.ServiceId,
                    RentTime = svc.TimeInMinutes,
                    Status = "Принят",
                    TimeIn = TimeOnly.Parse(TimeInBox.Text),
                    TimeOut = TimeOnly.Parse(TimeOutBox.Text),
                    Date = DateOnly.Parse(DateBox.Text)
                });
            }

            await _context.SaveChangesAsync();

            InfoText.Text = $"Заказ успешно создан. Номер заказа: {generatedOrderNum}";
            BasketServices.Clear();
            ChosenClient = null;
            OrderNumberField.Text = "Генерация при оформление";

            Close();
        }
        catch (Exception ex)
        {
            InfoText.Text = $"Ошибка при оформлении: {ex.Message}";
        }
    }



    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}