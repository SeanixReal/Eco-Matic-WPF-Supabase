#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Servo.h>
#include <avr/pgmspace.h>

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

static const uint8_t DEFAULT_LCD_ADDRESS = 0x27;
static const uint8_t FALLBACK_LCD_ADDRESS = 0x3F;
static const int LCD_COLUMNS = 16;
static const int LCD_ROWS = 2;
static const uint32_t SERIAL_BAUD_RATE = 9600;
static const int SERVO_CLOSED_ANGLE = 0;
static const int SERVO_TIP_ANGLE = 100;
static const unsigned long SERVO_TIP_STEP_MS = 500;
static const unsigned long SERVO_DETACH_DELAY_MS = 450;
static const unsigned long RFID_VALIDATION_TIMEOUT_MS = 12000;
static const unsigned long READY_CUE_MIN_INTERVAL_MS = 8000;

LiquidCrystal_I2C *lcd = nullptr;
MFRC522 mfrc522(SS_PIN, RST_PIN);
Servo lidServo;

unsigned long afkTimer = 0;
unsigned long messageTimer = 0;
unsigned long activeLedTimer = 0;
unsigned long servoStepAt = 0;
unsigned long validationStartedAt = 0;
unsigned long lastReadyCueAt = 0;
unsigned long servoDetachAt = 0;

bool lcdReady = false;
bool showingMessage = false;
bool waitingValidation = false;
bool lastScanWasValid = false;
bool activeHeartbeatOn = false;
bool activeBlinkState = false;
bool servoTipActive = false;
bool servoAttached = false;
bool servoDetachPending = false;
int servoTipStep = 0;

// 0 = AFK/customer closed, 1 = customer vending mode open
int systemMode = 0;
int afkFrame = 0;

enum LedMode {
  LED_BOOT,
  LED_AFK,
  LED_READY,
  LED_SCANNING,
  LED_SUCCESS,
  LED_ERROR,
  LED_DISPENSING,
  LED_CASH
};

LedMode ledMode = LED_BOOT;
unsigned long ledTimer = 0;
int ledStep = 0;

const char afkFact00[] PROGMEM = "1 ALUMINUM CAN";
const char afkFact01[] PROGMEM = "SAVES 95% POWER";
const char afkFact10[] PROGMEM = "RECYCLED PAPER";
const char afkFact11[] PROGMEM = "SAVES TREES";
const char afkFact20[] PROGMEM = "PLASTIC BOTTLES";
const char afkFact21[] PROGMEM = "CAN BECOME BAGS";
const char afkFact30[] PROGMEM = "GLASS CAN BE";
const char afkFact31[] PROGMEM = "RECYCLED AGAIN";
const char afkFact40[] PROGMEM = "CANS RECYCLE IN";
const char afkFact41[] PROGMEM = "ABOUT 60 DAYS";
const char afkFact50[] PROGMEM = "CLEAN BOTTLES";
const char afkFact51[] PROGMEM = "EARN ECO POINTS";

const char *const afkFacts[][2] PROGMEM = {
  {afkFact00, afkFact01},
  {afkFact10, afkFact11},
  {afkFact20, afkFact21},
  {afkFact30, afkFact31},
  {afkFact40, afkFact41},
  {afkFact50, afkFact51}
};
static const int AFK_FACT_COUNT = 6;

void playCue(const String &cue);

void setLedMode(LedMode mode) {
  if (ledMode == mode) {
    return;
  }

  ledMode = mode;
  ledTimer = millis();
  ledStep = 0;
}

void writeLeds(bool greenOn, bool redOn) {
  digitalWrite(GREEN_LED, greenOn ? HIGH : LOW);
  digitalWrite(RED_LED, redOn ? HIGH : LOW);
}

void advanceLedStep(unsigned long intervalMs) {
  unsigned long now = millis();
  if (now - ledTimer >= intervalMs) {
    ledTimer = now;
    ledStep++;
  }
}

void updateLedState() {
  switch (ledMode) {
    case LED_BOOT:
      advanceLedStep(120);
      writeLeds(ledStep % 2 == 0, ledStep % 2 != 0);
      break;

    case LED_AFK:
      advanceLedStep(180);
      // Slow green-green-red-rest pattern while the machine is waiting.
      switch (ledStep % 8) {
        case 0:
        case 2:
          writeLeds(true, false);
          break;
        case 4:
          writeLeds(false, true);
          break;
        default:
          writeLeds(false, false);
          break;
      }
      break;

    case LED_READY:
      advanceLedStep(140);
      // Green heartbeat with a tiny red status wink so both LEDs prove alive.
      switch (ledStep % 12) {
        case 0:
        case 2:
          writeLeds(true, false);
          break;
        case 6:
          writeLeds(false, true);
          break;
        default:
          writeLeds(false, false);
          break;
      }
      break;

    case LED_SCANNING:
      advanceLedStep(95);
      writeLeds(ledStep % 2 == 0, ledStep % 2 != 0);
      break;

    case LED_SUCCESS:
      advanceLedStep(100);
      writeLeds(ledStep % 2 == 0, false);
      break;

    case LED_ERROR:
      advanceLedStep(90);
      writeLeds(false, ledStep % 2 == 0);
      break;

    case LED_DISPENSING:
      advanceLedStep(75);
      switch (ledStep % 4) {
        case 0:
          writeLeds(true, false);
          break;
        case 1:
          writeLeds(true, true);
          break;
        case 2:
          writeLeds(false, true);
          break;
        default:
          writeLeds(false, false);
          break;
      }
      break;

    case LED_CASH:
      advanceLedStep(85);
      switch (ledStep % 6) {
        case 0:
        case 2:
        case 4:
          writeLeds(true, false);
          break;
        case 1:
        case 3:
          writeLeds(true, true);
          break;
        default:
          writeLeds(false, false);
          break;
      }
      break;
  }
}

uint8_t scanLcdAddress() {
  Serial.println(F("LCD:I2C_SCAN_START"));

  uint8_t firstAddress = 0;
  for (uint8_t address = 1; address < 127; address++) {
    Wire.beginTransmission(address);
    byte error = Wire.endTransmission();
    if (error == 0) {
      Serial.print(F("LCD:I2C_DEVICE=0x"));
      if (address < 16) {
        Serial.print("0");
      }
      Serial.println(address, HEX);

      if (address == DEFAULT_LCD_ADDRESS || address == FALLBACK_LCD_ADDRESS) {
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

  // Write every cell explicitly so old characters never bleed through
  lcd->setCursor(0, 0);
  for (int i = 0; i < LCD_COLUMNS; i++) {
    lcd->write(i < (int)clean1.length() ? (char)clean1[i] : ' ');
  }
  lcd->setCursor(0, 1);
  for (int i = 0; i < LCD_COLUMNS; i++) {
    lcd->write(i < (int)clean2.length() ? (char)clean2[i] : ' ');
  }
}



void writeWrappedMessage(const String &message) {
  String clean = sanitizeLcdText(message);

  if (clean == "CUSTOMER MODE READY") {
    resetDisplay();
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

  if (clean == "LOADING MACHINE") {
    writeLcdLines("LOADING...", "PLEASE WAIT");
    return;
  }

  if (clean.indexOf("THANK YOU") >= 0) {
    writeLcdLines("THANK YOU!", "COME AGAIN SOON");
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

  if (clean == "POINT PAYMENT OK") {
    writeLcdLines("POINT PAYMENT", "APPROVED");
    return;
  }

  if (clean == "POINT PAY READY") {
    writeLcdLines("POINT PAY READY", "SELECT ITEM");
    return;
  }

  if (clean == "POINT PAY OFF") {
    writeLcdLines("POINT PAY OFF", "CASH MODE");
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
    writeLcdLines("DISPENSING...", "PLEASE WAIT");
    return;
  }

  if (clean.indexOf("TAKE YOUR ITEM") >= 0) {
    writeLcdLines("ITEM DISPENSED!", "COLLECT & ENJOY");
    return;
  }

  if (clean == "NOT ENOUGH MONEY") {
    writeLcdLines("INSERT MORE CASH", "TRY AGAIN");
    return;
  }

  writeLcdLines(fitLcdLine(clean, 0), fitLcdLine(clean, LCD_COLUMNS));
}

void readAfkFactLine(int frame, int row, char *buffer, size_t bufferSize) {
  if (bufferSize == 0) {
    return;
  }

  frame = constrain(frame, 0, AFK_FACT_COUNT - 1);
  row = constrain(row, 0, 1);

  const char *linePtr = (const char *)pgm_read_word(&(afkFacts[frame][row]));
  strncpy_P(buffer, linePtr, bufferSize - 1);
  buffer[bufferSize - 1] = '\0';
}

void initLcd() {
  Wire.begin();
  Wire.setClock(100000);
  delay(300);

  uint8_t detectedAddress = scanLcdAddress();
  if (detectedAddress == 0) {
    lcdReady = false;
    Serial.println(F("LCD:NOT_FOUND_CHECK_5V_GND_SDA_A4_SCL_A5"));
    return;
  }

  Serial.print(F("LCD:USING_ADDRESS=0x"));
  if (detectedAddress < 16) {
    Serial.print("0");
  }
  Serial.println(detectedAddress, HEX);

  lcd = new LiquidCrystal_I2C(detectedAddress, LCD_COLUMNS, LCD_ROWS);
  lcd->init();
  delay(80);
  lcd->display();
  lcd->backlight();
  delay(80);
  lcd->clear();
  delay(20);

  lcdReady = true;
  writeLcdLines("ECO-MATIC SYSTEM", "BOOTING...");
  delay(1000);
  writeLcdLines("ADJUST CONTRAST", "IF TEXT IS DIM");
  delay(1000);
}

void ensureLidServoAttached() {
  if (!servoAttached) {
    lidServo.attach(SERVO_PIN);
    delay(15);
    servoAttached = true;
  }

  servoDetachPending = false;
}

void closeLidServo() {
  if (!servoTipActive && !servoAttached) {
    return;
  }

  ensureLidServoAttached();
  lidServo.write(SERVO_CLOSED_ANGLE);
  servoTipActive = false;
  servoTipStep = 0;
  servoDetachAt = millis() + SERVO_DETACH_DELAY_MS;
  servoDetachPending = true;
  if (ledMode == LED_DISPENSING) {
    setLedMode(systemMode == 1 ? LED_READY : LED_AFK);
  }
}

void openLidServo() {
  ensureLidServoAttached();
  lidServo.write(SERVO_TIP_ANGLE);
  servoStepAt = millis();
  servoTipActive = true;
  servoTipStep = 0;
  servoDetachPending = false;
  setLedMode(LED_DISPENSING);
}

void updateLidServo() {
  if (servoTipActive && millis() - servoStepAt >= SERVO_TIP_STEP_MS) {
    servoStepAt = millis();
    servoTipStep++;

    switch (servoTipStep) {
      case 1:
        lidServo.write(SERVO_CLOSED_ANGLE);
        break;
      case 2:
      case 3:
        lidServo.write(SERVO_TIP_ANGLE);
        break;
      case 4:
        closeLidServo();
        break;
    }
  }

  if (servoDetachPending && (long)(millis() - servoDetachAt) >= 0) {
    lidServo.detach();
    servoAttached = false;
    servoDetachPending = false;
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
    setLedMode(LED_READY);
  } else {
    writeLcdLines("ECO-MATIC IDLE", "START IN APP");
    setLedMode(LED_AFK);
  }

  afkTimer = millis();
}

void showAfkFrame() {
  char line1[LCD_COLUMNS + 1];
  char line2[LCD_COLUMNS + 1];
  readAfkFactLine(afkFrame, 0, line1, sizeof(line1));
  readAfkFactLine(afkFrame, 1, line2, sizeof(line2));
  writeLcdLines(line1, line2);
}

void updateActiveLedState() {
  updateLedState();
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
    Serial.println(F("SESSION:ACTIVE"));
    return;
  }

  if (msg == "STATE:AFK") {
    systemMode = 0;
    closeLidServo();
    resetDisplay();
    Serial.println(F("SESSION:AFK"));
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
      setLedMode(LED_CASH);
      playCue("CASH");
    } else if (cleanMsg.indexOf("DISPENS") >= 0) {
      openLidServo();
      playCue("DISPENSE");
    } else if (cleanMsg == "PRINTING RECEIPT") {
      setLedMode(LED_SCANNING);
      playCue("RECEIPT");
    } else if (cleanMsg == "RECEIPT COMPLETE" || cleanMsg == "CARD REGISTERED" || cleanMsg.indexOf("POINTS SAVED") >= 0) {
      setLedMode(LED_SUCCESS);
      playCue("SUCCESS");
    } else if (cleanMsg == "RECEIPT FAILED" || cleanMsg == "REGISTER CANCELLED" || cleanMsg.indexOf("OFFLINE") >= 0) {
      setLedMode(LED_ERROR);
      playCue("ERROR");
    } else if (cleanMsg.indexOf("CHANGE") >= 0 || cleanMsg.indexOf("RETURN") >= 0) {
      setLedMode(LED_CASH);
      playCue("CHANGE");
    } else if (cleanMsg.indexOf("NOT ENOUGH") >= 0 || cleanMsg.indexOf("ERROR") >= 0 || cleanMsg.indexOf("SOLD OUT") >= 0) {
      setLedMode(LED_ERROR);
      playCue("ERROR");
    } else if (cleanMsg.indexOf("POINTS") >= 0 || cleanMsg.indexOf("RFID") >= 0 || cleanMsg.indexOf("CASH") >= 0) {
      setLedMode(LED_SCANNING);
      playCue("CLICK");
    } else {
      setLedMode(LED_READY);
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
    setLedMode(LED_SUCCESS);
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
    setLedMode(LED_ERROR);
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
  setLedMode(LED_BOOT);

  initLcd();

  SPI.begin();
  mfrc522.PCD_Init();
  delay(30);

  Serial.print(F("RFID:FIRMWARE_VERSION="));
  mfrc522.PCD_DumpVersionToSerial();

  byte version = mfrc522.PCD_ReadRegister(mfrc522.VersionReg);
  if (version == 0x00 || version == 0xFF) {
    Serial.println(F("RFID:NOT_FOUND_CHECK_SPI_WIRING"));
    writeLcdLines("RFID NOT FOUND", "CHECK WIRING");
    setLedMode(LED_ERROR);
  } else {
    Serial.println(F("RFID:READY"));
    resetDisplay();
  }

  lidServo.write(SERVO_CLOSED_ANGLE);
  lidServo.attach(SERVO_PIN);
  servoAttached = true;
  servoDetachAt = millis() + SERVO_DETACH_DELAY_MS;
  servoDetachPending = true;

  Serial.println(F("SYSTEM:READY"));
}

void loop() {
  updateLidServo();

  while (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    handleIncomingCommand(msg);
  }

  if (systemMode == 0) {
    updateLedState();
    // Don't overwrite temporary messages (like LOADING MACHINE) with AFK facts
    if (!showingMessage && millis() - afkTimer > 3000) {
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
      Serial.println(F("RFID:PC_RESPONSE_TIMEOUT"));
      writeLcdLines("RFID TIMEOUT", "TRY AGAIN");
      playCue("ERROR");
      waitingValidation = false;
      validationStartedAt = 0;
      lastScanWasValid = false;
      showingMessage = true;
      messageTimer = millis();
      activeLedTimer = millis();
      setLedMode(LED_ERROR);
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

  Serial.print(F("RFID:"));
  Serial.println(rfidStr);

  writeLcdLines("CARD SCANNED", "CHECKING...");
  playCue("SCAN");
  waitingValidation = true;
  validationStartedAt = millis();
  activeLedTimer = millis();
  activeBlinkState = true;
  setLedMode(LED_SCANNING);

  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();
}
