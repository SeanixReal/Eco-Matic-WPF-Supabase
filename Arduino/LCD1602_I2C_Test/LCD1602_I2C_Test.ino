#include <Wire.h>
#include <LiquidCrystal_I2C.h>

// Simple LCD1602 I2C test for Arduino Uno/Nano.
//
// Wiring for Arduino Uno/Nano:
// LCD VCC -> 5V
// LCD GND -> GND
// LCD SDA -> A4
// LCD SCL -> A5
//
// Open Serial Monitor at 9600 baud.

static const int LCD_COLUMNS = 16;
static const int LCD_ROWS = 2;

uint8_t lcdAddress = 0x27;
LiquidCrystal_I2C *lcd = nullptr;

uint8_t scanI2cAddress() {
  Serial.println("Scanning I2C bus...");

  uint8_t firstAddress = 0;
  for (uint8_t address = 1; address < 127; address++) {
    Wire.beginTransmission(address);
    byte error = Wire.endTransmission();

    if (error == 0) {
      Serial.print("I2C device found at 0x");
      if (address < 16) {
        Serial.print("0");
      }
      Serial.println(address, HEX);

      if (address == 0x27 || address == 0x3F) {
        return address;
      }

      if (firstAddress == 0) {
        firstAddress = address;
      }
    }
  }

  return firstAddress;
}

void showMessage(const char *line1, const char *line2) {
  lcd->clear();
  lcd->setCursor(0, 0);
  lcd->print(line1);
  lcd->setCursor(0, 1);
  lcd->print(line2);
}

void setup() {
  Serial.begin(9600);
  Wire.begin();
  delay(300);

  lcdAddress = scanI2cAddress();
  if (lcdAddress == 0) {
    Serial.println("No I2C LCD found. Check VCC, GND, SDA, SCL.");
    while (true) {
      delay(1000);
    }
  }

  Serial.print("Using LCD address 0x");
  if (lcdAddress < 16) {
    Serial.print("0");
  }
  Serial.println(lcdAddress, HEX);

  lcd = new LiquidCrystal_I2C(lcdAddress, LCD_COLUMNS, LCD_ROWS);
  lcd->init();
  lcd->backlight();

  showMessage("LCD1602 TEST", "HELLO ECO-MATIC");
  Serial.println("LCD test started.");
}

void loop() {
  showMessage("LCD1602 TEST", "TEXT VISIBLE?");
  delay(1200);

  showMessage("ADJUST CONTRAST", "BLUE SCREW");
  delay(1200);

  showMessage("ADDR DETECTED", lcdAddress == 0x27 ? "0x27" : (lcdAddress == 0x3F ? "0x3F" : "OTHER ADDRESS"));
  delay(1200);
}
