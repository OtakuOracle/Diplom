using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Helpers;
using Elbrus.Models;
using Elbrus.ViewModels;

namespace Elbrus;

public partial class AllOrderWindow : Window
{
    public AllOrderWindow()
    {
        InitializeComponent();
        DataContext = new OrderViewModel();
    }

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


