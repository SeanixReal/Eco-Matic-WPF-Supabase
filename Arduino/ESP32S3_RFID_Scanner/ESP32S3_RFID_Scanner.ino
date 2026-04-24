#include <SPI.h>
#include <MFRC522.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <driver/i2s.h>

// ---------------------------------------------------------------------------
// Eco-Matic ESP32-S3 RFID + LCD + LEDs + MAX98357A
//
// Serial protocol used by the WPF desktop app:
//   -> PC receives:  RFID:A1B2C3D4
//   <- PC sends:     VALID
//   <- PC sends:     INVALID
//   <- PC sends:     STATE:ACTIVE
//   <- PC sends:     STATE:AFK
//   <- PC sends:     MSG:<up to 32 LCD chars>
//
// Hardware target:
//   ESP32-S3 N16R8-style development board
//   LCD1602 I2C backpack at 3.3V
//   RC522 RFID reader
//   green/red LEDs
//   MAX98357A I2S amplifier with 4 ohm 3W speaker
//
// Notes:
// - RC522 is 3.3V only.
// - If an LCD backpack is powered at 5V, use an I2C level shifter.
// - RFID scans are accepted only while the WPF app has customer mode open.
// ---------------------------------------------------------------------------

// ---------------------------
// User-configurable pin map
// ---------------------------
static const int I2C_SDA_PIN = 8;
static const int I2C_SCL_PIN = 9;

static const int RC522_SS_PIN   = 10; // SDA / SS on RC522
static const int RC522_MOSI_PIN = 11;
static const int RC522_SCK_PIN  = 12;
static const int RC522_MISO_PIN = 13;
static const int RC522_RST_PIN  = 14;

static const int GREEN_LED_PIN = 16;
static const int RED_LED_PIN   = 17;

static const int I2S_BCLK_PIN = 4;
static const int I2S_LRC_PIN  = 5;
static const int I2S_DIN_PIN  = 6;

static const uint8_t DEFAULT_LCD_ADDRESS = 0x27;
static const uint8_t FALLBACK_LCD_ADDRESS = 0x3F;
static const int LCD_COLUMNS = 16;
static const int LCD_ROWS = 2;
static const uint32_t I2C_CLOCK_HZ = 100000;
static const uint32_t SERIAL_BAUD_RATE = 115200;

static const i2s_port_t I2S_PORT = I2S_NUM_0;
static const int AUDIO_SAMPLE_RATE = 22050;
static const int AUDIO_VOLUME = 9000;

LiquidCrystal_I2C *lcd = nullptr;
MFRC522 mfrc522(RC522_SS_PIN, RC522_RST_PIN);

unsigned long afkTimer = 0;
unsigned long messageTimer = 0;
unsigned long activeLedTimer = 0;

bool lcdReady = false;
bool audioReady = false;
bool showingMessage = false;
bool waitingValidation = false;
bool lastScanWasValid = false;
bool activeHeartbeatOn = false;
bool activeBlinkState = false;

// 0 = AFK mode, 1 = active customer vending mode
int systemMode = 0;
int afkFrame = 0;

const char *afkFacts[][2] = {
  {"ECO-MATIC IDLE", "RECYCLE TODAY "},
  {"BOTTLES = PTS ", "TAP START IN APP"},
  {"CANS CAN EARN", "ECO CREDITS   "},
  {"BRING CLEAN PET", "SAVE IT TO RFID"},
  {"REDUCE REUSE  ", "RECYCLE DAILY "},
  {"GREEN CHOICES ", "START IN APP  "}
};
static const int AFK_FACT_COUNT = sizeof(afkFacts) / sizeof(afkFacts[0]);

String sanitizeLcdText(const String &value) {
  String output = "";
  output.reserve(value.length());

  for (size_t i = 0; i < value.length(); i++) {
    char c = value[i];
    if (c >= 32 && c <= 126) {
      output += c;
    } else {
      output += ' ';
    }
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
  writeLcdLines(fitLcdLine(clean, 0), fitLcdLine(clean, LCD_COLUMNS));
}

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

void initLcd() {
  Wire.begin(I2C_SDA_PIN, I2C_SCL_PIN);
  Wire.setClock(I2C_CLOCK_HZ);
  delay(300);

  uint8_t detectedAddress = scanLcdAddress();
  if (detectedAddress == 0) {
    detectedAddress = DEFAULT_LCD_ADDRESS;
    Serial.println("LCD:NOT_FOUND_USING_DEFAULT_0x27");
  } else {
    Serial.print("LCD:USING_ADDRESS=0x");
    if (detectedAddress < 16) {
      Serial.print("0");
    }
    Serial.println(detectedAddress, HEX);
  }

  lcd = new LiquidCrystal_I2C(detectedAddress, LCD_COLUMNS, LCD_ROWS);
  for (int attempt = 1; attempt <= 3; attempt++) {
    lcd->init();
    lcd->backlight();
    delay(100);
  }

  lcdReady = true;
  writeLcdLines("ECO-MATIC BOOT", "LCD READY");
}

void initAudio() {
  i2s_config_t i2sConfig = {
    .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
    .sample_rate = AUDIO_SAMPLE_RATE,
    .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
    .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
    .communication_format = I2S_COMM_FORMAT_STAND_I2S,
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
    .dma_buf_count = 4,
    .dma_buf_len = 128,
    .use_apll = false,
    .tx_desc_auto_clear = true,
    .fixed_mclk = 0
  };

  i2s_pin_config_t pinConfig = {
    .bck_io_num = I2S_BCLK_PIN,
    .ws_io_num = I2S_LRC_PIN,
    .data_out_num = I2S_DIN_PIN,
    .data_in_num = I2S_PIN_NO_CHANGE
  };

  esp_err_t result = i2s_driver_install(I2S_PORT, &i2sConfig, 0, nullptr);
  if (result == ESP_OK) {
    result = i2s_set_pin(I2S_PORT, &pinConfig);
  }

  audioReady = result == ESP_OK;
  Serial.println(audioReady ? "AUDIO:READY" : "AUDIO:INIT_FAILED");
}

void playTone(int frequencyHz, int durationMs) {
  if (!audioReady || frequencyHz <= 0 || durationMs <= 0) {
    return;
  }

  const int totalSamples = (AUDIO_SAMPLE_RATE * durationMs) / 1000;
  const float phaseStep = 2.0f * PI * frequencyHz / AUDIO_SAMPLE_RATE;
  float phase = 0.0f;
  int16_t sampleBuffer[128];

  int samplesWritten = 0;
  while (samplesWritten < totalSamples) {
    int chunk = min(128, totalSamples - samplesWritten);
    for (int i = 0; i < chunk; i++) {
      float envelope = 1.0f;
      int absoluteIndex = samplesWritten + i;
      int fadeSamples = min(400, totalSamples / 6);
      if (fadeSamples > 0 && absoluteIndex < fadeSamples) {
        envelope = (float)absoluteIndex / fadeSamples;
      } else if (fadeSamples > 0 && absoluteIndex > totalSamples - fadeSamples) {
        envelope = (float)(totalSamples - absoluteIndex) / fadeSamples;
      }

      sampleBuffer[i] = (int16_t)(sin(phase) * AUDIO_VOLUME * envelope);
      phase += phaseStep;
      if (phase > 2.0f * PI) {
        phase -= 2.0f * PI;
      }
    }

    size_t bytesWritten = 0;
    i2s_write(I2S_PORT, sampleBuffer, chunk * sizeof(int16_t), &bytesWritten, portMAX_DELAY);
    samplesWritten += chunk;
  }

  i2s_zero_dma_buffer(I2S_PORT);
}

void playSilence(int durationMs) {
  if (!audioReady || durationMs <= 0) {
    return;
  }

  int16_t silence[96] = {0};
  int totalSamples = (AUDIO_SAMPLE_RATE * durationMs) / 1000;
  int samplesWritten = 0;
  while (samplesWritten < totalSamples) {
    int chunk = min(96, totalSamples - samplesWritten);
    size_t bytesWritten = 0;
    i2s_write(I2S_PORT, silence, chunk * sizeof(int16_t), &bytesWritten, portMAX_DELAY);
    samplesWritten += chunk;
  }
}

void playCue(const String &cue) {
  if (cue == "READY") {
    playTone(659, 80);
    playSilence(35);
    playTone(880, 110);
  } else if (cue == "SCAN") {
    playTone(988, 80);
  } else if (cue == "VALID") {
    playTone(784, 90);
    playSilence(35);
    playTone(1046, 130);
  } else if (cue == "INVALID") {
    playTone(220, 160);
    playSilence(35);
    playTone(196, 180);
  } else if (cue == "ERROR") {
    playTone(165, 120);
    playSilence(40);
    playTone(165, 120);
    playSilence(40);
    playTone(165, 180);
  } else if (cue == "DISPENSE") {
    playTone(523, 80);
    playSilence(30);
    playTone(659, 80);
    playSilence(30);
    playTone(784, 140);
  }
}

void resetDisplay() {
  waitingValidation = false;
  showingMessage = false;
  lastScanWasValid = false;
  activeLedTimer = millis();
  activeHeartbeatOn = false;
  activeBlinkState = false;

  if (systemMode == 1) {
    writeLcdLines("ECO-MATIC READY", "ADD CASH/RECYCLE");
    digitalWrite(GREEN_LED_PIN, HIGH);
    digitalWrite(RED_LED_PIN, LOW);
    playCue("READY");
  } else {
    writeLcdLines("ECO-MATIC IDLE", "START IN APP");
    digitalWrite(GREEN_LED_PIN, LOW);
    digitalWrite(RED_LED_PIN, LOW);
  }

  afkTimer = millis();
}

void showAfkFrame() {
  writeLcdLines(afkFacts[afkFrame][0], afkFacts[afkFrame][1]);
  digitalWrite(GREEN_LED_PIN, afkFrame % 2 == 0 ? HIGH : LOW);
  digitalWrite(RED_LED_PIN, afkFrame % 2 != 0 ? HIGH : LOW);
}

void updateActiveLedState() {
  unsigned long now = millis();

  if (showingMessage) {
    if (lastScanWasValid) {
      digitalWrite(GREEN_LED_PIN, HIGH);
      digitalWrite(RED_LED_PIN, LOW);
    } else {
      if (now - activeLedTimer >= 180) {
        activeLedTimer = now;
        activeBlinkState = !activeBlinkState;
        digitalWrite(GREEN_LED_PIN, LOW);
        digitalWrite(RED_LED_PIN, activeBlinkState ? HIGH : LOW);
      }
    }
    return;
  }

  if (waitingValidation) {
    if (now - activeLedTimer >= 110) {
      activeLedTimer = now;
      activeBlinkState = !activeBlinkState;
      digitalWrite(GREEN_LED_PIN, activeBlinkState ? HIGH : LOW);
      digitalWrite(RED_LED_PIN, activeBlinkState ? HIGH : LOW);
    }
    return;
  }

  unsigned long interval = activeHeartbeatOn ? 120 : 760;
  if (now - activeLedTimer >= interval) {
    activeLedTimer = now;
    activeHeartbeatOn = !activeHeartbeatOn;
    digitalWrite(GREEN_LED_PIN, activeHeartbeatOn ? HIGH : LOW);
    digitalWrite(RED_LED_PIN, LOW);
  }
}

void handleIncomingCommand(const String &incoming) {
  String msg = incoming;
  msg.trim();
  if (msg.length() == 0) {
    return;
  }

  if (msg == "STATE:ACTIVE") {
    systemMode = 1;
    resetDisplay();
    return;
  }

  if (msg == "STATE:AFK") {
    systemMode = 0;
    resetDisplay();
    return;
  }

  if (msg.startsWith("MSG:")) {
    String customMsg = msg.substring(4);
    customMsg.trim();
    writeWrappedMessage(customMsg);

    String cleanMsg = sanitizeLcdText(customMsg);
    if (cleanMsg.indexOf("DISPENS") >= 0 || cleanMsg.indexOf("TAKE YOUR ITEM") >= 0) {
      playCue("DISPENSE");
    } else if (cleanMsg.indexOf("ERROR") >= 0 || cleanMsg.indexOf("OFFLINE") >= 0 || cleanMsg.indexOf("FAILED") >= 0) {
      playCue("ERROR");
    }

    showingMessage = true;
    messageTimer = millis();
    return;
  }

  if (systemMode == 1 && msg == "VALID") {
    writeLcdLines("POINTS SAVED", "BAL UPDATED");
    waitingValidation = false;
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

  pinMode(GREEN_LED_PIN, OUTPUT);
  pinMode(RED_LED_PIN, OUTPUT);
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(RED_LED_PIN, LOW);

  initLcd();
  initAudio();

  SPI.begin(RC522_SCK_PIN, RC522_MISO_PIN, RC522_MOSI_PIN, RC522_SS_PIN);
  mfrc522.PCD_Init();
  delay(20);

  Serial.print("RFID:FIRMWARE_VERSION=");
  mfrc522.PCD_DumpVersionToSerial();

  byte version = mfrc522.PCD_ReadRegister(mfrc522.VersionReg);
  if (version == 0x00 || version == 0xFF) {
    Serial.println("RFID:NOT_FOUND_CHECK_SPI_WIRING");
    writeLcdLines("RFID NOT FOUND", "CHECK WIRING");
    playCue("ERROR");
  } else {
    Serial.println("RFID:READY");
  }

  resetDisplay();
  Serial.println("SYSTEM:READY");
}

void loop() {
  while (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    handleIncomingCommand(msg);
  }

  if (systemMode == 1 &&
      !waitingValidation &&
      mfrc522.PICC_IsNewCardPresent() &&
      mfrc522.PICC_ReadCardSerial()) {
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
    activeLedTimer = millis();
    activeBlinkState = true;
    digitalWrite(GREEN_LED_PIN, HIGH);
    digitalWrite(RED_LED_PIN, HIGH);

    mfrc522.PICC_HaltA();
    mfrc522.PCD_StopCrypto1();
  }

  if (systemMode == 0) {
    if (millis() - afkTimer > 3000) {
      afkTimer = millis();
      afkFrame = (afkFrame + 1) % AFK_FACT_COUNT;
      showAfkFrame();
    }
  } else {
    if (showingMessage && millis() - messageTimer > 3000) {
      resetDisplay();
    }
    updateActiveLedState();
  }

  if (waitingValidation && (millis() - activeLedTimer > 4000)) {
    Serial.println("RFID:PC_RESPONSE_TIMEOUT");
    writeLcdLines("RFID TIMEOUT", "TRY AGAIN");
    waitingValidation = false;
    lastScanWasValid = false;
    showingMessage = true;
    messageTimer = millis();
    activeLedTimer = millis();
    playCue("ERROR");
  }
}
