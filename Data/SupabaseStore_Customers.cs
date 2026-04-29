using System;
using System.Text.Json;

namespace Eco_Matic.Data
{
    /// <summary>
    /// Customer operations for Supabase (partial class of SupabaseStore).
    /// Handles RFID-based customers for recycling/eco-credits.
    /// ESP32 with RFID reader will hit the same Supabase tables.
    /// </summary>
    public partial class SupabaseStore
    {
        // Table is created via migration, no need for EnsureCustomerTableExists anymore.
        // Kept as a no-op for compatibility.
        public void EnsureCustomerTableExists()
        {
            // No-op: table is managed by Supabase migrations
        }

        public bool CustomerExists(string rfid)
        {
            try
            {
                var rows = Run(_client.GetAsync("customers",
                    $"select=rfid_tag&rfid_tag=eq.{Uri.EscapeDataString(rfid)}"));
                return rows.Count > 0;
            }
            catch { return false; }
        }

        public string? AuthenticateCustomer(string email, string pass)
        {
            try
            {
                var rows = Run(_client.GetAsync("customers",
                    $"select=rfid_tag&email=eq.{Uri.EscapeDataString(email)}&password_hash=eq.{Uri.EscapeDataString(pass)}"));

                if (rows.Count > 0)
                {
                    return rows[0]?["rfid_tag"]?.GetValue<string>();
                }
            }
            catch { }
            return null;
        }

        public bool RegisterCustomer(string rfid, string email, string pass)
        {
            try
            {
                var result = Run(_client.PostAsync("customers", new
                {
                    rfid_tag = rfid,
                    email,
                    password_hash = pass
                }));
                if (result.Count > 0)
                {
                    LogEvent("CUSTOMER_REGISTERED", $"Registered customer account {email} with RFID ({rfid}).");
                }
                return result.Count > 0;
            }
            catch { return false; }
        }

        public (string Email, int EcoCredits) GetCustomerInfo(string rfid)
        {
            try
            {
                var rows = Run(_client.GetAsync("customers",
                    $"select=email,eco_credits&rfid_tag=eq.{Uri.EscapeDataString(rfid)}"));

                if (rows.Count > 0)
                {
                    string email = rows[0]?["email"]?.GetValue<string>() ?? "";
                    int credits = rows[0]?["eco_credits"]?.GetValue<int>() ?? 0;
                    return (email, credits);
                }
            }
            catch { }
            return ("", 0);
        }

        public System.Data.DataTable GetCustomers()
        {
            var dt = new System.Data.DataTable();
            try
            {
                var rows = Run(_client.GetAsync("customers",
                    "select=rfid_tag,email,eco_credits,registered_date&order=registered_date.desc"));

                dt.Columns.Add("RFID", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Points", typeof(int));
                dt.Columns.Add("Joined", typeof(DateTime));

                foreach (var node in rows)
                {
                    dt.Rows.Add(
                        node?["rfid_tag"]?.GetValue<string>() ?? "",
                        node?["email"]?.GetValue<string>() ?? "",
                        node?["eco_credits"]?.GetValue<int>() ?? 0,
                        DateTime.Parse(node?["registered_date"]?.GetValue<string>() ?? DateTime.Now.ToString())
                    );
                }
            }
            catch { }
            return dt;
        }

        public bool UpdateCustomerCredits(string rfid, int newCredits)
        {
            try
            {
                var current = GetCustomerInfo(rfid);
                Run(_client.PatchAsync("customers",
                    $"rfid_tag=eq.{Uri.EscapeDataString(rfid)}",
                    new { eco_credits = newCredits }));
                string customerLabel = string.IsNullOrWhiteSpace(current.Email) ? "customer account" : current.Email;
                LogEvent("CUSTOMER_CREDIT_UPDATED", $"Updated eco-credit balance for {customerLabel} RFID ({rfid}): {current.EcoCredits} -> {newCredits}.");
                return true;
            }
            catch { return false; }
        }

        public bool DeleteCustomer(string rfid)
        {
            try
            {
                var current = GetCustomerInfo(rfid);
                Run(_client.DeleteAsync("customers",
                    $"rfid_tag=eq.{Uri.EscapeDataString(rfid)}"));
                string customerLabel = string.IsNullOrWhiteSpace(current.Email) ? "customer account" : current.Email;
                LogEvent("CUSTOMER_DELETED", $"Deleted {customerLabel} RFID ({rfid}); previous balance {current.EcoCredits}.");
                return true;
            }
            catch { return false; }
        }
    }
}
