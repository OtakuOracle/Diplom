using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Elbrus.Models;

public partial class ElbrusRegionContext : DbContext
{
    public ElbrusRegionContext()
    {
    }

    public ElbrusRegionContext(DbContextOptions<ElbrusRegionContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderInventory> OrderInventories { get; set; }

    public virtual DbSet<OrderService> OrderServices { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5438;Database=Elbrus_region;Username=nastya;Password=123");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("clients_pkey");

            entity.ToTable("client");

            entity.HasIndex(e => e.ClientCode, "unique_client_code").IsUnique();

            entity.Property(e => e.ClientId)
                .HasDefaultValueSql("nextval('clients_id_seq'::regclass)")
                .HasColumnName("client_id");
            entity.Property(e => e.Address)
                .HasColumnType("character varying")
                .HasColumnName("address");
            entity.Property(e => e.Birthday).HasColumnName("birthday");
            entity.Property(e => e.ClientCode).HasColumnName("client_code");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.Fio)
                .HasMaxLength(100)
                .HasColumnName("fio");
            entity.Property(e => e.Passport)
                .HasMaxLength(10)
                .HasColumnName("passport");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.Role).HasColumnName("role");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Clients)
                .HasForeignKey(d => d.Role)
                .HasConstraintName("fk_role");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("staff_pkey");

            entity.ToTable("employees");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.EnterStatus)
                .HasColumnType("character varying")
                .HasColumnName("enter_status");
            entity.Property(e => e.Fio)
                .HasMaxLength(255)
                .HasColumnName("fio");
            entity.Property(e => e.LastEnter)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_enter");
            entity.Property(e => e.Login)
                .HasMaxLength(255)
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Photo)
                .HasColumnType("character varying")
                .HasColumnName("photo");
            entity.Property(e => e.Role).HasColumnName("role");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.Role)
                .HasConstraintName("fk_post_role");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_pkey");

            entity.ToTable("inventory");

            entity.HasIndex(e => e.Number, "inventory_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("model");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Number)
                .HasMaxLength(50)
                .HasColumnName("number");
            entity.Property(e => e.Size)
                .HasMaxLength(20)
                .HasColumnName("size");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.Property(e => e.OrderId)
                .HasDefaultValueSql("nextval('orders_orderid_seq'::regclass)")
                .HasColumnName("order_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.DateCreate).HasColumnName("date_create");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.OrderCode)
                .HasColumnType("character varying")
                .HasColumnName("order_code");
            entity.Property(e => e.TimeCreate).HasColumnName("time_create");

            entity.HasOne(d => d.Client).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_client");

            entity.HasOne(d => d.Employee).WithMany(p => p.Orders)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_orders_employees");
        });

        modelBuilder.Entity<OrderInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_inventory_pkey");

            entity.ToTable("order_inventory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.Model)
                .HasColumnType("character varying")
                .HasColumnName("model");
            entity.Property(e => e.OrderServiceId).HasColumnName("order_service_id");
            entity.Property(e => e.Size)
                .HasColumnType("character varying")
                .HasColumnName("size");

            entity.HasOne(d => d.Inventory).WithMany(p => p.OrderInventories)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_inventory_inventory_id_fkey");

            entity.HasOne(d => d.OrderService).WithMany(p => p.OrderInventories)
                .HasForeignKey(d => d.OrderServiceId)
                .HasConstraintName("fk_order_inventory_service");
        });

        modelBuilder.Entity<OrderService>(entity =>
        {
            entity.HasKey(e => e.OrderServiceId).HasName("order_services_pkey");

            entity.ToTable("order_services");

            entity.Property(e => e.OrderServiceId).HasColumnName("order_service_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.RentTime).HasColumnName("rent_time");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TimeIn).HasColumnName("time_in");
            entity.Property(e => e.TimeOut).HasColumnName("time_out");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order");

            entity.HasOne(d => d.Service).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_service");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("roles");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasColumnType("character varying")
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("services_pkey");

            entity.ToTable("service");

            entity.Property(e => e.ServiceId)
                .ValueGeneratedNever()
                .HasColumnName("service_id");
            entity.Property(e => e.CostPerHour).HasColumnName("cost_per_hour");
            entity.Property(e => e.ServiceCode)
                .HasColumnType("character varying")
                .HasColumnName("service_code");
            entity.Property(e => e.ServiceName)
                .HasColumnType("character varying")
                .HasColumnName("service_name");
        });
        modelBuilder.HasSequence("inventory_id_seq");
        modelBuilder.HasSequence("inventory_type_inventory_type_id_seq");
        modelBuilder.HasSequence("order_inventory_id_seq");
        modelBuilder.HasSequence("order_services_order_service_id_seq");
        modelBuilder.HasSequence("orders_orderid_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
