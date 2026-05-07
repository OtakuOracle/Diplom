using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Helpers;
using Elbrus.Models;
using MsBox.Avalonia;

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
            using (DiplomContext db = new DiplomContext())
            {
                var employee = db.Employees.FirstOrDefault(it => it.Login == LoginTextBox.Text && it.Passwrd == PasswordTextBox.Text);
                if (employee != null)
                {
                    CurrentUser.EmployeeId = employee.EmployeeId;

                    int? roleId = employee.RoleId;
                    if (roleId == null)
                    {
                        var message = MessageBoxManager.GetMessageBoxStandard(
                            "Ошибка",
                            "Не задана роль пользователя",
                            MsBox.Avalonia.Enums.ButtonEnum.Ok,
                            MsBox.Avalonia.Enums.Icon.Error);

                        message.ShowAsync();
                    }

                        switch (roleId.Value)
                    {
                        case 1:
                        case 2:
                            EmployeeWindow empWindow = new EmployeeWindow(employee);
                            empWindow.Show();
                            this.Close();
                            break;
                        case 3:
                            AdminWindow adminWindow = new AdminWindow(employee);
                            adminWindow.Show();
                            this.Close();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var message = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Неверный логин или пароль", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    message.ShowAsync();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Ошибка", "Произошла ошибка", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
        }

    }


}
