using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Elbrus.Models;

public partial class DiplomContext : DbContext
{
    public DiplomContext()
    {
    }

    public DiplomContext(DbContextOptions<DiplomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EnterStatus> EnterStatuses { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryStatus> InventoryStatuses { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderInventory> OrderInventories { get; set; }

    public virtual DbSet<OrderService> OrderServices { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=213.171.24.157; Port=5432; Username=nastya; Database=diplom; Password=123");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("client_pkey");

            entity.ToTable("client");

            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Address)
                .HasColumnType("character varying")
                .HasColumnName("address");
            entity.Property(e => e.Birthday).HasColumnName("birthday");
            entity.Property(e => e.ClientCode).HasColumnName("client_code");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasColumnType("character varying")
                .HasColumnName("full_name");
            entity.Property(e => e.Passport)
                .HasColumnType("character varying")
                .HasColumnName("passport");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Clients)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("client_role_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("employee_pkey");

            entity.ToTable("employee");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EnterStatus).HasColumnName("enter_status");
            entity.Property(e => e.FullName)
                .HasColumnType("character varying")
                .HasColumnName("full_name");
            entity.Property(e => e.LastEnter)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_enter");
            entity.Property(e => e.Login)
                .HasColumnType("character varying")
                .HasColumnName("login");
            entity.Property(e => e.Passwrd)
                .HasColumnType("character varying")
                .HasColumnName("passwrd");
            entity.Property(e => e.Photo)
                .HasColumnType("character varying")
                .HasColumnName("photo");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.EnterStatusNavigation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.EnterStatus)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("employee_enter_status_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("employee_role_id_fkey");
        });

        modelBuilder.Entity<EnterStatus>(entity =>
        {
            entity.HasKey(e => e.EnterStatusId).HasName("enter_status_pkey");

            entity.ToTable("enter_status");

            entity.Property(e => e.EnterStatusId).HasColumnName("enter_status_id");
            entity.Property(e => e.EnterStatusName)
                .HasColumnType("character varying")
                .HasColumnName("enter_status_name");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("inventory_pkey");

            entity.ToTable("inventory");

            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.InventoryModel)
                .HasColumnType("character varying")
                .HasColumnName("inventory_model");
            entity.Property(e => e.InventoryName)
                .HasColumnType("character varying")
                .HasColumnName("inventory_name");
            entity.Property(e => e.InventoryNumber)
                .HasColumnType("character varying")
                .HasColumnName("inventory_number");
            entity.Property(e => e.InventorySize)
                .HasColumnType("character varying")
                .HasColumnName("inventory_size");
            entity.Property(e => e.InventoryStatusId).HasColumnName("inventory_status_id");
            entity.Property(e => e.Photo)
                .HasColumnType("character varying")
                .HasColumnName("photo");
            entity.Property(e => e.RentalCostPerHour).HasColumnName("rental_cost_per_hour");

            entity.HasOne(d => d.InventoryStatus).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.InventoryStatusId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("inventory_inventory_status_id_fkey");
        });

        modelBuilder.Entity<InventoryStatus>(entity =>
        {
            entity.HasKey(e => e.InventoryStatusId).HasName("inventory_status_pkey");

            entity.ToTable("inventory_status");

            entity.Property(e => e.InventoryStatusId).HasColumnName("inventory_status_id");
            entity.Property(e => e.InventoryStatusName)
                .HasColumnType("character varying")
                .HasColumnName("inventory_status_name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("order_pkey");

            entity.ToTable("order");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.DateCreate).HasColumnName("date_create");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.OrderCode)
                .HasColumnType("character varying")
                .HasColumnName("order_code");
            entity.Property(e => e.TimeCreate).HasColumnName("time_create");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");

            entity.HasOne(d => d.Client).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_client_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Orders)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_employee_id_fkey");
        });

        modelBuilder.Entity<OrderInventory>(entity =>
        {
            entity.HasKey(e => e.OrderInventoryId).HasName("order_inventory_pkey");

            entity.ToTable("order_inventory");

            entity.Property(e => e.OrderInventoryId).HasColumnName("order_inventory_id");
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.OrderServiceId).HasColumnName("order_service_id");
            entity.Property(e => e.RentTime).HasColumnName("rent_time");

            entity.HasOne(d => d.Inventory).WithMany(p => p.OrderInventories)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_inventory_inventory_id_fkey");

            entity.HasOne(d => d.OrderService).WithMany(p => p.OrderInventories)
                .HasForeignKey(d => d.OrderServiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_inventory_order_service_id_fkey");
        });

        modelBuilder.Entity<OrderService>(entity =>
        {
            entity.HasKey(e => e.OrderServiceId).HasName("order_service_pkey");

            entity.ToTable("order_service");

            entity.Property(e => e.OrderServiceId).HasColumnName("order_service_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderStatusId).HasColumnName("order_status_id");
            entity.Property(e => e.RentTime).HasColumnName("rent_time");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.TimeIn).HasColumnName("time_in");
            entity.Property(e => e.TimeOut).HasColumnName("time_out");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_service_order_id_fkey");

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.OrderStatusId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_service_order_status_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_service_service_id_fkey");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("order_status_pkey");

            entity.ToTable("order_status");

            entity.Property(e => e.OrderStatusId).HasColumnName("order_status_id");
            entity.Property(e => e.OrderStatusName)
                .HasColumnType("character varying")
                .HasColumnName("order_status_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasColumnType("character varying")
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("service_pkey");

            entity.ToTable("service");

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.CostPerHour).HasColumnName("cost_per_hour");
            entity.Property(e => e.ServiceCode)
                .HasColumnType("character varying")
                .HasColumnName("service_code");
            entity.Property(e => e.ServiceName)
                .HasColumnType("character varying")
                .HasColumnName("service_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
