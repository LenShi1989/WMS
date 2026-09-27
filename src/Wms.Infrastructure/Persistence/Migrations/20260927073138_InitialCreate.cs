using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "number_sequences",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_number_sequences", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit_of_measures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_of_measures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    refresh_token_expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    specification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    safety_stock = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    min_stock = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    max_stock = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_materials_material_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "material_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    external_order_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    expected_arrival_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbound_orders_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbound_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_order_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    requested_ship_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    shipped_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbound_orders_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    zone_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_zones", x => x.id);
                    table.ForeignKey(
                        name: "fk_warehouse_zones_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_barcodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    barcode_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_barcodes_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inbound_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    putaway_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbound_details_inbound_orders_inbound_order_id",
                        column: x => x.inbound_order_id,
                        principalTable: "inbound_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inbound_details_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbound_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbound_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    allocated_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    picked_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    shipped_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbound_details_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outbound_details_outbound_orders_outbound_order_id",
                        column: x => x.outbound_order_id,
                        principalTable: "outbound_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    capacity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    weight_limit = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_locations_warehouse_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "warehouse_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_locations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktakes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktake_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktakes", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocktakes_warehouse_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "warehouse_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_stocktakes_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    available_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, computedColumnSql: "quantity - reserved_quantity", stored: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventories", x => x.id);
                    table.CheckConstraint("ck_inventories_quantity_non_negative", "quantity >= 0");
                    table.CheckConstraint("ck_inventories_reserved_le_quantity", "reserved_quantity <= quantity");
                    table.CheckConstraint("ck_inventories_reserved_non_negative", "reserved_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_inventories_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventories_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventories_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_locations_from_location_id",
                        column: x => x.from_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_locations_to_location_id",
                        column: x => x.to_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pick_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    outbound_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    picked_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pick_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_pick_tasks_locations_source_location_id",
                        column: x => x.source_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pick_tasks_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pick_tasks_outbound_details_outbound_detail_id",
                        column: x => x.outbound_detail_id,
                        principalTable: "outbound_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "putaway_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    inbound_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_putaway_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_putaway_tasks_inbound_details_inbound_detail_id",
                        column: x => x.inbound_detail_id,
                        principalTable: "inbound_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_putaway_tasks_locations_source_location_id",
                        column: x => x.source_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_putaway_tasks_locations_target_location_id",
                        column: x => x.target_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_putaway_tasks_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    executed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    executed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfer_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfer_orders_locations_from_location_id",
                        column: x => x.from_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_orders_locations_to_location_id",
                        column: x => x.to_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_orders_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_orders_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktake_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktake_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    counted_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    difference_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    counted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    counted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktake_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocktake_details_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stocktake_details_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stocktake_details_stocktakes_stocktake_id",
                        column: x => x.stocktake_id,
                        principalTable: "stocktakes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_module_action",
                table: "audit_logs",
                columns: new[] { "module", "action" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_details_inbound_order_id",
                table: "inbound_details",
                column: "inbound_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_details_material_id",
                table: "inbound_details",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_orders_created_at",
                table: "inbound_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_orders_order_no",
                table: "inbound_orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbound_orders_warehouse_id_status",
                table: "inbound_orders",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inventories_location_id_material_id_status",
                table: "inventories",
                columns: new[] { "location_id", "material_id", "status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventories_material_id",
                table: "inventories",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_warehouse_id_material_id",
                table: "inventories",
                columns: new[] { "warehouse_id", "material_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_created_at",
                table: "inventory_transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_from_location_id",
                table: "inventory_transactions",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_material_id_created_at",
                table: "inventory_transactions",
                columns: new[] { "material_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_reference_type_reference_id",
                table: "inventory_transactions",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_to_location_id",
                table: "inventory_transactions",
                column: "to_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_transaction_no",
                table: "inventory_transactions",
                column: "transaction_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_warehouse_id",
                table: "inventory_transactions",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_code",
                table: "locations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_locations_warehouse_id_zone_id",
                table: "locations",
                columns: new[] { "warehouse_id", "zone_id" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_zone_id",
                table: "locations",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_barcodes_barcode",
                table: "material_barcodes",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_barcodes_material_id",
                table: "material_barcodes",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_categories_code",
                table: "material_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_materials_category_id",
                table: "materials",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_code",
                table: "materials",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_materials_is_active",
                table: "materials",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_details_material_id",
                table: "outbound_details",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_details_outbound_order_id",
                table: "outbound_details",
                column: "outbound_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_orders_created_at",
                table: "outbound_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_orders_order_no",
                table: "outbound_orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbound_orders_warehouse_id_status",
                table: "outbound_orders",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_module",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_pick_tasks_material_id",
                table: "pick_tasks",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_pick_tasks_outbound_detail_id",
                table: "pick_tasks",
                column: "outbound_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_pick_tasks_source_location_id",
                table: "pick_tasks",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_pick_tasks_task_no",
                table: "pick_tasks",
                column: "task_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pick_tasks_warehouse_id_status",
                table: "pick_tasks",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_inbound_detail_id",
                table: "putaway_tasks",
                column: "inbound_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_material_id",
                table: "putaway_tasks",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_source_location_id",
                table: "putaway_tasks",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_target_location_id",
                table: "putaway_tasks",
                column: "target_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_task_no",
                table: "putaway_tasks",
                column: "task_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_putaway_tasks_warehouse_id_status",
                table: "putaway_tasks",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stocktake_details_location_id",
                table: "stocktake_details",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktake_details_material_id",
                table: "stocktake_details",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktake_details_stocktake_id",
                table: "stocktake_details",
                column: "stocktake_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktake_details_stocktake_id_location_id_material_id",
                table: "stocktake_details",
                columns: new[] { "stocktake_id", "location_id", "material_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stocktakes_stocktake_no",
                table: "stocktakes",
                column: "stocktake_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stocktakes_warehouse_id_status",
                table: "stocktakes",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stocktakes_zone_id",
                table: "stocktakes",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_created_at",
                table: "transfer_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_from_location_id",
                table: "transfer_orders",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_material_id",
                table: "transfer_orders",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_to_location_id",
                table: "transfer_orders",
                column: "to_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_transfer_no",
                table: "transfer_orders",
                column: "transfer_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfer_orders_warehouse_id_status",
                table: "transfer_orders",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_of_measures_code",
                table: "unit_of_measures",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_refresh_token",
                table: "users",
                column: "refresh_token");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_zones_warehouse_id_code",
                table: "warehouse_zones",
                columns: new[] { "warehouse_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_code",
                table: "warehouses",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropTable(
                name: "material_barcodes");

            migrationBuilder.DropTable(
                name: "number_sequences");

            migrationBuilder.DropTable(
                name: "pick_tasks");

            migrationBuilder.DropTable(
                name: "putaway_tasks");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "stocktake_details");

            migrationBuilder.DropTable(
                name: "transfer_orders");

            migrationBuilder.DropTable(
                name: "unit_of_measures");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "outbound_details");

            migrationBuilder.DropTable(
                name: "inbound_details");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "stocktakes");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "outbound_orders");

            migrationBuilder.DropTable(
                name: "inbound_orders");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropTable(
                name: "warehouse_zones");

            migrationBuilder.DropTable(
                name: "material_categories");

            migrationBuilder.DropTable(
                name: "warehouses");
        }
    }
}
