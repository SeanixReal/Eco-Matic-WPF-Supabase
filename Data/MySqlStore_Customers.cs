using System;
using MySql.Data.MySqlClient;

namespace Eco_Matic.Data
{
    public partial class MySqlStore
    {
        public void EnsureCustomerTableExists()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string query = @"CREATE TABLE IF NOT EXISTS customers (
                                    rfid_tag VARCHAR(50) PRIMARY KEY,
                                    email VARCHAR(100) UNIQUE NOT NULL,
                                    password_hash VARCHAR(255) NOT NULL,
                                    eco_credits INT DEFAULT 0,
                                    registered_date DATETIME DEFAULT CURRENT_TIMESTAMP
                                 );";
                using var cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public bool CustomerExists(string rfid)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new MySqlCommand("SELECT COUNT(*) FROM customers WHERE rfid_tag = @r", conn);
                cmd.Parameters.AddWithValue("@r", rfid);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch { return false; }
        }

        public bool RegisterCustomer(string rfid, string email, string pass)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new MySqlCommand("INSERT INTO customers (rfid_tag, email, password_hash) VALUES (@r, @e, @p)", conn);
                cmd.Parameters.AddWithValue("@r", rfid);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", pass);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public (string Email, int EcoCredits) GetCustomerInfo(string rfid)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new MySqlCommand("SELECT email, eco_credits FROM customers WHERE rfid_tag = @r", conn);
                cmd.Parameters.AddWithValue("@r", rfid);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return (reader.GetString("email"), reader.GetInt32("eco_credits"));
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
                using var conn = GetConnection();
                conn.Open();
                string query = "SELECT rfid_tag as 'RFID', email as 'Email', eco_credits as 'Points', registered_date as 'Joined' FROM customers ORDER BY registered_date DESC";
                using var adapter = new MySqlDataAdapter(query, conn);
                adapter.Fill(dt);
            }
            catch { }
            return dt;
        }

        public bool UpdateCustomerCredits(string rfid, int newCredits)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new MySqlCommand("UPDATE customers SET eco_credits = @c WHERE rfid_tag = @r", conn);
                cmd.Parameters.AddWithValue("@c", newCredits);
                cmd.Parameters.AddWithValue("@r", rfid);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool DeleteCustomer(string rfid)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM customers WHERE rfid_tag = @r", conn);
                cmd.Parameters.AddWithValue("@r", rfid);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }
    }
}
// temp
