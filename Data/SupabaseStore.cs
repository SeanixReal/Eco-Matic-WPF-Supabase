using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Eco_Matic.Utilities;
using Eco_Matic;

namespace Eco_Matic.Data;

/// <summary>
/// Data store backed by Supabase (PostgREST).
/// Drop-in replacement for MySqlStore — all methods are synchronous wrappers
/// around async calls so the existing WPF code doesn't need to change.
/// </summary>
public partial class SupabaseStore
{
    private readonly SupabaseClient _client = SupabaseClient.Instance;

    // ──────────────────────────────────────────────────
    //  Helper: run async as sync (safe for WPF non-UI thread calls)
    // ──────────────────────────────────────────────────
    private T Run<T>(System.Threading.Tasks.Task<T> task)
    {
        return task.GetAwaiter().GetResult();
    }

    private void Run(System.Threading.Tasks.Task task)
    {
        task.GetAwaiter().GetResult();
    }

    private sealed class MachineSlotRecord
    {
        public int InventoryId { get; init; }
        public string RawSlotId { get; init; } = string.Empty;
        public string? NormalizedSlotId { get; init; }
    }

    private List<MachineSlotRecord> GetMachineSlotRecords(int machineId)
    {
        var list = new List<MachineSlotRecord>();
        var rows = Run(_client.GetAsync("machine_inventory",
            $"select=inventory_id,slot_id&machine_id=eq.{machineId}"));

        foreach (var node in rows)
        {
            string rawSlotId = node?["slot_id"]?.GetValue<string>() ?? "";
            list.Add(new MachineSlotRecord
            {
                InventoryId = node?["inventory_id"]?.GetValue<int>() ?? 0,
                RawSlotId = rawSlotId,
                NormalizedSlotId = SlotIdHelper.Normalize(rawSlotId)
            });
        }

        return list;
    }

    private bool TryValidateSlotForMachine(int machineId, string slotId, out string normalizedSlotId, out string errorMessage, int? excludeInventoryId = null)
    {
        normalizedSlotId = SlotIdHelper.Normalize(slotId) ?? "";
        errorMessage = "";
        string targetSlotId = normalizedSlotId;

        if (string.IsNullOrWhiteSpace(targetSlotId))
        {
            errorMessage = "Slot ID must be a number from 1 to 12.";
            return false;
        }

        var slots = GetMachineSlotRecords(machineId);
        bool slotTaken = slots.Any(x =>
            x.InventoryId != excludeInventoryId &&
            string.Equals(x.NormalizedSlotId, targetSlotId, StringComparison.Ordinal));

        if (slotTaken)
        {
            errorMessage = $"Slot {normalizedSlotId} is already in use for this machine.";
            return false;
        }

        if (!excludeInventoryId.HasValue && slots.Count >= SlotIdHelper.MaxSlot)
        {
            errorMessage = "A vending machine can only contain up to 12 slots.";
            return false;
        }

        return true;
    }

    private static bool TryValidateStockValues(int stock, int maxCap, out string errorMessage)
    {
        errorMessage = "";
        if (stock < 0)
        {
            errorMessage = "Stock cannot be negative.";
            return false;
        }

        if (maxCap <= 0)
        {
            errorMessage = "Max capacity must be greater than zero.";
            return false;
        }

        if (maxCap > DataStore.MaxStockPerItem)
        {
            errorMessage = $"Max capacity cannot exceed {DataStore.MaxStockPerItem}.";
            return false;
        }

        if (stock > maxCap)
        {
            errorMessage = $"Stock cannot exceed max capacity ({maxCap}).";
            return false;
        }

        return true;
    }

    private static object? ToDbNumber(decimal? value)
    {
        return value.HasValue ? value.Value : null;
    }

    public bool CanConnect()
    {
        return Run(_client.CanConnectAsync());
    }

    private static InvalidOperationException BuildClientSyncColumnException(string tableName, Exception innerException)
    {
        return new InvalidOperationException(
            $"Supabase sync replay requires the nullable client_sync_id column on {tableName}. Apply the repo migration for client_sync_id first.",
            innerException);
    }

    // ══════════════════════════════════════════════════
    //  ITEMS (Master Catalog)
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetAllItems()
    {
        var dt = new System.Data.DataTable();
        try
        {
            var rows = Run(_client.GetAsync("items", "select=item_id,name,type,price,calories,image_path,dispense_message,examine_message&order=name.asc"));
            dt.Columns.Add("item_id", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("type", typeof(string));
            dt.Columns.Add("price", typeof(decimal));
            dt.Columns.Add("calories", typeof(int));
            dt.Columns.Add("image_path", typeof(string));
            dt.Columns.Add("dispense_message", typeof(string));
            dt.Columns.Add("examine_message", typeof(string));

            foreach (var node in rows)
            {
                dt.Rows.Add(
                    node?["item_id"]?.GetValue<int>() ?? 0,
                    node?["name"]?.GetValue<string>() ?? "",
                    node?["type"]?.GetValue<string>() ?? "Misc",
                    node?["price"]?.GetValue<decimal>() ?? 0m,
                    node?["calories"]?.GetValue<int>() ?? 0,
                    node?["image_path"]?.GetValue<string>() ?? "",
                    node?["dispense_message"]?.GetValue<string>() ?? "Enjoy your item!",
                    node?["examine_message"]?.GetValue<string>() ?? "A standard vending item."
                );
            }
        }
        catch { }
        return dt;
    }

    public System.Data.DataTable GetCatalogItems()
    {
        var dt = new System.Data.DataTable();
        try
        {
            var rows = Run(_client.GetAsync("items",
                "select=item_id,name,type,price,calories,image_path,dispense_message,examine_message&order=item_id.asc"));
            var usageRows = Run(_client.GetAsync("machine_inventory", "select=item_id"));
            var usageCounts = new Dictionary<int, int>();

            foreach (var node in usageRows)
            {
                int itemId = node?["item_id"]?.GetValue<int>() ?? 0;
                if (itemId <= 0)
                {
                    continue;
                }

                usageCounts[itemId] = usageCounts.TryGetValue(itemId, out int currentCount) ? currentCount + 1 : 1;
            }

            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Default Price", typeof(decimal));
            dt.Columns.Add("Calories", typeof(int));
            dt.Columns.Add("Image", typeof(string));
            dt.Columns.Add("Dispense Message", typeof(string));
            dt.Columns.Add("Examine Message", typeof(string));
            dt.Columns.Add("Machines Using Item", typeof(int));

            foreach (var node in rows)
            {
                int itemId = node?["item_id"]?.GetValue<int>() ?? 0;
                dt.Rows.Add(
                    itemId,
                    node?["name"]?.GetValue<string>() ?? "",
                    node?["type"]?.GetValue<string>() ?? "Misc",
                    node?["price"]?.GetValue<decimal>() ?? 0m,
                    node?["calories"]?.GetValue<int>() ?? 0,
                    node?["image_path"]?.GetValue<string>() ?? "",
                    node?["dispense_message"]?.GetValue<string>() ?? "Enjoy your item!",
                    node?["examine_message"]?.GetValue<string>() ?? "A standard vending item.",
                    usageCounts.TryGetValue(itemId, out int usageCount) ? usageCount : 0
                );
            }
        }
        catch { }
        return dt;
    }

    public bool AddCatalogItem(string name, string type, decimal price, int calories, string imagePath, string dispenseMessage, string examineMessage)
    {
        try
        {
            if (TryFindDuplicateCatalogItemName(name, out _))
            {
                System.Windows.MessageBox.Show(
                    "An item with the same name already exists in the global catalog. Reuse the existing item or rename this one.",
                    "Duplicate Item",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            var result = Run(_client.PostAsync("items", new
            {
                name,
                type,
                price,
                calories,
                image_path = imagePath,
                dispense_message = dispenseMessage,
                examine_message = examineMessage
            }));
            return result.Count > 0;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to create item: " + ex.Message);
            return false;
        }
    }

    public bool UpdateCatalogItem(int itemId, string name, string type, decimal price, int calories, string imagePath, string dispenseMessage, string examineMessage)
    {
        try
        {
            if (TryFindDuplicateCatalogItemName(name, out int duplicateItemId) && duplicateItemId != itemId)
            {
                System.Windows.MessageBox.Show(
                    "Another global item already uses that name. Choose a different item name to avoid duplicate catalog entries.",
                    "Duplicate Item",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            Run(_client.PatchAsync("items", $"item_id=eq.{itemId}", new
            {
                name,
                type,
                price,
                calories,
                image_path = imagePath,
                dispense_message = dispenseMessage,
                examine_message = examineMessage
            }));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to update item: " + ex.Message);
            return false;
        }
    }

    private bool TryFindDuplicateCatalogItemName(string name, out int itemId)
    {
        itemId = 0;
        string normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        var rows = Run(_client.GetAsync("items", "select=item_id,name"));
        foreach (var node in rows)
        {
            string existingName = node?["name"]?.GetValue<string>() ?? string.Empty;
            if (!string.Equals(existingName.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            itemId = node?["item_id"]?.GetValue<int>() ?? 0;
            return itemId > 0;
        }

        return false;
    }

    public bool DeleteCatalogItem(int itemId)
    {
        try
        {
            var usageRows = Run(_client.GetAsync("machine_inventory", $"select=inventory_id&item_id=eq.{itemId}"));
            if (usageRows.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    "This item is still assigned to one or more machine slots. Remove those assignments first.",
                    "Item In Use",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            Run(_client.DeleteAsync("items", $"item_id=eq.{itemId}"));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to delete item: " + ex.Message);
            return false;
        }
    }

    public List<RecyclableItemDefinition> GetActiveRecyclableItems()
    {
        var items = new List<RecyclableItemDefinition>();

        try
        {
            var rows = Run(_client.GetAsync("recyclable_items",
                "select=id,display_name,material_type,unit_label,points_per_unit,description,is_active,sort_order&is_active=eq.true&order=sort_order.asc"));

            foreach (var node in rows)
            {
                items.Add(new RecyclableItemDefinition
                {
                    Id = node?["id"]?.GetValue<int>() ?? 0,
                    DisplayName = node?["display_name"]?.GetValue<string>() ?? string.Empty,
                    MaterialType = node?["material_type"]?.GetValue<string>() ?? string.Empty,
                    UnitLabel = node?["unit_label"]?.GetValue<string>() ?? "piece",
                    PointsPerUnit = node?["points_per_unit"]?.GetValue<int>() ?? 0,
                    Description = node?["description"]?.GetValue<string>() ?? string.Empty,
                    IsActive = node?["is_active"]?.GetValue<bool>() ?? true,
                    SortOrder = node?["sort_order"]?.GetValue<int>() ?? 0
                });
            }
        }
        catch
        {
        }

        return items;
    }

    public System.Data.DataTable GetRecyclableCatalog(bool includeInactive = true)
    {
        var dt = new System.Data.DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("Display Name", typeof(string));
        dt.Columns.Add("Material Type", typeof(string));
        dt.Columns.Add("Unit Label", typeof(string));
        dt.Columns.Add("Points / Unit", typeof(int));
        dt.Columns.Add("Description", typeof(string));
        dt.Columns.Add("Active", typeof(string));
        dt.Columns.Add("Sort Order", typeof(int));

        try
        {
            string filter = includeInactive ? string.Empty : "&is_active=eq.true";
            var rows = Run(_client.GetAsync("recyclable_items",
                $"select=id,display_name,material_type,unit_label,points_per_unit,description,is_active,sort_order&order=sort_order.asc{filter}"));

            foreach (var node in rows)
            {
                bool isActive = node?["is_active"]?.GetValue<bool>() ?? true;
                dt.Rows.Add(
                    node?["id"]?.GetValue<int>() ?? 0,
                    node?["display_name"]?.GetValue<string>() ?? string.Empty,
                    node?["material_type"]?.GetValue<string>() ?? string.Empty,
                    node?["unit_label"]?.GetValue<string>() ?? "piece",
                    node?["points_per_unit"]?.GetValue<int>() ?? 0,
                    node?["description"]?.GetValue<string>() ?? string.Empty,
                    isActive ? "Active" : "Inactive",
                    node?["sort_order"]?.GetValue<int>() ?? 0
                );
            }
        }
        catch
        {
        }

        return dt;
    }

    public bool AddRecyclableItem(string displayName, string materialType, string unitLabel, int pointsPerUnit, string description, bool isActive, int sortOrder)
    {
        try
        {
            var inserted = Run(_client.PostAsync("recyclable_items", new
            {
                display_name = displayName,
                material_type = materialType,
                unit_label = unitLabel,
                points_per_unit = pointsPerUnit,
                description,
                is_active = isActive,
                sort_order = sortOrder
            }));
            return inserted.Count > 0;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to create recyclable item: " + ex.Message);
            return false;
        }
    }

    public bool UpdateRecyclableItem(int recyclableItemId, string displayName, string materialType, string unitLabel, int pointsPerUnit, string description, bool isActive, int sortOrder)
    {
        try
        {
            Run(_client.PatchAsync("recyclable_items", $"id=eq.{recyclableItemId}", new
            {
                display_name = displayName,
                material_type = materialType,
                unit_label = unitLabel,
                points_per_unit = pointsPerUnit,
                description,
                is_active = isActive,
                sort_order = sortOrder
            }));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to update recyclable item: " + ex.Message);
            return false;
        }
    }

    public bool SetRecyclableItemActive(int recyclableItemId, bool isActive)
    {
        try
        {
            Run(_client.PatchAsync("recyclable_items", $"id=eq.{recyclableItemId}", new
            {
                is_active = isActive
            }));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to update recyclable item status: " + ex.Message);
            return false;
        }
    }

    // ══════════════════════════════════════════════════
    //  AUTHENTICATION
    // ══════════════════════════════════════════════════

    public (string? Role, int? AssignedMachineId) AuthenticateUser(string username, string password)
    {
        try
        {
            // Query users joined with roles
            var rows = Run(_client.GetAsync("users",
                $"select=user_id,username,password_hash,assigned_machine_id,roles(role_name)&username=eq.{Uri.EscapeDataString(username)}&password_hash=eq.{Uri.EscapeDataString(password)}"));

            if (rows.Count > 0)
            {
                var user = rows[0];
                string? roleName = user?["roles"]?["role_name"]?.GetValue<string>();
                int? machineId = null;
                var mid = user?["assigned_machine_id"];
                if (mid != null && mid.GetValueKind() != JsonValueKind.Null)
                    machineId = mid.GetValue<int>();

                return (roleName, machineId);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Database connection failed: {ex.Message}");
            return (null, null);
        }
    }

    public (string? Role, List<int> AssignedMachineIds) AuthenticateUserAccess(string username, string password)
    {
        try
        {
            var rows = Run(_client.GetAsync("users",
                $"select=user_id,username,password_hash,assigned_machine_id,roles(role_name)&username=eq.{Uri.EscapeDataString(username)}&password_hash=eq.{Uri.EscapeDataString(password)}"));

            if (rows.Count == 0)
            {
                return (null, new List<int>());
            }

            var user = rows[0];
            int userId = user?["user_id"]?.GetValue<int>() ?? 0;
            string? roleName = user?["roles"]?["role_name"]?.GetValue<string>();
            var assignedIds = GetAssignedMachineIdsForUser(userId);

            if (assignedIds.Count == 0)
            {
                var mid = user?["assigned_machine_id"];
                if (mid != null && mid.GetValueKind() != JsonValueKind.Null)
                {
                    assignedIds.Add(mid.GetValue<int>());
                }
            }

            return (roleName, assignedIds);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Database connection failed: {ex.Message}");
            return (null, new List<int>());
        }
    }

    // ══════════════════════════════════════════════════
    //  VENDING MACHINES
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetVendingMachines()
    {
        var dt = new System.Data.DataTable();
        try
        {
            JsonArray rows;
            try
            {
                rows = Run(_client.GetAsync("vending_machines", "select=machine_id,location_name,address_text,latitude,longitude,status,created_at"));
            }
            catch
            {
                rows = Run(_client.GetAsync("vending_machines", "select=machine_id,location_name,status,created_at"));
            }

            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("Deployed", typeof(DateTime));
            dt.Columns.Add("_Latitude", typeof(double));
            dt.Columns.Add("_Longitude", typeof(double));

            foreach (var node in rows)
            {
                var latitudeNode = node?["latitude"];
                var longitudeNode = node?["longitude"];
                double? latitude = latitudeNode != null && latitudeNode.GetValueKind() != JsonValueKind.Null
                    ? latitudeNode.GetValue<double>()
                    : null;
                double? longitude = longitudeNode != null && longitudeNode.GetValueKind() != JsonValueKind.Null
                    ? longitudeNode.GetValue<double>()
                    : null;

                dt.Rows.Add(
                    node?["machine_id"]?.GetValue<int>() ?? 0,
                    node?["location_name"]?.GetValue<string>() ?? "",
                    node?["address_text"]?.GetValue<string>() ?? "",
                    node?["status"]?.GetValue<string>() ?? "Active",
                    DateTime.Parse(node?["created_at"]?.GetValue<string>() ?? DateTime.Now.ToString()),
                    latitude.HasValue ? latitude.Value : DBNull.Value,
                    longitude.HasValue ? longitude.Value : DBNull.Value
                );
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load machines: {ex.Message}");
        }
        return dt;
    }

    public System.Data.DataTable GetVendingMachinesLookup()
    {
        var dt = new System.Data.DataTable();
        try
        {
            JsonArray rows;
            try
            {
                rows = Run(_client.GetAsync("vending_machines", "select=machine_id,location_name,address_text,latitude,longitude,status"));
            }
            catch
            {
                rows = Run(_client.GetAsync("vending_machines", "select=machine_id,location_name,status"));
            }

            dt.Columns.Add("machine_id", typeof(int));
            dt.Columns.Add("location_name", typeof(string));
            dt.Columns.Add("address_text", typeof(string));
            dt.Columns.Add("latitude", typeof(double));
            dt.Columns.Add("longitude", typeof(double));
            dt.Columns.Add("status", typeof(string));

            foreach (var node in rows)
            {
                var latitudeNode = node?["latitude"];
                var longitudeNode = node?["longitude"];
                double? latitude = latitudeNode != null && latitudeNode.GetValueKind() != JsonValueKind.Null
                    ? latitudeNode.GetValue<double>()
                    : null;
                double? longitude = longitudeNode != null && longitudeNode.GetValueKind() != JsonValueKind.Null
                    ? longitudeNode.GetValue<double>()
                    : null;

                dt.Rows.Add(
                    node?["machine_id"]?.GetValue<int>() ?? 0,
                    node?["location_name"]?.GetValue<string>() ?? "",
                    node?["address_text"]?.GetValue<string>() ?? "",
                    latitude.HasValue ? latitude.Value : DBNull.Value,
                    longitude.HasValue ? longitude.Value : DBNull.Value,
                    node?["status"]?.GetValue<string>() ?? ""
                );
            }
        }
        catch { }
        return dt;
    }

    public int GetAssignedSlotCount(int machineId)
    {
        return GetMachineSlotRecords(machineId).Count;
    }

    public string? GetNextAvailableSlotId(int machineId)
    {
        var usedSlots = new HashSet<string>(StringComparer.Ordinal);
        foreach (string? slotId in GetMachineSlotRecords(machineId).Select(slot => slot.NormalizedSlotId))
        {
            if (!string.IsNullOrWhiteSpace(slotId))
            {
                usedSlots.Add(slotId);
            }
        }

        for (int slotNumber = SlotIdHelper.MinSlot; slotNumber <= SlotIdHelper.MaxSlot; slotNumber++)
        {
            string candidate = slotNumber.ToString();
            if (!usedSlots.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public int? CreateMachine(string locationName, string? addressText = null, double? latitude = null, double? longitude = null)
    {
        try
        {
            // Enforce max 4 vending machines
            var existing = Run(_client.GetAsync("vending_machines", "select=machine_id"));
            if (existing.Count >= 4)
            {
                System.Windows.MessageBox.Show("Maximum of 4 vending machines allowed.", "Limit Reached",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return null;
            }

            JsonArray result;
            try
            {
                result = Run(_client.PostAsync("vending_machines", new
                {
                    location_name = locationName,
                    address_text = string.IsNullOrWhiteSpace(addressText) ? null : addressText,
                    latitude,
                    longitude
                }));
            }
            catch
            {
                result = Run(_client.PostAsync("vending_machines", new { location_name = locationName }));
            }

            if (result.Count == 0)
            {
                return null;
            }

            return result[0]?["machine_id"]?.GetValue<int>();
        }
        catch { return null; }
    }

    public bool AddMachine(string locationName, string? addressText = null, double? latitude = null, double? longitude = null)
    {
        return CreateMachine(locationName, addressText, latitude, longitude).HasValue;
    }

    public bool DeleteMachine(int machineId)
    {
        try
        {
            Run(_client.DeleteAsync("vending_machines", $"machine_id=eq.{machineId}"));
            return true;
        }
        catch { return false; }
    }

    public bool UpdateMachine(int machineId, string locationName, string addressText, string status, double? latitude = null, double? longitude = null)
    {
        try
        {
            try
            {
                Run(_client.PatchAsync("vending_machines", $"machine_id=eq.{machineId}",
                    new
                    {
                        location_name = locationName,
                        address_text = string.IsNullOrWhiteSpace(addressText) ? null : addressText,
                        latitude,
                        longitude,
                        status
                    }));
            }
            catch
            {
                Run(_client.PatchAsync("vending_machines", $"machine_id=eq.{machineId}",
                    new { location_name = locationName, status }));
            }

            return true;
        }
        catch { return false; }
    }

    // ══════════════════════════════════════════════════
    //  ROLES
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetRoles()
    {
        var dt = new System.Data.DataTable();
        try
        {
            var rows = Run(_client.GetAsync("roles", "select=role_id,role_name&order=role_id.asc"));
            dt.Columns.Add("role_id", typeof(int));
            dt.Columns.Add("role_name", typeof(string));

            foreach (var node in rows)
            {
                dt.Rows.Add(
                    node?["role_id"]?.GetValue<int>() ?? 0,
                    node?["role_name"]?.GetValue<string>() ?? ""
                );
            }
        }
        catch { }
        return dt;
    }

    public int? GetInventoryManagerRoleId()
    {
        try
        {
            var rows = Run(_client.GetAsync("roles",
                "select=role_id&role_name=eq.Inventory%20Manager&limit=1"));
            return rows.Count > 0 ? rows[0]?["role_id"]?.GetValue<int>() : null;
        }
        catch
        {
            return null;
        }
    }

    // ══════════════════════════════════════════════════
    //  USERS
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetUsers()
    {
        var dt = new System.Data.DataTable();
        try
        {
            var rows = Run(_client.GetAsync("users",
                "select=user_id,username,assigned_machine_id,roles(role_name)&order=user_id.asc"));
            var assignmentRows = Run(_client.GetAsync("user_machine_assignments", "select=user_id,machine_id"));
            var machineRows = Run(_client.GetAsync("vending_machines", "select=machine_id,location_name"));
            var machineNames = new Dictionary<int, string>();
            foreach (var machine in machineRows)
            {
                int machineId = machine?["machine_id"]?.GetValue<int>() ?? 0;
                if (machineId > 0)
                {
                    machineNames[machineId] = machine?["location_name"]?.GetValue<string>() ?? $"Machine {machineId}";
                }
            }

            var assignmentsByUser = new Dictionary<int, List<int>>();
            foreach (var assignment in assignmentRows)
            {
                int userId = assignment?["user_id"]?.GetValue<int>() ?? 0;
                int machineId = assignment?["machine_id"]?.GetValue<int>() ?? 0;
                if (userId <= 0 || machineId <= 0)
                {
                    continue;
                }

                if (!assignmentsByUser.TryGetValue(userId, out var list))
                {
                    list = new List<int>();
                    assignmentsByUser[userId] = list;
                }

                if (!list.Contains(machineId))
                {
                    list.Add(machineId);
                }
            }

            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Username", typeof(string));
            dt.Columns.Add("Role", typeof(string));
            dt.Columns.Add("Assigned Machines", typeof(string));
            dt.Columns.Add("_AssignedMachineIds", typeof(string));

            foreach (var node in rows)
            {
                string? roleName = node?["roles"]?["role_name"]?.GetValue<string>();
                if (roleName == "Admin") continue; // Skip admin users
                int userId = node?["user_id"]?.GetValue<int>() ?? 0;
                var machineIds = assignmentsByUser.TryGetValue(userId, out var assigned)
                    ? assigned
                    : new List<int>();

                if (machineIds.Count == 0)
                {
                    var legacyMachineId = node?["assigned_machine_id"];
                    if (legacyMachineId != null && legacyMachineId.GetValueKind() != JsonValueKind.Null)
                    {
                        machineIds.Add(legacyMachineId.GetValue<int>());
                    }
                }

                machineIds = machineIds.Distinct().OrderBy(id => id).ToList();
                string assignedMachineNames = machineIds.Count == 0
                    ? ""
                    : string.Join(", ", machineIds.Select(id => machineNames.TryGetValue(id, out string? name) ? name : $"Machine {id}"));

                dt.Rows.Add(
                    userId,
                    node?["username"]?.GetValue<string>() ?? "",
                    roleName ?? "",
                    assignedMachineNames,
                    string.Join(",", machineIds)
                );
            }
        }
        catch { }
        return dt;
    }

    public bool AddUser(string username, string password, int roleId, int? assignedMachineId)
    {
        return AddUser(username, password, roleId, assignedMachineId.HasValue ? new[] { assignedMachineId.Value } : Array.Empty<int>());
    }

    public bool AddUser(string username, string password, int roleId, IEnumerable<int> assignedMachineIds)
    {
        try
        {
            var machineIds = assignedMachineIds.Distinct().Where(id => id > 0).ToList();
            var body = new Dictionary<string, object?>
            {
                ["username"] = username,
                ["password_hash"] = password,
                ["role_id"] = roleId,
                ["assigned_machine_id"] = machineIds.FirstOrDefault() > 0 ? machineIds.First() : null
            };

            var result = Run(_client.PostAsync("users", body));
            int userId = result.Count > 0 ? result[0]?["user_id"]?.GetValue<int>() ?? 0 : 0;
            if (userId <= 0)
            {
                return false;
            }

            return ReplaceUserMachineAssignments(userId, machineIds);
        }
        catch { return false; }
    }

    public bool UpdateUserMachineAssignments(int userId, IEnumerable<int> assignedMachineIds)
    {
        try
        {
            return ReplaceUserMachineAssignments(userId, assignedMachineIds);
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteUser(int userId)
    {
        try
        {
            Run(_client.DeleteAsync("user_machine_assignments", $"user_id=eq.{userId}"));
            Run(_client.DeleteAsync("users", $"user_id=eq.{userId}"));
            return true;
        }
        catch { return false; }
    }

    private List<int> GetAssignedMachineIdsForUser(int userId)
    {
        var result = new List<int>();
        if (userId <= 0)
        {
            return result;
        }

        try
        {
            var rows = Run(_client.GetAsync("user_machine_assignments",
                $"select=machine_id&user_id=eq.{userId}&order=machine_id.asc"));
            foreach (var row in rows)
            {
                int machineId = row?["machine_id"]?.GetValue<int>() ?? 0;
                if (machineId > 0 && !result.Contains(machineId))
                {
                    result.Add(machineId);
                }
            }
        }
        catch
        {
        }

        return result;
    }

    private bool ReplaceUserMachineAssignments(int userId, IEnumerable<int> assignedMachineIds)
    {
        var machineIds = assignedMachineIds.Distinct().Where(id => id > 0).OrderBy(id => id).ToList();
        Run(_client.DeleteAsync("user_machine_assignments", $"user_id=eq.{userId}"));

        if (machineIds.Count > 0)
        {
            var assignments = machineIds.Select(machineId => new
            {
                user_id = userId,
                machine_id = machineId
            }).ToList();
            Run(_client.PostAsync("user_machine_assignments", assignments));
        }

        Run(_client.PatchAsync("users", $"user_id=eq.{userId}", new
        {
            assigned_machine_id = machineIds.Count > 0 ? (int?)machineIds[0] : null
        }));

        return true;
    }

    // ══════════════════════════════════════════════════
    //  MACHINE INVENTORY
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetMachineInventory(int machineId)
    {
        var dt = new System.Data.DataTable();
        try
        {
            JsonArray rows;
            try
            {
                rows = Run(_client.GetAsync("machine_inventory",
                    $"select=inventory_id,slot_id,stock_level,max_capacity,slot_price,item_id,items(item_id,name,type,price,calories,image_path,dispense_message,examine_message)&machine_id=eq.{machineId}&order=slot_id.asc"));
            }
            catch
            {
                rows = Run(_client.GetAsync("machine_inventory",
                    $"select=inventory_id,slot_id,stock_level,max_capacity,item_id,items(item_id,name,type,price,calories,image_path,dispense_message,examine_message)&machine_id=eq.{machineId}&order=slot_id.asc"));
            }

            dt.Columns.Add("Slot", typeof(string));
            dt.Columns.Add("_SlotSort", typeof(int));
            dt.Columns.Add("_InventoryID", typeof(int));
            dt.Columns.Add("_ItemID", typeof(int));
            dt.Columns.Add("Image", typeof(string));
            dt.Columns.Add("Item", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Default Price", typeof(decimal));
            dt.Columns.Add("Slot Price", typeof(decimal));
            dt.Columns.Add("Price", typeof(decimal));
            dt.Columns.Add("Calories", typeof(int));
            dt.Columns.Add("Dispense Message", typeof(string));
            dt.Columns.Add("Examine Message", typeof(string));
            dt.Columns.Add("Stock", typeof(int));
            dt.Columns.Add("Max Capacity", typeof(int));

            foreach (var node in rows)
            {
                var item = node?["items"];
                decimal defaultPrice = item?["price"]?.GetValue<decimal>() ?? 0m;
                var slotPriceNode = node?["slot_price"];
                decimal? slotPrice = slotPriceNode != null && slotPriceNode.GetValueKind() != JsonValueKind.Null
                    ? slotPriceNode.GetValue<decimal>()
                    : null;

                string normalizedSlot = SlotIdHelper.Normalize(node?["slot_id"]?.GetValue<string>() ?? "") ?? (node?["slot_id"]?.GetValue<string>() ?? "");
                int slotSort = SlotIdHelper.TryGetSlotNumber(normalizedSlot, out int parsedSlot) ? parsedSlot : 999;

                dt.Rows.Add(
                    normalizedSlot,
                    slotSort,
                    node?["inventory_id"]?.GetValue<int>() ?? 0,
                    item?["item_id"]?.GetValue<int>() ?? 0,
                    item?["image_path"]?.GetValue<string>() ?? "",
                    item?["name"]?.GetValue<string>() ?? "Unknown",
                    item?["type"]?.GetValue<string>() ?? "Misc",
                    defaultPrice,
                    slotPrice.HasValue ? slotPrice.Value : DBNull.Value,
                    slotPrice ?? defaultPrice,
                    item?["calories"]?.GetValue<int>() ?? 0,
                    item?["dispense_message"]?.GetValue<string>() ?? "Enjoy your item!",
                    item?["examine_message"]?.GetValue<string>() ?? "A standard vending item.",
                    node?["stock_level"]?.GetValue<int>() ?? 0,
                    node?["max_capacity"]?.GetValue<int>() ?? 15
                );
            }
        }
        catch
        {
        }
        return dt;
    }

    /// <summary>
    /// Links an existing item from the master catalog to a specific vending machine slot.
    /// </summary>
    public bool AddItemToMachineSlot(int machineId, string slotId, int itemId, int stock, decimal? slotPrice = null)
    {
        try
        {
            if (!TryValidateStockValues(stock, 15, out string stockError))
            {
                System.Windows.MessageBox.Show(stockError, "Invalid Stock",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (!TryValidateSlotForMachine(machineId, slotId, out string normalizedSlotId, out string slotError))
            {
                System.Windows.MessageBox.Show(slotError, "Invalid Slot",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            try
            {
                Run(_client.PostAsync("machine_inventory", new
                {
                    machine_id = machineId,
                    item_id = itemId,
                    slot_id = normalizedSlotId,
                    stock_level = stock,
                    max_capacity = 15,
                    slot_price = ToDbNumber(slotPrice)
                }));
            }
            catch when (!slotPrice.HasValue)
            {
                Run(_client.PostAsync("machine_inventory", new
                {
                    machine_id = machineId,
                    item_id = itemId,
                    slot_id = normalizedSlotId,
                    stock_level = stock,
                    max_capacity = 15
                }));
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to link item to slot: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Creates a brand new item in the master catalog AND links it to a vending machine slot.
    /// </summary>
    public bool AddNewItemToMachine(int machineId, string slotId, string name, string type, decimal price, int calories, int stock, int maxCap, string imagePath = "/Assets/Placeholder.png", string dispenseMessage = "Enjoy your item!", string examineMessage = "A standard vending item.", decimal? slotPrice = null)
    {
        try
        {
            if (!TryValidateStockValues(stock, maxCap, out string stockError))
            {
                System.Windows.MessageBox.Show(stockError, "Invalid Stock",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (!TryValidateSlotForMachine(machineId, slotId, out string normalizedSlotId, out string slotError))
            {
                System.Windows.MessageBox.Show(slotError, "Invalid Slot",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (TryFindDuplicateCatalogItemName(name, out _))
            {
                System.Windows.MessageBox.Show(
                    "An item with the same name already exists in the global catalog. Reuse the existing item or rename this one.",
                    "Duplicate Item",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            // 1. Create new master item
            var inserted = Run(_client.PostAsync("items", new
            {
                name,
                type,
                price,
                calories,
                image_path = imagePath,
                dispense_message = dispenseMessage,
                examine_message = examineMessage
            }));

            if (inserted.Count == 0) return false;
            int itemId = inserted[0]?["item_id"]?.GetValue<int>() ?? 0;

            try
            {
                // 2. Link to slot
                try
                {
                    Run(_client.PostAsync("machine_inventory", new
                    {
                        machine_id = machineId,
                        item_id = itemId,
                        slot_id = normalizedSlotId,
                        stock_level = stock,
                        max_capacity = maxCap,
                        slot_price = ToDbNumber(slotPrice)
                    }));
                }
                catch when (!slotPrice.HasValue)
                {
                    Run(_client.PostAsync("machine_inventory", new
                    {
                        machine_id = machineId,
                        item_id = itemId,
                        slot_id = normalizedSlotId,
                        stock_level = stock,
                        max_capacity = maxCap
                    }));
                }
            }
            catch
            {
                if (itemId > 0)
                {
                    try
                    {
                        Run(_client.DeleteAsync("items", $"item_id=eq.{itemId}"));
                    }
                    catch
                    {
                    }
                }

                throw;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to add new item: " + ex.Message);
            return false;
        }
    }

    public bool RestockInventoryItem(int inventoryId, int quantity)
    {
        try
        {
            var rows = Run(_client.GetAsync("machine_inventory",
                $"select=stock_level,max_capacity&inventory_id=eq.{inventoryId}"));

            if (rows.Count == 0) return false;

            int max = rows[0]?["max_capacity"]?.GetValue<int>() ?? 15;
            int stock = rows[0]?["stock_level"]?.GetValue<int>() ?? 0;
            int total = stock + quantity;

            if (total > max)
            {
                System.Windows.MessageBox.Show($"Restock failed: Exceeds max capacity ({max}).",
                    "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            Run(_client.PatchAsync("machine_inventory", $"inventory_id=eq.{inventoryId}",
                new { stock_level = total }));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to restock item: {ex.Message}");
            return false;
        }
    }

    public void UpdateStock(int inventoryId, int newStock)
    {
        Run(_client.PatchAsync("machine_inventory", $"inventory_id=eq.{inventoryId}",
            new { stock_level = newStock }));
    }

    public bool RestockInventoryItemToMax(int inventoryId)
    {
        try
        {
            var rows = Run(_client.GetAsync("machine_inventory",
                $"select=max_capacity&inventory_id=eq.{inventoryId}"));

            if (rows.Count == 0)
            {
                return false;
            }

            int maxCapacity = Math.Max(0, rows[0]?["max_capacity"]?.GetValue<int>() ?? 15);
            Run(_client.PatchAsync("machine_inventory", $"inventory_id=eq.{inventoryId}",
                new { stock_level = maxCapacity }));

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateMachineInventoryAssignment(int inventoryId, int machineId, string slotId, int itemId, int stock, int maxCap, decimal? slotPrice)
    {
        try
        {
            if (!TryValidateStockValues(stock, maxCap, out string stockError))
            {
                System.Windows.MessageBox.Show(stockError, "Invalid Stock",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (!TryValidateSlotForMachine(machineId, slotId, out string normalizedSlotId, out string slotError, inventoryId))
            {
                System.Windows.MessageBox.Show(slotError, "Invalid Slot",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            // Update machine slot
            try
            {
                Run(_client.PatchAsync("machine_inventory", $"inventory_id=eq.{inventoryId}", new
                {
                    item_id = itemId,
                    slot_id = normalizedSlotId,
                    stock_level = stock,
                    max_capacity = maxCap,
                    slot_price = ToDbNumber(slotPrice)
                }));
            }
            catch when (!slotPrice.HasValue)
            {
                Run(_client.PatchAsync("machine_inventory", $"inventory_id=eq.{inventoryId}", new
                {
                    item_id = itemId,
                    slot_id = normalizedSlotId,
                    stock_level = stock,
                    max_capacity = maxCap
                }));
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to update item: " + ex.Message);
            return false;
        }
    }

    public bool DeleteInventoryItem(int inventoryId)
    {
        try
        {
            // Remove ONLY from machine inventory.
            // The item remains in the global 'items' catalog for other machines to use.
            Run(_client.DeleteAsync("machine_inventory", $"inventory_id=eq.{inventoryId}"));
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to delete item: " + ex.Message);
            return false;
        }
    }

    // ══════════════════════════════════════════════════
    //  EVENT LOGS
    // ══════════════════════════════════════════════════

    public System.Data.DataTable GetEventLogs()
    {
        var dt = new System.Data.DataTable();
        try
        {
            var rows = Run(_client.GetAsync("event_logs",
                "select=log_id,log_date,event_type,description&order=log_date.desc&limit=100"));

            dt.Columns.Add("Log ID", typeof(int));
            dt.Columns.Add("Timestamp", typeof(DateTime));
            dt.Columns.Add("Event", typeof(string));
            dt.Columns.Add("Notes", typeof(string));

            foreach (var node in rows)
            {
                dt.Rows.Add(
                    node?["log_id"]?.GetValue<int>() ?? 0,
                    DateTime.Parse(node?["log_date"]?.GetValue<string>() ?? DateTime.Now.ToString()),
                    node?["event_type"]?.GetValue<string>() ?? "",
                    node?["description"]?.GetValue<string>() ?? ""
                );
            }
        }
        catch { }
        return dt;
    }

    public System.Data.DataTable GetFilteredEventLogs(DateTime date, string filterType)
    {
        var dt = new System.Data.DataTable();
        try
        {
            string filter = BuildDateFilter("log_date", date, filterType);
            var rows = Run(_client.GetAsync("event_logs",
                $"select=log_id,log_date,event_type,description&{filter}&order=log_date.desc"));

            dt.Columns.Add("Log ID", typeof(int));
            dt.Columns.Add("Timestamp", typeof(DateTime));
            dt.Columns.Add("Event", typeof(string));
            dt.Columns.Add("Notes", typeof(string));

            foreach (var node in rows)
            {
                dt.Rows.Add(
                    node?["log_id"]?.GetValue<int>() ?? 0,
                    DateTime.Parse(node?["log_date"]?.GetValue<string>() ?? DateTime.Now.ToString()),
                    node?["event_type"]?.GetValue<string>() ?? "",
                    node?["description"]?.GetValue<string>() ?? ""
                );
            }
        }
        catch { }
        return dt;
    }

    public void LogEvent(string eventType, string details, decimal amount = 0m, int machineId = 1)
    {
        try
        {
            Run(_client.PostAsync("event_logs", new
            {
                event_type = eventType,
                description = details,
                machine_id = machineId
            }));
        }
        catch { }
    }

    public bool EventLogExists(string clientSyncId)
    {
        try
        {
            var rows = Run(_client.GetAsync("event_logs",
                $"select=log_id&client_sync_id=eq.{Uri.EscapeDataString(clientSyncId)}&limit=1"));
            return rows.Count > 0;
        }
        catch (Exception ex)
        {
            throw BuildClientSyncColumnException("event_logs", ex);
        }
    }

    public void InsertQueuedEventLog(string clientSyncId, string eventType, string details, int machineId, DateTime occurredUtc)
    {
        try
        {
            Run(_client.PostAsync("event_logs", new
            {
                event_type = eventType,
                description = details,
                machine_id = machineId,
                log_date = occurredUtc.ToUniversalTime().ToString("O"),
                client_sync_id = clientSyncId
            }));
        }
        catch (Exception ex)
        {
            throw BuildClientSyncColumnException("event_logs", ex);
        }
    }

    public void ClearEventLogs()
    {
        try
        {
            // Delete all event logs (PostgREST needs a filter, use a tautology)
            Run(_client.DeleteAsync("event_logs", "log_id=gt.0"));
        }
        catch { }
    }

    // ══════════════════════════════════════════════════
    //  SALES & DASHBOARD
    // ══════════════════════════════════════════════════

    public void RecordSale(int machineId, int inventoryId, decimal amountPaid)
    {
        try
        {
            // Get item_id from machine_inventory
            var rows = Run(_client.GetAsync("machine_inventory",
                $"select=item_id&inventory_id=eq.{inventoryId}"));
            if (rows.Count == 0) return;

            int itemId = rows[0]?["item_id"]?.GetValue<int>() ?? 0;

            Run(_client.PostAsync("sales_transactions", new
            {
                machine_id = machineId,
                item_id = itemId,
                amount_paid = amountPaid
            }));
        }
        catch { }
    }

    public bool SaleExists(string clientSyncId)
    {
        try
        {
            var rows = Run(_client.GetAsync("sales_transactions",
                $"select=transaction_id&client_sync_id=eq.{Uri.EscapeDataString(clientSyncId)}&limit=1"));
            return rows.Count > 0;
        }
        catch (Exception ex)
        {
            throw BuildClientSyncColumnException("sales_transactions", ex);
        }
    }

    public void InsertQueuedSale(string clientSyncId, int machineId, int itemId, decimal amountPaid, DateTime occurredUtc)
    {
        try
        {
            Run(_client.PostAsync("sales_transactions", new
            {
                machine_id = machineId,
                item_id = itemId,
                amount_paid = amountPaid,
                transaction_date = occurredUtc.ToUniversalTime().ToString("O"),
                client_sync_id = clientSyncId
            }));
        }
        catch (Exception ex)
        {
            throw BuildClientSyncColumnException("sales_transactions", ex);
        }
    }

    public bool ReceiptSessionExists(string clientSyncId)
    {
        var rows = Run(_client.GetAsync("receipt_sessions",
            $"select=receipt_session_id&client_sync_id=eq.{Uri.EscapeDataString(clientSyncId)}&limit=1"));
        return rows.Count > 0;
    }

    public void InsertQueuedReceiptSession(Transaction transaction)
    {
        var inserted = Run(_client.PostAsync("receipt_sessions", new
        {
            client_sync_id = transaction.ClientSyncId,
            receipt_number = transaction.ReceiptNumber,
            machine_id = transaction.MachineId,
            session_started_at = transaction.SessionStartedAt.ToUniversalTime().ToString("O"),
            session_ended_at = transaction.SessionEndedAt.ToUniversalTime().ToString("O"),
            total_amount = transaction.TotalAmount,
            amount_paid = transaction.AmountPaid,
            change_amount = transaction.Change,
            recycle_points_total = transaction.RecyclePointsTotal,
            source = transaction.Source
        }));

        if (inserted.Count == 0)
        {
            throw new InvalidOperationException("Receipt session insert did not return a session id.");
        }

        int receiptSessionId = inserted[0]?["receipt_session_id"]?.GetValue<int>() ?? 0;
        if (receiptSessionId <= 0)
        {
            throw new InvalidOperationException("Receipt session insert returned an invalid session id.");
        }

        int lineOrder = 1;
        foreach (var item in transaction.Items)
        {
            Run(_client.PostAsync("receipt_session_lines", new
            {
                receipt_session_id = receiptSessionId,
                line_order = lineOrder++,
                entry_type = "sale",
                slot_id = item.SlotId,
                item_name = item.ProductName,
                quantity = item.Quantity,
                unit_price = item.UnitPrice,
                line_total = item.LineTotal
            }));
        }

        foreach (var recycle in transaction.RecycledItems)
        {
            Run(_client.PostAsync("receipt_session_lines", new
            {
                receipt_session_id = receiptSessionId,
                line_order = lineOrder++,
                entry_type = "recycle",
                recycle_item_id = recycle.RecyclableItemId > 0 ? (int?)recycle.RecyclableItemId : null,
                recycle_display_name = recycle.DisplayName,
                recycle_material_type = recycle.MaterialType,
                recycle_unit_label = recycle.UnitLabel,
                recycle_points_per_unit = recycle.PointsPerUnit,
                recycle_material = recycle.DisplayName,
                recycle_pieces = recycle.Pieces,
                recycle_points = recycle.TotalPoints
            }));
        }
    }

    public (decimal Daily, decimal Weekly, decimal Monthly, decimal Yearly) GetSalesTotals()
    {
        try
        {
            var now = DateTime.UtcNow;
            var rows = Run(_client.GetAsync("sales_transactions", "select=amount_paid,transaction_date"));

            decimal daily = 0, weekly = 0, monthly = 0, yearly = 0;

            foreach (var node in rows)
            {
                decimal amt = node?["amount_paid"]?.GetValue<decimal>() ?? 0m;
                var dateStr = node?["transaction_date"]?.GetValue<string>();
                if (dateStr == null) continue;
                var txDate = DateTime.Parse(dateStr);

                if (txDate.Date == now.Date) daily += amt;
                if (IsSameWeek(txDate, now)) weekly += amt;
                if (txDate.Year == now.Year && txDate.Month == now.Month) monthly += amt;
                if (txDate.Year == now.Year) yearly += amt;
            }

            return (daily, weekly, monthly, yearly);
        }
        catch
        {
            return (0m, 0m, 0m, 0m);
        }
    }

    public (System.Data.DataTable Data, decimal Total) GetFilteredSales(DateTime date, string filterType, int? machineId = null)
    {
        var dt = new System.Data.DataTable();
        decimal total = 0m;
        try
        {
            string machineFilter = machineId.HasValue ? $"&machine_id=eq.{machineId.Value}" : string.Empty;
            var rows = Run(_client.GetAsync("sales_transactions",
                $"select=transaction_id,transaction_date,machine_id,item_id,amount_paid,vending_machines(location_name),items(name,price){machineFilter}&order=transaction_date.desc"));

            dt.Columns.Add("TX ID", typeof(int));
            dt.Columns.Add("Date", typeof(DateTime));
            dt.Columns.Add("Period", typeof(string));
            dt.Columns.Add("Machine", typeof(string));
            dt.Columns.Add("Item", typeof(string));
            dt.Columns.Add("Quantity", typeof(int));
            dt.Columns.Add("Price", typeof(decimal));
            dt.Columns.Add("Total Paid", typeof(decimal));

            foreach (var node in rows)
            {
                DateTime txDate = ParseSupabaseLocalDateTime(node?["transaction_date"]?.GetValue<string>());
                if (!IsDateInFilter(txDate, date, filterType))
                {
                    continue;
                }

                decimal paid = node?["amount_paid"]?.GetValue<decimal>() ?? 0m;
                total += paid;

                dt.Rows.Add(
                    node?["transaction_id"]?.GetValue<int>() ?? 0,
                    txDate,
                    BuildSalesPeriodLabel(txDate, filterType),
                    node?["vending_machines"]?["location_name"]?.GetValue<string>() ?? "",
                    node?["items"]?["name"]?.GetValue<string>() ?? "",
                    1,
                    node?["items"]?["price"]?.GetValue<decimal>() ?? 0m,
                    paid
                );
            }
        }
        catch { }
        return (dt, total);
    }

    public System.Data.DataTable GetStockMonitoring()
    {
        var dt = new System.Data.DataTable();
        dt.Columns.Add("Machine", typeof(string));
        dt.Columns.Add("Slot", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Type", typeof(string));
        dt.Columns.Add("Stock", typeof(int));
        dt.Columns.Add("Max Capacity", typeof(int));
        dt.Columns.Add("Fill %", typeof(decimal));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Reorder Qty", typeof(int));

        try
        {
            var rows = Run(_client.GetAsync("machine_inventory",
                "select=slot_id,stock_level,max_capacity,vending_machines(location_name),items(name,type)&order=stock_level.asc"));

            foreach (var node in rows)
            {
                int stock = node?["stock_level"]?.GetValue<int>() ?? 0;
                int maxCapacity = Math.Max(1, node?["max_capacity"]?.GetValue<int>() ?? 15);
                decimal fillPercent = Math.Round((decimal)stock / maxCapacity * 100m, 1);
                string status = stock <= 0
                    ? "OUT OF STOCK"
                    : stock <= 3
                        ? "LOW STOCK"
                        : fillPercent <= 40m
                            ? "WATCH"
                            : "OK";

                dt.Rows.Add(
                    node?["vending_machines"]?["location_name"]?.GetValue<string>() ?? "Machine",
                    SlotIdHelper.Normalize(node?["slot_id"]?.GetValue<string>() ?? "") ?? "",
                    node?["items"]?["name"]?.GetValue<string>() ?? "Unknown",
                    node?["items"]?["type"]?.GetValue<string>() ?? "Misc",
                    stock,
                    maxCapacity,
                    fillPercent,
                    status,
                    Math.Max(0, maxCapacity - stock)
                );
            }
        }
        catch
        {
        }

        return dt;
    }

    public void GetDashboardMetrics(out decimal totalSales, out int totalItemsSold, out int lowStockAlerts, out int activeMachines)
    {
        totalSales = 0m;
        totalItemsSold = 0;
        lowStockAlerts = 0;
        activeMachines = 0;

        try
        {
            // Total sales
            var salesRows = Run(_client.GetAsync("sales_transactions", "select=amount_paid"));
            foreach (var node in salesRows)
            {
                totalSales += node?["amount_paid"]?.GetValue<decimal>() ?? 0m;
                totalItemsSold++;
            }

            // Low stock alerts
            var lowStockRows = Run(_client.GetAsync("machine_inventory", "select=inventory_id&stock_level=lte.3"));
            lowStockAlerts = lowStockRows.Count;

            // Active machines
            var activeRows = Run(_client.GetAsync("vending_machines", "select=machine_id&status=eq.Active"));
            activeMachines = activeRows.Count;
        }
        catch { }
    }

    // ══════════════════════════════════════════════════
    //  HELPER: Date Filtering for PostgREST
    // ══════════════════════════════════════════════════

    private static string BuildDateFilter(string column, DateTime date, string filterType)
    {
        return filterType switch
        {
            "Day" => $"{column}=gte.{date.Date:yyyy-MM-dd}T00:00:00&{column}=lt.{date.Date.AddDays(1):yyyy-MM-dd}T00:00:00",
            "Week" =>
                BuildWeekFilter(column, date),
            "Month" => $"{column}=gte.{new DateTime(date.Year, date.Month, 1):yyyy-MM-dd}T00:00:00&{column}=lt.{new DateTime(date.Year, date.Month, 1).AddMonths(1):yyyy-MM-dd}T00:00:00",
            "Year" => $"{column}=gte.{date.Year}-01-01T00:00:00&{column}=lt.{date.Year + 1}-01-01T00:00:00",
            "All Time" => "", // No filter
            _ => $"{column}=gte.{date.Date:yyyy-MM-dd}T00:00:00&{column}=lt.{date.Date.AddDays(1):yyyy-MM-dd}T00:00:00"
        };
    }

    private static string BuildWeekFilter(string column, DateTime date)
    {
        // ISO week: Monday to Sunday
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = date.Date.AddDays(-diff);
        var endOfWeek = startOfWeek.AddDays(7);
        return $"{column}=gte.{startOfWeek:yyyy-MM-dd}T00:00:00&{column}=lt.{endOfWeek:yyyy-MM-dd}T00:00:00";
    }

    private static bool IsSameWeek(DateTime a, DateTime b)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        return cal.GetWeekOfYear(a, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) ==
               cal.GetWeekOfYear(b, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) &&
               a.Year == b.Year;
    }

    private static DateTime ParseSupabaseLocalDateTime(string? value)
    {
        if (DateTimeOffset.TryParse(value, out var offset))
        {
            return offset.ToLocalTime().DateTime;
        }

        return DateTime.TryParse(value, out var parsed) ? parsed : DateTime.Now;
    }

    private static bool IsDateInFilter(DateTime txDate, DateTime selectedDate, string filterType)
    {
        selectedDate = selectedDate.Date;
        return filterType switch
        {
            "Day" => txDate.Date == selectedDate,
            "Week" => GetWeekStart(txDate.Date) == GetWeekStart(selectedDate),
            "Month" => txDate.Year == selectedDate.Year && txDate.Month == selectedDate.Month,
            "Year" => txDate.Year == selectedDate.Year,
            "All Time" => true,
            _ => txDate.Date == selectedDate
        };
    }

    private static string BuildSalesPeriodLabel(DateTime txDate, string filterType)
    {
        return filterType switch
        {
            "Day" => txDate.ToString("HH:00"),
            "Week" => txDate.ToString("ddd"),
            "Month" => txDate.ToString("MMM dd"),
            "Year" => txDate.ToString("MMM"),
            _ => txDate.ToString("yyyy-MM")
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }
}
