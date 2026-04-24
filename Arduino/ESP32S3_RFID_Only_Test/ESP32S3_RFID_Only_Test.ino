#include <SPI.h>
#include <MFRC522.h>

// ESP32-S3 + RC522 RFID-only test.
//
// This ignores WPF, LCD, LEDs, and audio so you can isolate RFID wiring.
//
// Wiring:
// RC522 3.3V    -> ESP32 3V3
// RC522 GND     -> ESP32 GND
// RC522 SDA/SS  -> GPIO10
// RC522 MOSI    -> GPIO11
// RC522 SCK     -> GPIO12
// RC522 MISO    -> GPIO13
// RC522 RST     -> GPIO14
// RC522 IRQ     -> not connected
//
// Open Serial Monitor at 115200 baud.

static const int RC522_SS_PIN   = 10;
static const int RC522_MOSI_PIN = 11;
static const int RC522_SCK_PIN  = 12;
static const int RC522_MISO_PIN = 13;
static const int RC522_RST_PIN  = 14;

MFRC522 rfid(RC522_SS_PIN, RC522_RST_PIN);

void setup() {
  Serial.begin(115200);
  delay(500);

  Serial.println();
  Serial.println("ESP32-S3 RC522 RFID-only test");

  SPI.begin(RC522_SCK_PIN, RC522_MISO_PIN, RC522_MOSI_PIN, RC522_SS_PIN);
  rfid.PCD_Init();
  delay(50);

  Serial.print("Firmware check: ");
  rfid.PCD_DumpVersionToSerial();

  byte version = rfid.PCD_ReadRegister(rfid.VersionReg);
  if (version == 0x00 || version == 0xFF) {
    Serial.println("ERROR: RC522 not detected. Check SPI wiring and 3.3V power.");
  } else {
    Serial.println("RC522 detected. Tap a 13.56 MHz RFID card.");
  }
}

void loop() {
  if (!rfid.PICC_IsNewCardPresent()) {
    delay(50);
    return;
  }

  if (!rfid.PICC_ReadCardSerial()) {
    delay(50);
    return;
  }

  Serial.print("CARD UID: ");
  for (byte i = 0; i < rfid.uid.size; i++) {
    if (rfid.uid.uidByte[i] < 0x10) {
      Serial.print("0");
    }
    Serial.print(rfid.uid.uidByte[i], HEX);
  }
  Serial.println();

  rfid.PICC_HaltA();
  rfid.PCD_StopCrypto1();
  delay(600);
}
