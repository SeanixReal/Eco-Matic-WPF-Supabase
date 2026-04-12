#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

#define RST_PIN 9
#define SS_PIN 10

#define GREEN_LED 6
#define RED_LED 7

LiquidCrystal_I2C lcd(0x27, 16, 2);
MFRC522 mfrc522(SS_PIN, RST_PIN);

unsigned long timer = 0;
unsigned long messageTimer = 0;
bool showingMessage = false;

// 0 = AFK Mode (Screensaver/Facts), 1 = Active Vending Mode
int systemMode = 0; 
int afkFrame = 0;

// Fun facts for AFK Mode (MAX 16 CHARS PER LINE)
const char* facts0[] = {"Eco-Matic System", "   Welcome!     "};
const char* facts1[] = {"1 Plastic Bottle", "60W Bulb 3 Hours"};
const char* facts2[] = {"1 Aluminum Can  ", "Powers TV 3 Hrs "};
const char* facts3[] = {"Save The Earth  ", "1 Item at a Time"};
const char* facts4[] = {"Help Environment", "Start Recycling!"};
const char* facts5[] = {"Reduce and Reuse", "Recycle Daily!  "};

void setup() {
  Serial.begin(9600);
  while (!Serial);
  
  SPI.begin();
  mfrc522.PCD_Init();
  
  pinMode(GREEN_LED, OUTPUT);
  pinMode(RED_LED, OUTPUT);

  lcd.init();
  lcd.backlight();
  resetDisplay();
  
  Serial.println("Eco-Matic RFID & LCD Scanner Ready.");
}

void loop() {
  // 1. Listen for State Changes from C# App
  if (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    msg.trim();
    
    if (msg == "STATE:ACTIVE") {
      systemMode = 1;
      resetDisplay();
    }
    else if (msg == "STATE:AFK") {
      systemMode = 0;
      resetDisplay();
    }
    else if (systemMode == 1 && msg == "VALID") {
      lcd.clear();
      lcd.setCursor(0, 0); lcd.print("Access Granted!");
      lcd.setCursor(0, 1); lcd.print("Welcome Back");
      digitalWrite(GREEN_LED, HIGH);
      digitalWrite(RED_LED, LOW);
      showingMessage = true;
      messageTimer = millis();
    } 
    else if (systemMode == 1 && msg == "INVALID") {
      lcd.clear();
      lcd.setCursor(0, 0); lcd.print("Unknown Card");
      lcd.setCursor(0, 1); lcd.print("Not Registered");
      digitalWrite(GREEN_LED, LOW);
      digitalWrite(RED_LED, HIGH);
      showingMessage = true;
      messageTimer = millis();
    }
  }

  // 2. Mode Behaviours
  if (systemMode == 0) {
    // --- AFK MODE ---
    if (millis() - timer > 3000) { // Change screen every 3 seconds
      timer = millis();
      afkFrame = (afkFrame + 1) % 6; // Changed from 4 to 6
      
      lcd.clear();
      if(afkFrame == 0) { lcd.setCursor(0,0); lcd.print(facts0[0]); lcd.setCursor(0,1); lcd.print(facts0[1]); }
      if(afkFrame == 1) { lcd.setCursor(0,0); lcd.print(facts1[0]); lcd.setCursor(0,1); lcd.print(facts1[1]); }
      if(afkFrame == 2) { lcd.setCursor(0,0); lcd.print(facts2[0]); lcd.setCursor(0,1); lcd.print(facts2[1]); }
      if(afkFrame == 3) { lcd.setCursor(0,0); lcd.print(facts3[0]); lcd.setCursor(0,1); lcd.print(facts3[1]); }
      if(afkFrame == 4) { lcd.setCursor(0,0); lcd.print(facts4[0]); lcd.setCursor(0,1); lcd.print(facts4[1]); }
      if(afkFrame == 5) { lcd.setCursor(0,0); lcd.print(facts5[0]); lcd.setCursor(0,1); lcd.print(facts5[1]); }

      // Cool alternating LED blink
      digitalWrite(GREEN_LED, afkFrame % 2 == 0 ? HIGH : LOW);
      digitalWrite(RED_LED, afkFrame % 2 != 0 ? HIGH : LOW);
    }
    return; // Skip RFID scanning in AFK mode
  }
  else {
    // --- ACTIVE VENDING MODE ---
    if (showingMessage && millis() - messageTimer > 3000) {
      resetDisplay();
    }

    if (!mfrc522.PICC_IsNewCardPresent() || !mfrc522.PICC_ReadCardSerial()) {
      return; // No card
    }

    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("Checking...");
    digitalWrite(GREEN_LED, LOW);
    digitalWrite(RED_LED, LOW);

    Serial.print("RFID:");
    for (byte i = 0; i < mfrc522.uid.size; i++) {
      Serial.print(mfrc522.uid.uidByte[i] < 0x10 ? "0" : "");
      Serial.print(mfrc522.uid.uidByte[i], HEX);
    } 
    Serial.println();

    mfrc522.PICC_HaltA();
    mfrc522.PCD_StopCrypto1();
  }
}

void resetDisplay() {
  lcd.clear();
  lcd.setCursor(0, 0);
  if (systemMode == 1) {
    lcd.print("Eco-Matic Ready");
    lcd.setCursor(0, 1);
    lcd.print("Please Tap Card");
  } else {
    lcd.print("Eco-Matic Sleeping");
  }
  digitalWrite(GREEN_LED, LOW);
  digitalWrite(RED_LED, LOW);
  showingMessage = false;
  timer = millis();
}
