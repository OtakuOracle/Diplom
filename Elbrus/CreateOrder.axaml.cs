using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Helpers;
using Elbrus.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using ReactiveUI;

namespace Elbrus;

public partial class CreateOrder : Window, INotifyPropertyChanged, IReactiveObject, INotifyPropertyChanging
{
    private readonly DiplomContext _context = new();
    public ObservableCollection<Client> ClientList { get; } = new();
    public ObservableCollection<Service> ServiceList { get; } = new();
    public ObservableCollection<ServiceWithTime> BasketServices { get; } = new();
    public ObservableCollection<Inventory> InventoryList { get; } = new();
    public ObservableCollection<InventoryWithTime> BasketInventory { get; } = new();

    public string TimeInText { get; set; } = string.Empty;
    public string TimeOutText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string InfoTextContent { get; set; } = string.Empty;

    private Inventory _chosenInventory;
    public Inventory ChosenInventory
    {
        get => _chosenInventory;
        set => this.RaiseAndSetIfChanged(ref _chosenInventory, value);
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

    private int _hourSelected = 1;
    public int HourSelected
    {
        get => _hourSelected;
        set => this.RaiseAndSetIfChanged(ref _hourSelected, value);
    }

    private int _inventoryHourSelected = 1;
    public int InventoryHourSelected
    {
        get => _inventoryHourSelected;
        set => this.RaiseAndSetIfChanged(ref _inventoryHourSelected, value);
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
    }

    private async void LoadInitialData()
    {
        try
        {
            await _context.Clients.LoadAsync();
            await _context.Services.LoadAsync();
            await _context.Inventories.LoadAsync();

            ClientList.Clear();
            ServiceList.Clear();
            InventoryList.Clear();

            foreach (var cl in _context.Clients.Local.ToList())
                ClientList.Add(cl);

            foreach (var svc in _context.Services.Local.ToList())
                ServiceList.Add(svc);

            foreach (var inv in _context.Inventories.Local.ToList())
                InventoryList.Add(inv);
        }
        catch (Exception ex)
        {
            InfoTextContent = $"Ошибка при загрузке данных: {ex.Message}";
        }
    }

    public class ServiceWithTime : Service
    {
        public int TimeInHour { get; set; } = 1;
        public string CurrentStatus { get; set; } = "Новая услуга";
        public int ItemTotalPrice { get; set; } = 0;
    }

    public class InventoryWithTime : Inventory
    {
        public int TimeInHourInv { get; set; } = 1;
        public string CurrentStatus { get; set; } = "Новый инвентарь";
        public int ItemTotalPrice { get; set; } = 0;
    }
    private async void AddServiceClick(object sender, RoutedEventArgs e)
    {
        if (ChosenService != null && HourSelected > 0)
        {
            if (BasketServices.All(s => s.ServiceId != ChosenService.ServiceId))
            {
                BasketServices.Add(new ServiceWithTime
                {
                    ServiceId = ChosenService.ServiceId,
                    ServiceName = ChosenService.ServiceName,
                    CostPerHour = ChosenService.CostPerHour,
                    TimeInHour = HourSelected,
                    CurrentStatus = "Новая услуга"
                });
            }
            else
            {
                var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Эта услуга уже добавлена", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await error.ShowAsync();
            }
        }

    }

    private void RemoveServiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ServiceWithTime svc)
        {
            BasketServices.Remove(svc);
        }
    }

    private async void AddInventoryClick(object sender, RoutedEventArgs e)
    {
        if (ChosenInventory != null && InventoryHourSelected > 0)
        {
            if (BasketInventory.All(inv => inv.InventoryId != ChosenInventory.InventoryId))
            {
                BasketInventory.Add(new InventoryWithTime
                {
                    InventoryId = ChosenInventory.InventoryId,
                    InventoryName = ChosenInventory.InventoryName,
                    RentalCostPerHour = ChosenInventory.RentalCostPerHour,
                    TimeInHourInv = InventoryHourSelected,
                    CurrentStatus = "Новый инвентарь"
                });
            }
            else
            {
                var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Этот инвентарь уже добавлен", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await error.ShowAsync();
            }
        }

    }

    private void RemoveInventoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is InventoryWithTime inv)
        {
            BasketInventory.Remove(inv);
        }
    }

    private async void CompleteOrderClick(object sender, RoutedEventArgs e)
    {
        if (ChosenClient == null)
        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте клиента", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
            return;
        }

        if (BasketServices.Count == 0 && BasketInventory.Count == 0)
        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте услугу", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
            return;
        }

        if (!DateOrderPicker.SelectedDate.HasValue)
        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте дату", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
            return;
        }


        if (!TimeInOrderPicker.SelectedTime.HasValue ||
            !TimeOutOrderPicker.SelectedTime.HasValue)
           
        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте время начала", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
            return;
        }

        if ( !TimeOutOrderPicker.SelectedTime.HasValue)

        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте время окончания", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
            return;
        }




        try
        {
            string generatedOrderNum = $"{new Random().Next(100, 999)}";

            var newOrder = new Models.Order
            {
                ClientId = ChosenClient.ClientId,
                DateCreate = DateOnly.FromDateTime(DateTime.Now),
                TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                EmployeeId = CurrentUser.EmployeeId,
                OrderCode = generatedOrderNum,
                TotalPrice = 0
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            int totalOrderPrice = 0;

            var orderServicesToAdd = new List<OrderService>();
            var orderInventoriesToAdd = new List<OrderInventory>();

            TimeOnly startTime = TimeOnly.FromTimeSpan(TimeInOrderPicker.SelectedTime.Value); 
            TimeOnly endTime = TimeOnly.FromTimeSpan(TimeOutOrderPicker.SelectedTime.Value);  
            DateOnly orderDate = DateOnly.FromDateTime(DateOrderPicker.SelectedDate.Value.DateTime);


            int newServiceStatus = 1;

            int errorCount = 0;
            totalOrderPrice = 0;

            foreach (var svc in BasketServices)
            {
                if (svc.CostPerHour.HasValue && svc.TimeInHour > 0)
                {
                    int serviceCost = svc.CostPerHour.Value * svc.TimeInHour;
                    svc.ItemTotalPrice = serviceCost;
                    totalOrderPrice += serviceCost;
                }
                else
                {
                    var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте время", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await error.ShowAsync();
                }
            }

            foreach (var inv in BasketInventory)
            {
                if (inv.RentalCostPerHour.HasValue && inv.TimeInHourInv > 0)
                {
                    int inventoryCost = inv.RentalCostPerHour.Value * inv.TimeInHourInv;
                    inv.ItemTotalPrice = inventoryCost;
                    totalOrderPrice += inventoryCost;
                }
                else
                {
                    var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте время", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await error.ShowAsync();
                }
            }

            foreach (var svc in BasketServices)
            {
                var newOrderService = new OrderService
                {
                    OrderId = newOrder.OrderId,
                    ServiceId = svc.ServiceId,
                    RentTime = svc.TimeInHour,
                    OrderStatusId = 1,
                    TimeIn = startTime,
                    TimeOut = endTime,
                    Date = orderDate,
                };
                orderServicesToAdd.Add(newOrderService);
            }

            _context.OrderServices.AddRange(orderServicesToAdd);
            await _context.SaveChangesAsync();

            var savedOrderServices = await _context.OrderServices
                                                 .Where(os => os.OrderId == newOrder.OrderId)
                                                 .ToListAsync();

            var savedOrderServicesMap = savedOrderServices.ToDictionary(os => os.ServiceId);


            foreach (var inv in BasketInventory)
            {


                int? targetOrderServiceId = null;
                if (ChosenInventory != null && ChosenInventory.InventoryId == inv.InventoryId)
                {
                    if (BasketServices.Any() && savedOrderServicesMap.TryGetValue(BasketServices.First().ServiceId, out var firstOrderService))
                    {
                        targetOrderServiceId = firstOrderService.OrderServiceId;
                    }
                }

                if (savedOrderServices.Any())
                {
                    targetOrderServiceId = savedOrderServices.First().OrderServiceId;
                }
                else
                {
                    var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Добавьте \nуслугу", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await error.ShowAsync();
                    return;
                }


                if (targetOrderServiceId.HasValue)
                {
                    var newOrderInventory = new OrderInventory
                    {
                        InventoryId = inv.InventoryId,
                        OrderServiceId = targetOrderServiceId.Value,
                        RentTime = inv.TimeInHourInv

                    };
                    orderInventoriesToAdd.Add(newOrderInventory);
                }
            }


            if (orderInventoriesToAdd.Any())
            {
                _context.OrderInventories.AddRange(orderInventoriesToAdd);
                await _context.SaveChangesAsync();
            }

            newOrder.TotalPrice = totalOrderPrice;

            await _context.SaveChangesAsync();

            var successMessage = MessageBoxManager.GetMessageBoxStandard(
               "Успех",
               $"Заказ №{newOrder.OrderId} успешно создан! Общая стоимость: {newOrder.TotalPrice} руб.", 
               MsBox.Avalonia.Enums.ButtonEnum.Ok,
               MsBox.Avalonia.Enums.Icon.Success);
            await successMessage.ShowAsync();

            BasketServices.Clear();
            BasketInventory.Clear();
            ChosenClient = null;
            ChosenInventory = null;
            TimeInOrderPicker.SelectedTime = null;
            TimeOutOrderPicker.SelectedTime = null;
            DateOrderPicker.SelectedDate = null;
            HourSelected = 1;
            InventoryHourSelected = 1;
            ChosenService = null;

            Close();
        }
        catch (FormatException ex)
        {
            var error = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Проверьте дату и время", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await error.ShowAsync();
        }
        
    }


    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
