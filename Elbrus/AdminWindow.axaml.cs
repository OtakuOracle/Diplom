using System;
using System.Collections.Generic;
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
using Elbrus.Helpers;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace Elbrus;

public partial class AdminWindow : Window
{
    private readonly TimeSpan _sessionDuration = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _warningTime = TimeSpan.FromMinutes(5);
    private DateTime _sessionStartTime;
    private bool _warningShown;
    private Employee _currentEmployee;
    private ObservableCollection<Employee> employees = new();
    public List<Employee> AllEmployees = new();

    public AdminWindow()
    {
        InitializeComponent();
        LoadData();
        DataContext = this;


        LoginComboBox.SelectionChanged += ComboBox_SelectionChanged;
        SortComboBox.SelectionChanged += ComboBox_SelectionChanged;
        ResetButton.Click += ResetButton_Click;
    }

    public AdminWindow(Employee admin) : this()
    {
        if (admin == null)
        {
            this.Close();
            return;
        }

        _currentEmployee = admin;

        _sessionStartTime = DateTime.Now;
        _ = StartSessionTimerAsync();

        LoadUserData();
    }
    private void LoadUserData()
    {
        using (DiplomContext db = new DiplomContext())
        {
            var fioBlock = this.FindControl<TextBlock>("FullName");
            if (fioBlock != null)
            {
                fioBlock.Text = _currentEmployee.FullName;
            }

            var roleBlock = this.FindControl<TextBlock>("RoleName");
            if (_currentEmployee.RoleId.HasValue)
            {
                var userRole = db.Roles.FirstOrDefault(r => r.RoleId == _currentEmployee.RoleId);
                if (roleBlock != null)
                {
                    roleBlock.Text = userRole?.RoleName; 
                }
            }
        }
        LoadUserImage();
    }

    private void LoadUserImage()
    {
        try
        {
            if (_currentEmployee != null && !string.IsNullOrEmpty(_currentEmployee.Photo))
            {
                var path = Path.Combine(AppContext.BaseDirectory, _currentEmployee.Photo);
                if (File.Exists(path))
                {
                    if (UserImage != null)
                    {
                        UserImage.Source = new Bitmap(path);
                    }
                    return;
                }
            }
            if (UserImage != null)
            {
                UserImage.Source = null;
            }
        }
        catch (Exception)
        {
            if (UserImage != null)
            {
                UserImage.Source = null;
            }
        }
    }



    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }

    private async Task StartSessionTimerAsync()
    {
        while (true)
        {
            TimeSpan elapsed;
            TimeSpan remaining;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                elapsed = DateTime.Now - _sessionStartTime;
                remaining = _sessionDuration - elapsed;

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

            if (_sessionStartTime > DateTime.Now)
            {
                _sessionStartTime = DateTime.Now;
            }

            await Task.Delay(1000);
        }
    }


    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void LoadData()
    {
        using var context = new DiplomContext();
        AllEmployees = context.Employees
            .OrderByDescending(e => e.LastEnter)
            .Select(e => new Employee
            {
                EmployeeId = e.EmployeeId,
                Login = e.Login,
                LastEnter = e.LastEnter,
            }).ToList();

        employees = new ObservableCollection<Employee>(AllEmployees);
        LastEnterBox.ItemsSource = employees;

        LoginComboBox.ItemsSource = new List<string> { "Все пользователи" }
            .Concat(AllEmployees.Select(e => e.Login).Distinct().OrderBy(l => l));
        LoginComboBox.SelectedIndex = 0;
    }

    private void ApplyFilters()
    {
        var filtered = AllEmployees.AsEnumerable();

        if (LoginComboBox.SelectedItem is string selectedLogin && selectedLogin != "Все пользователи")
        {
            filtered = filtered.Where(e => e.Login == selectedLogin);
        }

        filtered = SortComboBox.SelectedIndex == 0
            ? filtered.OrderByDescending(e => e.LastEnter)
            : filtered.OrderBy(e => e.LastEnter);

        employees.Clear();
        foreach (var emp in filtered)
        {
            employees.Add(emp);
        }
    }

    private void ResetButton_Click(object? sender, RoutedEventArgs e)
    {
        LoginComboBox.SelectedIndex = 0;
        SortComboBox.SelectedIndex = 0;
        ApplyFilters();
    }

 



    private void InventoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var inventoryWindow = new InventoryWindow();
        inventoryWindow.Show();
    }


}
