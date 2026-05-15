using System;
using System.Collections.ObjectModel;
using Npgsql;
using Elbrus.Models;

namespace Elbrus.ViewModels
{
    public class OrderViewModel
    {
        public ObservableCollection<OrderItem> Orders { get; set; } = new ObservableCollection<OrderItem>();

        public OrderViewModel(int? employeeId = null)
        {
            LoadOrders(employeeId);
        }


        private void LoadOrders(int? employeeId)
        {
            string connString = "Host=213.171.24.157;Port=5432;Username=nastya;Password=123;Database=diplom";

            using var conn = new NpgsqlConnection(connString);
            conn.Open();

            string query = @"SELECT 
        c.full_name,
        e.full_name,
        s.service_name,
        i.inventory_name,
        o.total_price,
        os.date,
        os.time_in,
        os.time_out
    FROM ""order"" o
    LEFT JOIN client c ON o.client_id = c.client_id
    LEFT JOIN employee e ON o.employee_id = e.employee_id
    LEFT JOIN order_service os ON o.order_id = os.order_id
    LEFT JOIN service s ON os.service_id = s.service_id
    LEFT JOIN order_inventory oi ON os.order_service_id = oi.order_service_id
    LEFT JOIN inventory i ON oi.inventory_id = i.inventory_id";

            if (employeeId != null)
                query += " WHERE o.employee_id = @employeeId";

            using var cmd = new NpgsqlCommand(query, conn);

            if (employeeId != null)
                cmd.Parameters.AddWithValue("@employeeId", employeeId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Orders.Add(new OrderItem
                {
                    Client = reader[0]?.ToString(),
                    Employee = reader[1]?.ToString(),
                    Service = reader[2]?.ToString(),
                    Inventory = reader[3]?.ToString(),
                    TotalPrice = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Date = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
                    TimeStart = reader.IsDBNull(6) ? null : reader.GetFieldValue<TimeOnly>(6),
                    TimeEnd = reader.IsDBNull(7) ? null : reader.GetFieldValue<TimeOnly>(7)
                });
            }
        }



    }
}