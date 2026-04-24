#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Servo.h>

// Eco-Matic Arduino Uno/Nano RFID + LCD + LED module.
//
// This is the recommended deadline hardware path when the LCD1602 works
// reliably on Arduino at 5V.
//
// Serial protocol used by the WPF app:
//   Arduino -> PC: RFID:A1B2C3D4
//   PC -> Arduino: STATE:ACTIVE
//   PC -> Arduino: STATE:AFK
//   PC -> Arduino: VALID
//   PC -> Arduino: INVALID
//   PC -> Arduino: MSG:<up to 32 chars>
//   PC -> Arduino: SERVO:OPEN

static const int RST_PIN = 9;
static const int SS_PIN = 10;

static const int BUZZER_PIN = 4;
static const int SERVO_PIN = 5;
static const int GREEN_LED = 6;
static const int RED_LED = 7;

static const int LCD_COLUMNS = 16;
static const int LCD_ROWS = 2;
static const uint32_t SERIAL_BAUD_RATE = 9600;
static const int SERVO_MOUNT_INSIDE = 0;
static const int SERVO_MOUNT_BACK = 1;
static const int SERVO_MOUNT_MODE = SERVO_MOUNT_INSIDE;

// INSIDE mount is your current working motion.
// BACK mount starts with the horn vertical. If it opens the wrong way,
// change SERVO_BACK_OPEN_ANGLE from 0 to 180.
static const int SERVO_INSIDE_CLOSED_ANGLE = 15;
static const int SERVO_INSIDE_OPEN_ANGLE = 95;
static const int SERVO_BACK_CLOSED_ANGLE = 90;
static const int SERVO_BACK_OPEN_ANGLE = 0;
static const int SERVO_CLOSED_ANGLE = SERVO_MOUNT_MODE == SERVO_MOUNT_BACK
  ? SERVO_BACK_CLOSED_ANGLE
  : SERVO_INSIDE_CLOSED_ANGLE;
static const int SERVO_OPEN_ANGLE = SERVO_MOUNT_MODE == SERVO_MOUNT_BACK
  ? SERVO_BACK_OPEN_ANGLE
  : SERVO_INSIDE_OPEN_ANGLE;
static const unsigned long SERVO_OPEN_DURATION_MS = 1500;
static const unsigned long RFID_VALIDATION_TIMEOUT_MS = 12000;
static const unsigned long READY_CUE_MIN_INTERVAL_MS = 8000;

LiquidCrystal_I2C *lcd = nullptr;
MFRC522 mfrc522(SS_PIN, RST_PIN);
Servo lidServo;

unsigned long afkTimer = 0;
unsigned long messageTimer = 0;
unsigned long activeLedTimer = 0;
unsigned long servoOpenedAt = 0;
unsigned long validationStartedAt = 0;
unsigned long lastReadyCueAt = 0;

bool lcdReady = false;
bool showingMessage = false;
bool waitingValidation = false;
bool lastScanWasValid = false;
bool activeHeartbeatOn = false;
bool activeBlinkState = false;
bool servoOpen = false;

// 0 = AFK/customer closed, 1 = customer vending mode open
int systemMode = 0;
int afkFrame = 0;

const char *afkFacts[][2] = {
  {"1 ALUMINUM CAN", "SAVES 95% POWER"},
  {"RECYCLED PAPER", "SAVES TREES    "},
  {"PLASTIC BOTTLES", "CAN BECOME BAGS"},
  {"GLASS CAN BE   ", "RECYCLED AGAIN "},
  {"CANS RECYCLE IN", "ABOUT 60 DAYS  "},
  {"CLEAN BOTTLES  ", "EARN ECO POINTS"}
};
static const int AFK_FACT_COUNT = sizeof(afkFacts) / sizeof(afkFacts[0]);

void playCue(const String &cue);

uint8_t scanLcdAddress() {
  Serial.println("LCD:I2C_SCAN_START");

  uint8_t firstAddress = 0;
  for (uint8_t address = 1; address < 127; address++) {
    Wire.beginTransmission(address);
    byte error = Wire.endTransmission();
    if (error == 0) {
      Serial.print("LCD:I2C_DEVICE=0x");
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

String sanitizeLcdText(const String &value) {
  String output = "";
  output.reserve(value.length());

  for (unsigned int i = 0; i < value.length(); i++) {
    char c = value[i];
    output += (c >= 32 && c <= 126) ? c : ' ';
  }

  output.trim();
  output.toUpperCase();
  return output;
}

String fitLcdLine(const String &value, int startIndex) {
  String line = "";
  for (int i = 0; i < LCD_COLUMNS; i++) {
    int sourceIndex = startIndex + i;
    line += sourceIndex < (int)value.length() ? value[sourceIndex] : ' ';
  }
  return line;
}

void writeLcdLines(const String &line1, const String &line2) {
  if (!lcdReady || lcd == nullptr) {
    return;
  }

  String clean1 = sanitizeLcdText(line1);
  String clean2 = sanitizeLcdText(line2);

  lcd->setCursor(0, 0);
  lcd->print(fitLcdLine(clean1, 0));
  lcd->setCursor(0, 1);
  lcd->print(fitLcdLine(clean2, 0));
}

void writeWrappedMessage(const String &message) {
  String clean = sanitizeLcdText(message);

  if (clean == "CUSTOMER MODE READY") {
    writeLcdLines("VEND MODE READY", "TAP RFID TO SAVE");
    return;
  }

  if (clean == "CHECKING DATABASE") {
    writeLcdLines("CHECKING CARD", "PLEASE WAIT");
    return;
  }

  if (clean == "WELCOME BACK") {
    writeLcdLines("WELCOME BACK", "POINTS READY");
    return;
  }

  if (clean == "CASH INSERTED") {
    writeLcdLines("CASH ACCEPTED", "BALANCE UPDATED");
    return;
  }

  if (clean == "QR PAYMENT OK") {
    writeLcdLines("QR PAYMENT OK", "BALANCE UPDATED");
    return;
  }

  if (clean == "CHANGE RETURNED") {
    writeLcdLines("CHANGE RETURNED", "COLLECT COINS");
    return;
  }

  if (clean == "PRINTING RECEIPT") {
    writeLcdLines("PRINTING", "RECEIPT");
    return;
  }

  if (clean == "RECEIPT COMPLETE") {
    writeLcdLines("RECEIPT DONE", "THANK YOU");
    return;
  }

  if (clean == "RECEIPT FAILED") {
    writeLcdLines("PRINT FAILED", "CHECK PRINTER");
    return;
  }

  if (clean == "LOADING MACHINE") {
    writeLcdLines("LOADING MACHINE", "PLEASE WAIT");
    return;
  }

  if (clean == "NEW USER REGISTER") {
    writeLcdLines("NEW RFID CARD", "REGISTER NOW");
    return;
  }

  if (clean == "REGISTER CANCELLED") {
    writeLcdLines("REG CANCELLED", "TRY AGAIN");
    return;
  }

  if (clean == "CARD REGISTERED") {
    writeLcdLines("CARD LINKED", "WELCOME ABOARD");
    return;
  }

  if (clean == "RFID OFFLINE TRY AGAIN") {
    writeLcdLines("RFID OFFLINE", "TRY AGAIN");
    return;
  }

  if (clean.indexOf("POINTS SAVED") >= 0) {
    writeLcdLines("POINTS SAVED", "THANK YOU");
    return;
  }

  if (clean.indexOf("POINTS TAP RFID") >= 0) {
    writeLcdLines("ECO POINTS ADDED", "TAP RFID TO SAVE");
    return;
  }

  if (clean.indexOf("DISPENS") >= 0) {
    writeLcdLines("OPENING LID", "PLEASE WAIT");
    return;
  }

  if (clean.indexOf("TAKE YOUR ITEM") >= 0) {
    writeLcdLines("TAKE YOUR ITEM", "THANK YOU");
    return;
  }

  if (clean == "NOT ENOUGH MONEY") {
    writeLcdLines("INSERT MORE CASH", "TRY AGAIN");
    return;
  }

  writeLcdLines(fitLcdLine(clean, 0), fitLcdLine(clean, LCD_COLUMNS));
}

void initLcd() {
  Wire.begin();
  uint8_t detectedAddress = scanLcdAddress();
  if (detectedAddress == 0) {
    detectedAddress = 0x27;
    Serial.println("LCD:NOT_FOUND_USING_DEFAULT_0x27");
  }

  Serial.print("LCD:USING_ADDRESS=0x");
  if (detectedAddress < 16) {
    Serial.print("0");
  }
  Serial.println(detectedAddress, HEX);

  lcd = new LiquidCrystal_I2C(detectedAddress, LCD_COLUMNS, LCD_ROWS);
  lcd->init();
  lcd->backlight();
  lcdReady = true;
}

void closeLidServo() {
  lidServo.write(SERVO_CLOSED_ANGLE);
  servoOpen = false;
}

void openLidServo() {
  lidServo.write(SERVO_OPEN_ANGLE);
  servoOpenedAt = millis();
  servoOpen = true;
}

void updateLidServo() {
  if (servoOpen && millis() - servoOpenedAt >= SERVO_OPEN_DURATION_MS) {
    closeLidServo();
  }
}

void buzzTone(unsigned int frequency, unsigned int durationMs) {
  tone(BUZZER_PIN, frequency, durationMs);
  delay(durationMs + 18);
  noTone(BUZZER_PIN);
}

void playReadyCueIfNeeded(bool modeChanged) {
  unsigned long now = millis();
  if (modeChanged || now - lastReadyCueAt >= READY_CUE_MIN_INTERVAL_MS) {
    lastReadyCueAt = now;
    playCue("READY");
  }
}

void playCue(const String &cue) {
  if (cue == "READY") {
    buzzTone(988, 70);
    buzzTone(1319, 90);
  } else if (cue == "SCAN") {
    buzzTone(1568, 55);
  } else if (cue == "CASH") {
    buzzTone(1760, 40);
    buzzTone(2093, 55);
  } else if (cue == "VALID") {
    buzzTone(1047, 75);
    buzzTone(1568, 95);
  } else if (cue == "INVALID") {
    buzzTone(220, 130);
    buzzTone(196, 150);
  } else if (cue == "DISPENSE") {
    buzzTone(784, 75);
    buzzTone(988, 75);
    buzzTone(1319, 120);
  } else if (cue == "CHANGE") {
    buzzTone(1760, 32);
    buzzTone(1397, 28);
    buzzTone(1568, 32);
    buzzTone(1175, 35);
    buzzTone(1319, 55);
  } else if (cue == "RECEIPT") {
    buzzTone(988, 45);
    buzzTone(1175, 45);
    buzzTone(988, 45);
  } else if (cue == "SUCCESS") {
    buzzTone(1047, 70);
    buzzTone(1319, 70);
    buzzTone(1568, 120);
  } else if (cue == "ERROR") {
    buzzTone(196, 150);
    buzzTone(165, 170);
  } else if (cue == "CLICK") {
    buzzTone(1400, 45);
  }
}

void resetDisplay() {
  waitingValidation = false;
  validationStartedAt = 0;
  showingMessage = false;
  lastScanWasValid = false;
  activeLedTimer = millis();
  activeHeartbeatOn = false;
  activeBlinkState = false;

  if (systemMode == 1) {
    writeLcdLines("ECO-MATIC READY", "CASH OR RECYCLE");
    digitalWrite(GREEN_LED, HIGH);
    digitalWrite(RED_LED, LOW);
  } else {
    writeLcdLines("ECO-MATIC IDLE", "START IN APP");
    digitalWrite(GREEN_LED, LOW);
    digitalWrite(RED_LED, LOW);
  }

  afkTimer = millis();
}

void showAfkFrame() {
  writeLcdLines(afkFacts[afkFrame][0], afkFacts[afkFrame][1]);
  digitalWrite(GREEN_LED, afkFrame % 2 == 0 ? HIGH : LOW);
  digitalWrite(RED_LED, afkFrame % 2 != 0 ? HIGH : LOW);
}

void updateActiveLedState() {
  unsigned long now = millis();

  if (showingMessage) {
    if (lastScanWasValid) {
      digitalWrite(GREEN_LED, HIGH);
      digitalWrite(RED_LED, LOW);
    } else if (now - activeLedTimer >= 180) {
      activeLedTimer = now;
      activeBlinkState = !activeBlinkState;
      digitalWrite(GREEN_LED, LOW);
      digitalWrite(RED_LED, activeBlinkState ? HIGH : LOW);
    }
    return;
  }

  if (waitingValidation) {
    if (now - activeLedTimer >= 110) {
      activeLedTimer = now;
      activeBlinkState = !activeBlinkState;
      digitalWrite(GREEN_LED, activeBlinkState ? HIGH : LOW);
      digitalWrite(RED_LED, activeBlinkState ? HIGH : LOW);
    }
    return;
  }

  unsigned long interval = activeHeartbeatOn ? 120 : 760;
  if (now - activeLedTimer >= interval) {
    activeLedTimer = now;
    activeHeartbeatOn = !activeHeartbeatOn;
    digitalWrite(GREEN_LED, activeHeartbeatOn ? HIGH : LOW);
    digitalWrite(RED_LED, LOW);
  }
}

void handleIncomingCommand(const String &incoming) {
  String msg = incoming;
  msg.trim();
  if (msg.length() == 0) {
    return;
  }

  if (msg == "STATE:ACTIVE") {
    bool modeChanged = systemMode != 1;
    systemMode = 1;
    resetDisplay();
    playReadyCueIfNeeded(modeChanged);
    Serial.println("SESSION:ACTIVE");
    return;
  }

  if (msg == "STATE:AFK") {
    systemMode = 0;
    closeLidServo();
    resetDisplay();
    Serial.println("SESSION:AFK");
    return;
  }

  if (msg == "SERVO:OPEN") {
    openLidServo();
    playCue("DISPENSE");
    return;
  }

  if (msg.startsWith("MSG:")) {
    String customMsg = msg.substring(4);
    customMsg.trim();
    writeWrappedMessage(customMsg);

    String cleanMsg = sanitizeLcdText(customMsg);
    if (cleanMsg == "CASH INSERTED" || cleanMsg == "QR PAYMENT OK") {
      playCue("CASH");
    } else if (cleanMsg.indexOf("DISPENS") >= 0 || cleanMsg.indexOf("TAKE YOUR ITEM") >= 0) {
      openLidServo();
      playCue("DISPENSE");
    } else if (cleanMsg == "PRINTING RECEIPT") {
      playCue("RECEIPT");
    } else if (cleanMsg == "RECEIPT COMPLETE") {
      playCue("SUCCESS");
    } else if (cleanMsg == "RECEIPT FAILED") {
      playCue("ERROR");
    } else if (cleanMsg.indexOf("CHANGE") >= 0 || cleanMsg.indexOf("RETURN") >= 0) {
      playCue("CHANGE");
    } else if (cleanMsg.indexOf("NOT ENOUGH") >= 0 || cleanMsg.indexOf("ERROR") >= 0 || cleanMsg.indexOf("SOLD OUT") >= 0) {
      playCue("ERROR");
    } else if (cleanMsg.indexOf("POINTS") >= 0 || cleanMsg.indexOf("RFID") >= 0 || cleanMsg.indexOf("CASH") >= 0) {
      playCue("CLICK");
    }

    showingMessage = true;
    messageTimer = millis();
    return;
  }

  if (systemMode == 1 && msg == "VALID") {
    writeLcdLines("POINTS SAVED", "BAL UPDATED");
    waitingValidation = false;
    validationStartedAt = 0;
    lastScanWasValid = true;
    activeLedTimer = millis();
    showingMessage = true;
    messageTimer = millis();
    playCue("VALID");
    return;
  }

  if (systemMode == 1 && msg == "INVALID") {
    writeLcdLines("CARD NOT FOUND", "REGISTER FIRST");
    waitingValidation = false;
    validationStartedAt = 0;
    lastScanWasValid = false;
    activeLedTimer = millis();
    activeBlinkState = true;
    showingMessage = true;
    messageTimer = millis();
    playCue("INVALID");
    return;
  }
}

void setup() {
  Serial.begin(SERIAL_BAUD_RATE);
  delay(300);

  pinMode(GREEN_LED, OUTPUT);
  pinMode(RED_LED, OUTPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(GREEN_LED, LOW);
  digitalWrite(RED_LED, LOW);
  digitalWrite(BUZZER_PIN, LOW);
  lidServo.attach(SERVO_PIN);
  closeLidServo();

  initLcd();

  SPI.begin();
  mfrc522.PCD_Init();
  delay(30);

  Serial.print("RFID:FIRMWARE_VERSION=");
  mfrc522.PCD_DumpVersionToSerial();

  byte version = mfrc522.PCD_ReadRegister(mfrc522.VersionReg);
  if (version == 0x00 || version == 0xFF) {
    Serial.println("RFID:NOT_FOUND_CHECK_SPI_WIRING");
    writeLcdLines("RFID NOT FOUND", "CHECK WIRING");
  } else {
    Serial.println("RFID:READY");
    resetDisplay();
  }

  Serial.println("SYSTEM:READY");
}

void loop() {
  updateLidServo();

  while (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    handleIncomingCommand(msg);
  }

  if (systemMode == 0) {
    if (millis() - afkTimer > 3000) {
      afkTimer = millis();
      afkFrame = (afkFrame + 1) % AFK_FACT_COUNT;
      showAfkFrame();
    }
    return;
  }

  if (showingMessage && millis() - messageTimer > 3000) {
    resetDisplay();
  }

  updateActiveLedState();

  if (waitingValidation) {
    if (millis() - validationStartedAt > RFID_VALIDATION_TIMEOUT_MS) {
      Serial.println("RFID:PC_RESPONSE_TIMEOUT");
      writeLcdLines("RFID TIMEOUT", "TRY AGAIN");
      playCue("ERROR");
      waitingValidation = false;
      validationStartedAt = 0;
      lastScanWasValid = false;
      showingMessage = true;
      messageTimer = millis();
      activeLedTimer = millis();
    }
    return;
  }

  if (!mfrc522.PICC_IsNewCardPresent() || !mfrc522.PICC_ReadCardSerial()) {
    return;
  }

  String rfidStr = "";
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    if (mfrc522.uid.uidByte[i] < 0x10) {
      rfidStr += "0";
    }
    rfidStr += String(mfrc522.uid.uidByte[i], HEX);
  }
  rfidStr.toUpperCase();

  Serial.print("RFID:");
  Serial.println(rfidStr);

  writeLcdLines("CARD SCANNED", "CHECKING...");
  playCue("SCAN");
  waitingValidation = true;
  validationStartedAt = millis();
  activeLedTimer = millis();
  activeBlinkState = true;
  digitalWrite(GREEN_LED, HIGH);
  digitalWrite(RED_LED, HIGH);

  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();
}
