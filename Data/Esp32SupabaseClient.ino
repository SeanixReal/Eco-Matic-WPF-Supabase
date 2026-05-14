#include <WiFi.h>
#include <HTTPClient.h>

const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";

// Supabase Configuration
// You can find your URL and Anon Key in your Supabase Dashboard under Settings > API
const char* supabase_url_telemetry = "https://your-project-ref.supabase.co/rest/v1/esp32_telemetry";
const char* supabase_url_commands = "https://your-project-ref.supabase.co/rest/v1/esp32_commands";
const char* supabase_anon_key = "YOUR_SUPABASE_ANON_KEY"; 

void setup() {
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }
  Serial.println("Connected to WiFi!");
}

// --------------------------------------------------------
// Function to POST telemetry data (e.g. sensor readings)
// --------------------------------------------------------
void sendTelemetry(int machineId, float temp, String status) {
  if (WiFi.status() == WL_CONNECTED) {
    HTTPClient http;
    http.begin(supabase_url_telemetry);
    
    // Required Supabase Headers
    http.addHeader("apikey", supabase_anon_key);
    http.addHeader("Authorization", "Bearer " + String(supabase_anon_key));
    http.addHeader("Content-Type", "application/json");
    http.addHeader("Prefer", "return=minimal"); // Don't return the inserted row

    // Construct JSON Payload
    String payload = "{\"machine_id\": " + String(machineId) + 
                     ", \"temperature_c\": " + String(temp) + 
                     ", \"status_code\": \"" + status + "\"}";
    
    int httpResponseCode = http.POST(payload);
    
    Serial.print("Telemetry POST HTTP Response code: ");
    Serial.println(httpResponseCode);
    
    http.end();
  }
}

// --------------------------------------------------------
// Function to GET pending commands from the cloud
// --------------------------------------------------------
void checkCommands(int machineId) {
  if (WiFi.status() == WL_CONNECTED) {
    HTTPClient http;
    
    // Filter by machine_id and pending status
    String url = String(supabase_url_commands) + "?machine_id=eq." + 
                 String(machineId) + "&status=eq.Pending";
                 
    http.begin(url);
    http.addHeader("apikey", supabase_anon_key);
    http.addHeader("Authorization", "Bearer " + String(supabase_anon_key));
    
    int httpResponseCode = http.GET();
    
    if (httpResponseCode > 0) {
      String response = http.getString();
      Serial.println("Pending Commands: " + response);
      // Parse JSON response here to execute the command...
      // E.g., if response is [{"command_id": 12, "action": "DISPENSE", "payload": "ITEM_005"}]
    } else {
      Serial.print("Error code: ");
      Serial.println(httpResponseCode);
    }
    
    http.end();
  }
}

void loop() {
  // Example usage (assuming this ESP32 is running Machine ID 1)
  
  // 1. Send status updates every 60 seconds
  sendTelemetry(1, 24.5, "OK");
  
  // 2. Check for new dispensing commands
  checkCommands(1);
  
  delay(60000); 
}
