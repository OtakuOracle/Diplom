using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Helpers;
using Elbrus.Models;
using System;
using System.Linq;

namespace Elbrus;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void TogglePasswordVisibility(object? sender, RoutedEventArgs e)
    {
        PasswordTextBox.PasswordChar = PasswordTextBox.PasswordChar == '*' ? '\0' : '*';
    }


    private void AuthorizeButton(object? sender, RoutedEventArgs e)
    {
        try
        {
            using (ElbrusRegionContext db = new ElbrusRegionContext())
            {
                var employee = db.Employees.FirstOrDefault(it => it.Login == LoginTextBox.Text && it.Password == PasswordTextBox.Text);
                if (employee != null)
                {
                    // Установите идентификатор текущего пользователя
                    CurrentUser.EmployeeId = employee.Id; // Предполагается, что у вас есть свойство Id в модели Employee

                    int roleId = employee.Role;

                    switch (roleId)
                    {
                        case 1:
                        case 2:
                            EmployeeWindow empWindow = new EmployeeWindow(employee);
                            empWindow.Show();
                            this.Close();
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    ErrorBlock.Text = "Пользователь не найден";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorBlock.Text = "Ошибка: " + ex.Message;
        }
    }
}