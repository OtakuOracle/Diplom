using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Elbrus.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Interactivity;
using System.Net.Sockets;

namespace Elbrus;

public partial class EmployeeWindow : Window
{
    // Свойства для доступа к данным из других частей программы
    public Employee CurrentEmployee { get; set; }
    public ObservableCollection<Service> ServicesList { get; set; }

    // Конструктор для предварительного просмотра в дизайнере
    public EmployeeWindow()
    {
        InitializeComponent();
    }

    // Основной конструктор, вызываемый при входе
    public EmployeeWindow(Employee employee)
    {
        InitializeComponent();

        // Проверка: если сотрудник не передан, выходим, чтобы не было ошибок
        if (employee == null) return;

        using (ElbrusRegionContext db = new ElbrusRegionContext())
        {
            // 1. Сохраняем переданного сотрудника в свойство
            this.CurrentEmployee = employee;

            // 2. Заполняем текстовые поля в шапке (по именам из XAML)
            // Эти элементы: TextBlock Name="EmployeeFio" и Name="RoleName"
            EmployeeFio.Text = employee.Fio;

            // 3. Получаем название роли из связанной таблицы roles
            // Ищем роль, где id совпадает с числом в employee.Role
            var userRole = db.Roles.FirstOrDefault(r => r.RoleId == employee.Role);
            RoleName.Text = userRole?.RoleName ?? "Должность не указана";

            // 4. Загружаем список услуг из базы данных
            // Модель Service должна содержать поля ServiceId, ServiceName, ServiceCode, CostPerHour
            var servicesFromDb = db.Services.ToList();

            // Создаем коллекцию для отображения
            ServicesList = new ObservableCollection<Service>(servicesFromDb);

            // 5. Привязываем данные к списку в интерфейсе (x:Name="ListServices")
            ListServices.ItemsSource = ServicesList;
        }
    }

    private void CreateOrderClick(object? sender, RoutedEventArgs e)
    {
        new CreateOrder().Show();
    }

}