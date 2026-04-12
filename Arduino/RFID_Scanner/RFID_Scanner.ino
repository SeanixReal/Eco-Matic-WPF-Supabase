#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

#define RST_PIN 9
#define SS_PIN 10

#define GREEN_LED 6
#define RED_LED 7

// LCD instance (Address 0x27 is most common, 16 columns, 2 rows)
LiquidCrystal_I2C lcd(0x27, 16, 2);
MFRC522 mfrc522(SS_PIN, RST_PIN);

unsigned long messageTimer = 0;
bool showingMessage = false;

void setup() {
  Serial.begin(9600);
  while (!Serial);
  
  SPI.begin();
  mfrc522.PCD_Init();
  
  pinMode(GREEN_LED, OUTPUT);
  pinMode(RED_LED, OUTPUT);

  // Initialize LCD
  lcd.init();
  lcd.backlight();
  resetDisplay();
  
  Serial.println("Eco-Matic RFID & LCD Scanner Ready.");
}

void loop() {
  // Clear message and turn off LEDs after 3 seconds
  if (showingMessage && millis() - messageTimer > 3000) {
    resetDisplay();
  }

  // Look for incoming messages from the C# WPF Application
  if (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    msg.trim();
    
    if (msg == "VALID") {
      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("Access Granted!");
      lcd.setCursor(0, 1);
      lcd.print("Welcome Back");
      digitalWrite(GREEN_LED, HIGH);
      digitalWrite(RED_LED, LOW);
      showingMessage = true;
      messageTimer = millis();
    } 
    else if (msg == "INVALID") {
      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("Unknown Card");
      lcd.setCursor(0, 1);
      lcd.print("Not Registered");
      digitalWrite(GREEN_LED, LOW);
      digitalWrite(RED_LED, HIGH);
      showingMessage = true;
      messageTimer = millis();
    }
  }

  // Look for new RFID cards
  if (!mfrc522.PICC_IsNewCardPresent() || !mfrc522.PICC_ReadCardSerial()) {
    return;
  }

  // If a card is tapped, show a loading state
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Checking...");
  digitalWrite(GREEN_LED, LOW);
  digitalWrite(RED_LED, LOW);

  // Send UID to PC
  Serial.print("RFID:");
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    Serial.print(mfrc522.uid.uidByte[i] < 0x10 ? "0" : "");
    Serial.print(mfrc522.uid.uidByte[i], HEX);
  } 
  Serial.println();

  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();
}

void resetDisplay() {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Eco-Matic Ready");
  lcd.setCursor(0, 1);
  lcd.print("Please Tap Card");
  digitalWrite(GREEN_LED, LOW);
  digitalWrite(RED_LED, LOW);
  showingMessage = false;
}
