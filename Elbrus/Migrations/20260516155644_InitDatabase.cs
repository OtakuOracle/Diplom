using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Elbrus.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "enter_status",
                columns: table => new
                {
                    enter_status_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enter_status_name = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("enter_status_pkey", x => x.enter_status_id);
                });

            migrationBuilder.CreateTable(
                name: "inventory",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_name = table.Column<string>(type: "character varying", nullable: true),
                    inventory_model = table.Column<string>(type: "character varying", nullable: true),
                    rental_cost_per_hour = table.Column<int>(type: "integer", nullable: true),
                    photo = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("inventory_pkey", x => x.inventory_id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_status",
                columns: table => new
                {
                    inventory_status_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_status_name = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("inventory_status_pkey", x => x.inventory_status_id);
                });

            migrationBuilder.CreateTable(
                name: "order_status",
                columns: table => new
                {
                    order_status_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_status_name = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_status_pkey", x => x.order_status_id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_pkey", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "service",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_name = table.Column<string>(type: "character varying", nullable: true),
                    service_code = table.Column<string>(type: "character varying", nullable: true),
                    cost_per_hour = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_pkey", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_item",
                columns: table => new
                {
                    inventory_item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_id = table.Column<int>(type: "integer", nullable: false),
                    inventory_number = table.Column<string>(type: "character varying", nullable: true),
                    size = table.Column<string>(type: "character varying", nullable: true),
                    inventory_status_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("inventory_item_pkey", x => x.inventory_item_id);
                    table.ForeignKey(
                        name: "FK_inventory_item_inventory_status_inventory_status_id",
                        column: x => x.inventory_status_id,
                        principalTable: "inventory_status",
                        principalColumn: "inventory_status_id");
                    table.ForeignKey(
                        name: "inventory_item_inventory_id_fkey",
                        column: x => x.inventory_id,
                        principalTable: "inventory",
                        principalColumn: "inventory_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying", nullable: true),
                    client_code = table.Column<int>(type: "integer", nullable: true),
                    passport = table.Column<string>(type: "character varying", nullable: true),
                    birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "character varying", nullable: true),
                    email = table.Column<string>(type: "character varying", nullable: true),
                    password = table.Column<string>(type: "character varying", nullable: true),
                    role_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_pkey", x => x.client_id);
                    table.ForeignKey(
                        name: "client_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: true),
                    full_name = table.Column<string>(type: "character varying", nullable: true),
                    login = table.Column<string>(type: "character varying", nullable: true),
                    passwrd = table.Column<string>(type: "character varying", nullable: true),
                    last_enter = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    photo = table.Column<string>(type: "character varying", nullable: true),
                    enter_status = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employee_pkey", x => x.employee_id);
                    table.ForeignKey(
                        name: "employee_enter_status_fkey",
                        column: x => x.enter_status,
                        principalTable: "enter_status",
                        principalColumn: "enter_status_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "employee_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_code = table.Column<string>(type: "character varying", nullable: true),
                    date_create = table.Column<DateOnly>(type: "date", nullable: true),
                    time_create = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    total_price = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_pkey", x => x.order_id);
                    table.ForeignKey(
                        name: "order_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "order_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_service",
                columns: table => new
                {
                    order_service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    rent_time = table.Column<int>(type: "integer", nullable: true),
                    order_status_id = table.Column<int>(type: "integer", nullable: true),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_service_pkey", x => x.order_service_id);
                    table.ForeignKey(
                        name: "order_service_order_id_fkey",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "order_service_order_status_id_fkey",
                        column: x => x.order_status_id,
                        principalTable: "order_status",
                        principalColumn: "order_status_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "order_service_service_id_fkey",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_inventory",
                columns: table => new
                {
                    order_inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_item_id = table.Column<int>(type: "integer", nullable: true),
                    order_service_id = table.Column<int>(type: "integer", nullable: true),
                    rent_time = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_inventory_pkey", x => x.order_inventory_id);
                    table.ForeignKey(
                        name: "order_inventory_inventory_item_id_fkey",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_item",
                        principalColumn: "inventory_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "order_inventory_order_service_id_fkey",
                        column: x => x.order_service_id,
                        principalTable: "order_service",
                        principalColumn: "order_service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_client_role_id",
                table: "client",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_enter_status",
                table: "employee",
                column: "enter_status");

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_id",
                table: "employee",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_inventory_id",
                table: "inventory_item",
                column: "inventory_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_inventory_status_id",
                table: "inventory_item",
                column: "inventory_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_client_id",
                table: "order",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_employee_id",
                table: "order",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_inventory_inventory_item_id",
                table: "order_inventory",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_inventory_order_service_id",
                table: "order_inventory",
                column: "order_service_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_service_order_id",
                table: "order_service",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_service_order_status_id",
                table: "order_service",
                column: "order_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_service_service_id",
                table: "order_service",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_inventory");

            migrationBuilder.DropTable(
                name: "inventory_item");

            migrationBuilder.DropTable(
                name: "order_service");

            migrationBuilder.DropTable(
                name: "inventory_status");

            migrationBuilder.DropTable(
                name: "inventory");

            migrationBuilder.DropTable(
                name: "order");

            migrationBuilder.DropTable(
                name: "order_status");

            migrationBuilder.DropTable(
                name: "service");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "enter_status");

            migrationBuilder.DropTable(
                name: "role");
        }
    }
}
