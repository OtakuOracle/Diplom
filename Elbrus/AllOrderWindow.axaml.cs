using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Helpers;
using Elbrus.Models;
using Elbrus.ViewModels;

namespace Elbrus;

public partial class AllOrderWindow : Window
{
    // нужен для Avalonia
    public AllOrderWindow()
    {
        InitializeComponent();
        DataContext = new OrderViewModel();
    }

    // для сотрудника
    public AllOrderWindow(int employeeId)
    {
        InitializeComponent();
        DataContext = new OrderViewModel(employeeId);
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}


