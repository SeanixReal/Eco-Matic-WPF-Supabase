# Eco-Matic ESP32-S3 Hardware Module

This sketch is the current hardware path for Eco-Matic. It replaces the older Arduino Uno RFID module with an ESP32-S3 N16R8-style board and keeps the same USB serial bridge used by the WPF app.

## Hardware

- ESP32-S3 N16R8 development board
- LCD1602 with I2C backpack
- RC522 RFID reader
- green LED and red LED
- MAX98357A I2S amplifier
- 4 ohm 3W speaker

Servo support is intentionally out of scope for this build.

## Serial Settings

- Baud rate: `115200`
- WPF default port: `COM5`
- Optional WPF settings:
  - `ECOMATIC_ARDUINO_PORT`
  - `ECOMATIC_ARDUINO_BAUD`

## Serial Protocol

ESP32 to PC:

- `RFID:<UID>`

PC to ESP32:

- `STATE:ACTIVE`
- `STATE:AFK`
- `VALID`
- `INVALID`
- `MSG:<text>`

RFID scans are only sent while the ESP32 is in `STATE:ACTIVE`. In Eco-Matic, that means customer mode is open.

## Pin Map

- LCD SDA -> `GPIO 8`
- LCD SCL -> `GPIO 9`
- RC522 SS/SDA -> `GPIO 10`
- RC522 MOSI -> `GPIO 11`
- RC522 SCK -> `GPIO 12`
- RC522 MISO -> `GPIO 13`
- RC522 RST -> `GPIO 14`
- green LED -> `GPIO 16`
- red LED -> `GPIO 17`
- MAX98357A BCLK -> `GPIO 4`
- MAX98357A LRC/WS -> `GPIO 5`
- MAX98357A DIN -> `GPIO 6`

Avoid `GPIO0`, `GPIO3`, `GPIO19`, `GPIO20`, `GPIO35` through `GPIO38`, `GPIO45`, `GPIO46`, and `GPIO48` for this deadline wiring.

## LCD Notes

The LCD uses I2C standard mode at `100 kHz`. The sketch scans for the LCD address at boot and prefers common backpack addresses `0x27` and `0x3F`.

If the LCD shows random symbols or emoticon-like characters, check:

- contrast potentiometer
- SDA/SCL order
- common ground
- short I2C jumper wires
- stable 3.3V power
- detected I2C address in Serial Monitor
- level shifting if the LCD backpack is powered from 5V

## Audio Notes

The sketch uses the MAX98357A through I2S and plays generated tones for:

- ready
- RFID scanned
- valid card
- invalid card
- error/offline
- dispense

Japanese train-station-style voice clips are a good follow-up, but they should be added later through LittleFS WAV files after the base hardware is stable.

## Arduino IDE Libraries

Install:

- `MFRC522`
- `LiquidCrystal I2C`
- ESP32 board support package

The I2S audio driver is provided by the ESP32 board support package.

## Bring-Up

1. Flash `ESP32S3_RFID_Scanner.ino`.
2. Open Serial Monitor at `115200`.
3. Confirm the LCD address is detected.
4. Confirm the LCD shows idle text.
5. Open Eco-Matic customer mode and confirm the display switches to ready text.
6. Tap an RFID card and confirm `RFID:<UID>` appears.
7. Verify valid/invalid LEDs and sounds.
8. Add the MAX98357A only after LCD and RFID are stable.

See [WIRING.md](./WIRING.md) for the pin-by-pin wiring guide.
