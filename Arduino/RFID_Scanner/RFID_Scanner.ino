// ============================================================
// Eco-Matic — Arduino Uno/Nano Firmware  v2.2
// RFID + LCD1602 (I2C) + LED + Servo + Buzzer
//
// LED layout: GREEN = left, RED = right
//
// Serial protocol (PC <-> Arduino):
//   Arduino -> PC : RFID:<hex uid>
//   PC -> Arduino : STATE:ACTIVE | STATE:AFK
//   PC -> Arduino : VALID | INVALID
//   PC -> Arduino : MSG:<up to 32 chars>
//   PC -> Arduino : SERVO:OPEN
// ============================================================

#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Servo.h>
#include <avr/pgmspace.h>

// ── Pin assignments ──────────────────────────────────────────
static const int PIN_RST        = 9;
static const int PIN_SS         = 10;
static const int PIN_BUZZER     = 4;
static const int PIN_SERVO      = 5;
static const int PIN_LED_GREEN  = 6;   // left
static const int PIN_LED_RED    = 7;   // right

// ── LCD ──────────────────────────────────────────────────────
static const uint8_t LCD_ADDR_PRIMARY  = 0x27;
static const uint8_t LCD_ADDR_FALLBACK = 0x3F;
static const int     LCD_COLS          = 16;
static const int     LCD_ROWS          = 2;

// ── Timing (ms) ──────────────────────────────────────────────
static const uint32_t      BAUD_RATE         = 9600;
static const unsigned long SERVO_TIP_STEP    = 500;
static const unsigned long SERVO_DETACH_DLY  = 450;
static const unsigned long RFID_TIMEOUT      = 12000;
static const unsigned long READY_CUE_PERIOD  = 8000;
static const unsigned long MSG_DISPLAY_TIME  = 3000;
static const unsigned long AFK_CYCLE_TIME    = 3000;

// ── Servo angles ─────────────────────────────────────────────
static const int SERVO_CLOSED = 0;
static const int SERVO_OPEN   = 100;

// ── AFK eco-facts (PROGMEM) ───────────────────────────────────
static const int AFK_FACT_COUNT = 6;

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

const char *const afkFacts[AFK_FACT_COUNT][2] PROGMEM = {
  { afkFact00, afkFact01 },
  { afkFact10, afkFact11 },
  { afkFact20, afkFact21 },
  { afkFact30, afkFact31 },
  { afkFact40, afkFact41 },
  { afkFact50, afkFact51 },
};

// ── Hardware objects ─────────────────────────────────────────
LiquidCrystal_I2C *lcd = nullptr;
MFRC522            rfid(PIN_SS, PIN_RST);
Servo              lidServo;

// ── System state ─────────────────────────────────────────────
enum SystemMode { MODE_AFK = 0, MODE_ACTIVE = 1 };
SystemMode systemMode = MODE_AFK;

bool lcdReady          = false;
bool showingMessage    = false;
bool waitingValidation = false;

// ── Timers ───────────────────────────────────────────────────
unsigned long messageTimer        = 0;
unsigned long afkTimer            = 0;
unsigned long rfidValidationStart = 0;
unsigned long lastReadyCueAt      = 0;
int           afkFrame            = 0;

// ── Servo state ──────────────────────────────────────────────
bool          servoAttached      = false;
bool          servoDetachPending = false;
bool          servoTipActive     = false;
int           servoTipStep       = 0;
unsigned long servoStepAt        = 0;
unsigned long servoDetachAt      = 0;

// ── LED state machine ─────────────────────────────────────────
enum LedMode {
  LED_BOOT,       // startup: sweep both LEDs, proving they work
  LED_AFK,        // closed: slow single red pulse
  LED_READY,      // open: double green heartbeat
  LED_SCANNING,   // processing: fast left-right chase
  LED_SUCCESS,    // accepted: triple green flash
  LED_ERROR,      // rejected: double red flash + pause
  LED_DISPENSING, // item moving: green→both→red ping-pong
  LED_CASH        // payment: green solid then both blink
};
LedMode       ledMode  = LED_BOOT;
unsigned long ledTimer = 0;
int           ledStep  = 0;

// ── Forward declarations ──────────────────────────────────────
void playCue(const String &cue);
void resetDisplay();
void closeLidServo();
void openLidServo();

// ─────────────────────────────────────────────────────────────
//  LED
// ─────────────────────────────────────────────────────────────
void setLed(bool green, bool red) {
  digitalWrite(PIN_LED_GREEN, green ? HIGH : LOW);
  digitalWrite(PIN_LED_RED,   red   ? HIGH : LOW);
}

void setLedMode(LedMode mode) {
  if (ledMode == mode) return;
  ledMode  = mode;
  ledTimer = millis();
  ledStep  = 0;
}

void ledTick(unsigned long intervalMs) {
  if (millis() - ledTimer >= intervalMs) {
    ledTimer = millis();
    ledStep++;
  }
}

void updateLedState() {
  switch (ledMode) {

    // ── BOOT ── alternating sweep, proves both LEDs are alive ─
    // G · R · G · R ...  (150 ms each)
    case LED_BOOT:
      ledTick(150);
      setLed(ledStep % 2 == 0, ledStep % 2 != 0);
      break;

    // ── AFK ── slow single red pulse: machine is closed ───────
    // · · · · · R R · · · · ·  (12 steps × 200 ms = 2.4 s cycle)
    // Mostly dark with one dim red heartbeat — do not disturb.
    case LED_AFK:
      ledTick(200);
      switch (ledStep % 12) {
        case 5:
        case 6:  setLed(false, true);  break;  // red pulse
        default: setLed(false, false); break;   // dark
      }
      break;

    // ── READY ── double green heartbeat: open, come deposit ───
    // G · G · · · · · · · · ·  (14 steps × 110 ms ≈ 1.5 s cycle)
    // Two quick green taps then a long rest — alive and waiting.
    case LED_READY:
      ledTick(110);
      switch (ledStep % 14) {
        case 0:
        case 2:  setLed(true,  false); break;  // double green tap
        default: setLed(false, false); break;   // rest
      }
      break;

    // ── SCANNING ── fast left→right chase: data is moving ─────
    // G R G R G R ...  (80 ms each)
    // Rapid alternation signals the system is actively working.
    case LED_SCANNING:
      ledTick(80);
      setLed(ledStep % 2 == 0, ledStep % 2 != 0);
      break;

    // ── SUCCESS ── triple green flash: universal yes ───────────
    // G · G · G · · · · ·  (10 steps × 100 ms = 1 s cycle)
    // Three celebratory green blinks then silence.
    case LED_SUCCESS:
      ledTick(100);
      switch (ledStep % 10) {
        case 0:
        case 2:
        case 4:  setLed(true,  false); break;  // 3 green flashes
        default: setLed(false, false); break;   // off
      }
      break;

    // ── ERROR ── double red flash + pause: universal no ───────
    // R · R · · · · ·  (8 steps × 110 ms ≈ 0.9 s cycle)
    // Two sharp red blinks then a pause — clear warning.
    case LED_ERROR:
      ledTick(110);
      switch (ledStep % 8) {
        case 0:
        case 2:  setLed(false, true);  break;  // 2 red flashes
        default: setLed(false, false); break;   // off
      }
      break;

    // ── DISPENSING ── ping-pong: item physically moving out ───
    // G B R B G ·  (6 steps × 80 ms ≈ 0.5 s cycle)
    // Green→both→red mirrors the item moving left-to-right
    // out of the machine chute, then a brief off before repeat.
    case LED_DISPENSING:
      ledTick(80);
      switch (ledStep % 6) {
        case 0: setLed(true,  false); break;  // green  (left)
        case 1: setLed(true,  true);  break;  // both   (center)
        case 2: setLed(false, true);  break;  // red    (right — item exits)
        case 3: setLed(true,  true);  break;  // both   (bounce back)
        case 4: setLed(true,  false); break;  // green  (reset)
        case 5: setLed(false, false); break;  // off    (pause)
      }
      break;

    // ── CASH ── green solid → both blink: payment confirmed ───
    // G G B B · ·  (8 steps × 90 ms ≈ 0.7 s cycle)
    // Green first (money received), then both (system confirms),
    // then off before the next cycle. Like a register "cha-ching".
    case LED_CASH:
      ledTick(90);
      switch (ledStep % 8) {
        case 0:
        case 1: setLed(true,  false); break;  // green  (incoming)
        case 2:
        case 3: setLed(true,  true);  break;  // both   (confirmed)
        default:setLed(false, false); break;  // off
      }
      break;
  }
}

// ─────────────────────────────────────────────────────────────
//  LCD
// ─────────────────────────────────────────────────────────────
uint8_t scanLcdAddress() {
  Serial.println(F("LCD:I2C_SCAN_START"));
  uint8_t firstFound = 0;

  for (uint8_t addr = 1; addr < 127; addr++) {
    Wire.beginTransmission(addr);
    if (Wire.endTransmission() != 0) continue;

    Serial.print(F("LCD:I2C_DEVICE=0x"));
    if (addr < 16) Serial.print('0');
    Serial.println(addr, HEX);

    if (addr == LCD_ADDR_PRIMARY || addr == LCD_ADDR_FALLBACK) return addr;
    if (firstFound == 0) firstFound = addr;
  }
  return firstFound;
}

String sanitize(const String &s) {
  String out;
  out.reserve(s.length());
  for (unsigned int i = 0; i < s.length(); i++) {
    char c = s[i];
    out += (c >= 32 && c <= 126) ? c : ' ';
  }
  out.trim();
  out.toUpperCase();
  return out;
}

void writeRow(int row, const String &text) {
  if (!lcdReady || lcd == nullptr) return;
  lcd->setCursor(0, row);
  for (int i = 0; i < LCD_COLS; i++) {
    lcd->write(i < (int)text.length() ? (char)text[i] : ' ');
  }
}

void writeLcd(const __FlashStringHelper *l1, const __FlashStringHelper *l2) {
  writeRow(0, String(l1));
  writeRow(1, String(l2));
}

void writeLcdStr(const String &l1, const String &l2) {
  writeRow(0, sanitize(l1));
  writeRow(1, sanitize(l2));
}

void writeLcdWrapped(const String &clean) {
  writeRow(0, clean.substring(0, LCD_COLS));
  writeRow(1, clean.length() > LCD_COLS ? clean.substring(LCD_COLS, LCD_COLS * 2) : String(""));
}

void afkFactToBuffer(int frame, int row, char *buf, size_t len) {
  if (len == 0) return;
  frame = constrain(frame, 0, AFK_FACT_COUNT - 1);
  row   = constrain(row,   0, 1);
  const char *ptr = (const char *)pgm_read_word(&afkFacts[frame][row]);
  strncpy_P(buf, ptr, len - 1);
  buf[len - 1] = '\0';
}

void showAfkFrame() {
  char l1[LCD_COLS + 1], l2[LCD_COLS + 1];
  afkFactToBuffer(afkFrame, 0, l1, sizeof(l1));
  afkFactToBuffer(afkFrame, 1, l2, sizeof(l2));
  writeLcdStr(l1, l2);
}

void initLcd() {
  Wire.begin();
  Wire.setClock(100000);
  delay(300);

  uint8_t addr = scanLcdAddress();
  if (addr == 0) {
    lcdReady = false;
    Serial.println(F("LCD:NOT_FOUND_CHECK_5V_GND_SDA_A4_SCL_A5"));
    return;
  }

  Serial.print(F("LCD:USING_ADDRESS=0x"));
  if (addr < 16) Serial.print('0');
  Serial.println(addr, HEX);

  lcd = new LiquidCrystal_I2C(addr, LCD_COLS, LCD_ROWS);
  lcd->init();
  delay(80);
  lcd->display();
  lcd->backlight();
  delay(80);
  lcd->clear();
  delay(20);

  lcdReady = true;
  writeLcd(F("ECO-MATIC SYSTEM"), F("BOOTING..."));
  delay(1000);
  writeLcd(F("ADJUST CONTRAST"), F("IF TEXT IS DIM"));
  delay(1000);
}

// ─────────────────────────────────────────────────────────────
//  MSG dispatch — all literals in flash via F()
// ─────────────────────────────────────────────────────────────
void dispatchMsg(const String &clean, bool &isSilent) {
  isSilent = false;

  // ── Silent refresh (no timer armed) ─────────────────────────
  if (clean == F("CUSTOMER MODE READY")) {
    isSilent = true;
    if (systemMode == MODE_ACTIVE)
      writeLcd(F("ECO-MATIC READY"), F("CASH OR RECYCLE"));
    return;
  }

  // ── Status ───────────────────────────────────────────────────
  if (clean == F("LOADING MACHINE")) {
    writeLcd(F("LOADING MACHINE"), F("PLEASE WAIT"));
    setLedMode(LED_SCANNING); return;
  }
  if (clean == F("CHECKING DATABASE")) {
    writeLcd(F("CHECKING CARD"), F("PLEASE WAIT"));
    setLedMode(LED_SCANNING); playCue(F("CLICK")); return;
  }
  if (clean == F("WELCOME BACK")) {
    writeLcd(F("WELCOME BACK"), F("POINTS READY"));
    setLedMode(LED_SUCCESS); playCue(F("VALID")); return;
  }

  // ── Payment ──────────────────────────────────────────────────
  if (clean == F("CASH INSERTED")) {
    writeLcd(F("CASH ACCEPTED"), F("BALANCE UPDATED"));
    setLedMode(LED_CASH); playCue(F("CASH")); return;
  }
  if (clean == F("QR PAYMENT OK")) {
    writeLcd(F("QR PAYMENT OK"), F("BALANCE UPDATED"));
    setLedMode(LED_CASH); playCue(F("CASH")); return;
  }
  if (clean == F("POINT PAYMENT OK")) {
    writeLcd(F("POINT PAYMENT"), F("APPROVED"));
    setLedMode(LED_SUCCESS); playCue(F("SUCCESS")); return;
  }
  if (clean == F("POINT PAY READY")) {
    writeLcd(F("POINT PAY READY"), F("SELECT ITEM"));
    setLedMode(LED_READY); playCue(F("CLICK")); return;
  }
  if (clean == F("POINT PAY OFF")) {
    writeLcd(F("POINT PAY OFF"), F("CASH MODE"));
    setLedMode(LED_READY); playCue(F("CLICK")); return;
  }
  if (clean == F("CHANGE RETURNED")) {
    writeLcd(F("CHANGE RETURNED"), F("COLLECT COINS"));
    setLedMode(LED_CASH); playCue(F("CHANGE")); return;
  }

  // ── Receipt ──────────────────────────────────────────────────
  if (clean == F("PRINTING RECEIPT")) {
    writeLcd(F("PRINTING"), F("RECEIPT"));
    setLedMode(LED_SCANNING); playCue(F("RECEIPT")); return;
  }
  if (clean == F("RECEIPT COMPLETE")) {
    writeLcd(F("RECEIPT DONE"), F("THANK YOU"));
    setLedMode(LED_SUCCESS); playCue(F("SUCCESS")); return;
  }
  if (clean == F("RECEIPT FAILED")) {
    writeLcd(F("PRINT FAILED"), F("CHECK PRINTER"));
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }

  // ── RFID registration ────────────────────────────────────────
  if (clean == F("NEW USER REGISTER")) {
    writeLcd(F("NEW RFID CARD"), F("REGISTER NOW"));
    setLedMode(LED_SCANNING); playCue(F("CLICK")); return;
  }
  if (clean == F("REGISTER CANCELLED")) {
    writeLcd(F("REG CANCELLED"), F("TRY AGAIN"));
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }
  if (clean == F("CARD REGISTERED")) {
    writeLcd(F("CARD LINKED"), F("WELCOME ABOARD"));
    setLedMode(LED_SUCCESS); playCue(F("SUCCESS")); return;
  }

  // ── Substring matches (order matters) ────────────────────────
  if (clean.indexOf(F("DISPENS")) >= 0) {
    writeLcd(F("DISPENSING..."), F("PLEASE WAIT"));
    openLidServo(); return;  // LED_DISPENSING + DISPENSE cue set inside
  }
  if (clean.indexOf(F("TAKE YOUR ITEM")) >= 0) {
    writeLcd(F("ITEM DISPENSED!"), F("COLLECT & ENJOY"));
    setLedMode(LED_SUCCESS); return;
  }
  if (clean.indexOf(F("POINTS SAVED")) >= 0) {
    writeLcd(F("POINTS SAVED"), F("THANK YOU"));
    setLedMode(LED_SUCCESS); playCue(F("SUCCESS")); return;
  }
  if (clean.indexOf(F("POINTS TAP RFID")) >= 0) {
    writeLcd(F("ECO POINTS ADDED"), F("TAP RFID TO SAVE"));
    setLedMode(LED_SCANNING); playCue(F("CLICK")); return;
  }
  if (clean.indexOf(F("OFFLINE")) >= 0) {
    writeLcd(F("RFID OFFLINE"), F("TRY AGAIN"));
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }
  if (clean.indexOf(F("NOT ENOUGH")) >= 0) {
    writeLcd(F("INSERT MORE CASH"), F("TRY AGAIN"));
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }
  if (clean.indexOf(F("SOLD OUT")) >= 0) {
    writeLcd(F("SOLD OUT"), F("CHOOSE ANOTHER"));
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }
  if (clean.indexOf(F("POINTS ADDED")) >= 0) {
    writeLcdWrapped(clean);
    setLedMode(LED_CASH); playCue(F("CASH")); return;
  }
  if (clean.indexOf(F("ERROR")) >= 0) {
    writeLcdWrapped(clean);
    setLedMode(LED_ERROR); playCue(F("ERROR")); return;
  }

  // ── Fallback ─────────────────────────────────────────────────
  writeLcdWrapped(clean);
  setLedMode(LED_READY);
}

// ─────────────────────────────────────────────────────────────
//  Buzzer
// ─────────────────────────────────────────────────────────────
void buzzTone(unsigned int freq, unsigned int durationMs) {
  tone(PIN_BUZZER, freq, durationMs);
  delay(durationMs + 18);
  noTone(PIN_BUZZER);
}

void playCue(const String &cue) {
  if      (cue == F("READY"))    { buzzTone(988,  70);  buzzTone(1319, 90);  }
  else if (cue == F("SCAN"))     { buzzTone(1568, 55);  }
  else if (cue == F("CASH"))     { buzzTone(1760, 40);  buzzTone(2093, 55);  }
  else if (cue == F("VALID"))    { buzzTone(1047, 75);  buzzTone(1568, 95);  }
  else if (cue == F("INVALID"))  { buzzTone(220,  130); buzzTone(196,  150); }
  else if (cue == F("DISPENSE")) { buzzTone(784,  75);  buzzTone(988,  75);  buzzTone(1319, 120); }
  else if (cue == F("CHANGE"))   { buzzTone(1760, 32);  buzzTone(1397, 28);  buzzTone(1568, 32);
                                   buzzTone(1175, 35);  buzzTone(1319, 55);  }
  else if (cue == F("RECEIPT"))  { buzzTone(988,  45);  buzzTone(1175, 45);  buzzTone(988,  45);  }
  else if (cue == F("SUCCESS"))  { buzzTone(1047, 70);  buzzTone(1319, 70);  buzzTone(1568, 120); }
  else if (cue == F("ERROR"))    { buzzTone(196,  150); buzzTone(165,  170); }
  else if (cue == F("CLICK"))    { buzzTone(1400, 45);  }
}

// ─────────────────────────────────────────────────────────────
//  Servo
// ─────────────────────────────────────────────────────────────
void ensureServoAttached() {
  if (!servoAttached) {
    lidServo.attach(PIN_SERVO);
    delay(15);
    servoAttached = true;
  }
  servoDetachPending = false;
}

void scheduleServoDetach() {
  servoDetachAt      = millis() + SERVO_DETACH_DLY;
  servoDetachPending = true;
}

void closeLidServo() {
  if (!servoTipActive && !servoAttached) return;
  ensureServoAttached();
  lidServo.write(SERVO_CLOSED);
  servoTipActive = false;
  servoTipStep   = 0;
  scheduleServoDetach();

  // Show collect message and reset timer after dispense cycle
  if (ledMode == LED_DISPENSING) {
    writeLcd(F("ITEM DISPENSED!"), F("COLLECT & ENJOY"));
    setLedMode(systemMode == MODE_ACTIVE ? LED_SUCCESS : LED_AFK);
    showingMessage = true;
    messageTimer   = millis();
  }
}

void openLidServo() {
  ensureServoAttached();
  lidServo.write(SERVO_OPEN);
  servoStepAt        = millis();
  servoTipActive     = true;
  servoTipStep       = 0;
  servoDetachPending = false;
  setLedMode(LED_DISPENSING);
  playCue(F("DISPENSE"));
}

void updateServo() {
  if (servoTipActive && millis() - servoStepAt >= SERVO_TIP_STEP) {
    servoStepAt = millis();
    servoTipStep++;
    switch (servoTipStep) {
      case 1: lidServo.write(SERVO_CLOSED); break;
      case 2:
      case 3: lidServo.write(SERVO_OPEN);   break;
      case 4: closeLidServo();              break;
    }
  }

  if (servoDetachPending && (long)(millis() - servoDetachAt) >= 0) {
    lidServo.detach();
    servoAttached      = false;
    servoDetachPending = false;
  }
}

// ─────────────────────────────────────────────────────────────
//  Session / display helpers
// ─────────────────────────────────────────────────────────────
void resetDisplay() {
  waitingValidation   = false;
  rfidValidationStart = 0;
  showingMessage      = false;

  if (systemMode == MODE_ACTIVE) {
    writeLcd(F("ECO-MATIC READY"), F("CASH OR RECYCLE"));
    setLedMode(LED_READY);
  } else {
    writeLcd(F("ECO-MATIC IDLE"), F("START IN APP"));
    setLedMode(LED_AFK);
  }

  afkTimer = millis();
}

void armMessageTimer() {
  showingMessage = true;
  messageTimer   = millis();
}

void playReadyCueIfNeeded(bool modeChanged) {
  unsigned long now = millis();
  if (modeChanged || now - lastReadyCueAt >= READY_CUE_PERIOD) {
    lastReadyCueAt = now;
    playCue(F("READY"));
  }
}

// ─────────────────────────────────────────────────────────────
//  Serial command handler
// ─────────────────────────────────────────────────────────────
void handleCommand(const String &raw) {
  String msg = raw;
  msg.trim();
  if (msg.length() == 0) return;

  if (msg == F("STATE:ACTIVE")) {
    bool changed = (systemMode != MODE_ACTIVE);
    systemMode   = MODE_ACTIVE;
    resetDisplay();
    playReadyCueIfNeeded(changed);
    Serial.println(F("SESSION:ACTIVE"));
    return;
  }

  if (msg == F("STATE:AFK")) {
    systemMode = MODE_AFK;
    closeLidServo();
    resetDisplay();
    Serial.println(F("SESSION:AFK"));
    return;
  }

  if (msg == F("SERVO:OPEN")) {
    openLidServo();
    return;
  }

  if (msg == F("VALID") && systemMode == MODE_ACTIVE) {
    waitingValidation   = false;
    rfidValidationStart = 0;
    writeLcd(F("POINTS SAVED"), F("BAL UPDATED"));
    setLedMode(LED_SUCCESS);
    playCue(F("VALID"));
    armMessageTimer();
    return;
  }

  if (msg == F("INVALID") && systemMode == MODE_ACTIVE) {
    waitingValidation   = false;
    rfidValidationStart = 0;
    writeLcd(F("CARD NOT FOUND"), F("REGISTER FIRST"));
    setLedMode(LED_ERROR);
    playCue(F("INVALID"));
    armMessageTimer();
    return;
  }

  if (msg.startsWith(F("MSG:"))) {
    String payload = msg.substring(4);
    payload.trim();
    if (payload.length() == 0) return;

    String clean    = sanitize(payload);
    bool   isSilent = false;
    dispatchMsg(clean, isSilent);
    if (!isSilent) armMessageTimer();
    return;
  }
}

// ─────────────────────────────────────────────────────────────
//  setup()
// ─────────────────────────────────────────────────────────────
void setup() {
  Serial.begin(BAUD_RATE);
  delay(300);

  pinMode(PIN_LED_GREEN, OUTPUT);
  pinMode(PIN_LED_RED,   OUTPUT);
  pinMode(PIN_BUZZER,    OUTPUT);
  setLed(false, false);
  setLedMode(LED_BOOT);

  initLcd();

  SPI.begin();
  rfid.PCD_Init();
  delay(30);

  Serial.print(F("RFID:FIRMWARE_VERSION="));
  rfid.PCD_DumpVersionToSerial();

  byte ver = rfid.PCD_ReadRegister(rfid.VersionReg);
  if (ver == 0x00 || ver == 0xFF) {
    Serial.println(F("RFID:NOT_FOUND_CHECK_SPI_WIRING"));
    writeLcd(F("RFID NOT FOUND"), F("CHECK WIRING"));
    setLedMode(LED_ERROR);
  } else {
    Serial.println(F("RFID:READY"));
    resetDisplay();
  }

  // Park servo closed then detach to stop jitter
  lidServo.attach(PIN_SERVO);
  lidServo.write(SERVO_CLOSED);
  servoAttached = true;
  scheduleServoDetach();

  Serial.println(F("SYSTEM:READY"));
}

// ─────────────────────────────────────────────────────────────
//  loop()
// ─────────────────────────────────────────────────────────────
void loop() {
  updateServo();
  updateLedState();

  while (Serial.available() > 0) {
    String line = Serial.readStringUntil('\n');
    handleCommand(line);
  }

  // ── AFK: rotate eco-facts, no RFID scanning ──────────────────
  if (systemMode == MODE_AFK) {
    if (millis() - afkTimer > AFK_CYCLE_TIME) {
      afkTimer = millis();
      afkFrame = (afkFrame + 1) % AFK_FACT_COUNT;
      showAfkFrame();
    }
    return;
  }

  // ── Active mode ──────────────────────────────────────────────

  // Expire timed messages → return to idle screen
  if (showingMessage && millis() - messageTimer > MSG_DISPLAY_TIME) {
    resetDisplay();
  }

  // Block new scans while waiting for PC validation response
  if (waitingValidation) {
    if (millis() - rfidValidationStart > RFID_TIMEOUT) {
      Serial.println(F("RFID:PC_RESPONSE_TIMEOUT"));
      writeLcd(F("RFID TIMEOUT"), F("TRY AGAIN"));
      playCue(F("ERROR"));
      waitingValidation   = false;
      rfidValidationStart = 0;
      setLedMode(LED_ERROR);
      armMessageTimer();
    }
    return;
  }

  // Poll RFID
  if (!rfid.PICC_IsNewCardPresent() || !rfid.PICC_ReadCardSerial()) return;

  String uid = "";
  for (byte i = 0; i < rfid.uid.size; i++) {
    if (rfid.uid.uidByte[i] < 0x10) uid += '0';
    uid += String(rfid.uid.uidByte[i], HEX);
  }
  uid.toUpperCase();

  Serial.print(F("RFID:"));
  Serial.println(uid);

  writeLcd(F("CARD SCANNED"), F("CHECKING..."));
  playCue(F("SCAN"));
  waitingValidation   = true;
  rfidValidationStart = millis();
  setLedMode(LED_SCANNING);

  rfid.PICC_HaltA();
  rfid.PCD_StopCrypto1();
}
