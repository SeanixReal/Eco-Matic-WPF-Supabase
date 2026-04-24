# ESP32-S3 Wiring Guide

This guide matches `ESP32S3_RFID_Scanner.ino` and targets an ESP32-S3 N16R8-style development board.

## Build Order

1. ESP32-S3 over USB only
2. LCD1602 I2C
3. RC522 RFID
4. Green/red LEDs
5. MAX98357A amplifier and speaker

Bring up one part at a time. If the LCD shows garbage characters, test the LCD by itself before adding RFID or audio.

## Pin Map

- `LCD SDA` -> `GPIO 8`
- `LCD SCL` -> `GPIO 9`
- `RC522 SS / SDA` -> `GPIO 10`
- `RC522 MOSI` -> `GPIO 11`
- `RC522 SCK` -> `GPIO 12`
- `RC522 MISO` -> `GPIO 13`
- `RC522 RST` -> `GPIO 14`
- `Green LED` -> `GPIO 16`
- `Red LED` -> `GPIO 17`
- `MAX98357A BCLK` -> `GPIO 4`
- `MAX98357A LRC / WS` -> `GPIO 5`
- `MAX98357A DIN` -> `GPIO 6`

Avoid using `GPIO0`, `GPIO3`, `GPIO19`, `GPIO20`, `GPIO35` through `GPIO38`, `GPIO45`, `GPIO46`, and `GPIO48` for this build.

## Power

- Power the ESP32-S3 from USB while developing.
- Use `3V3` for the RC522.
- Use `3V3` for the LCD1602 I2C backpack if it is readable and stable.
- Use `5V` for the MAX98357A `VIN`.
- Connect all grounds together.
- Do not power the speaker amplifier from the ESP32 `3V3` pin.

## LCD1602 I2C

Your LCD was confirmed working on Arduino at `5V`, but it only shows dim backlight/no visible text on ESP32 at `3.3V`. Use the `5V + level shifter` wiring for this module.

Recommended wiring:

- `LCD VCC` -> `5V`
- `LCD GND` -> `ESP32 GND`
- `ESP32 GPIO8` -> level shifter low-voltage `LV1`
- `ESP32 GPIO9` -> level shifter low-voltage `LV2`
- level shifter high-voltage `HV1` -> `LCD SDA`
- level shifter high-voltage `HV2` -> `LCD SCL`
- level shifter `LV` -> `ESP32 3V3`
- level shifter `HV` -> `5V`
- level shifter `GND` -> common `GND`

The sketch scans the I2C bus and supports common LCD backpack addresses `0x27` and `0x3F`. It uses standard-mode I2C at `100 kHz`.

If the LCD shows garbage characters:

- adjust the blue contrast potentiometer on the I2C backpack
- confirm `SDA` and `SCL` are not swapped
- keep SDA/SCL jumpers short
- confirm ESP32 GND and LCD GND are common
- confirm the serial monitor shows an I2C device at `0x27` or `0x3F`
- try the LCD alone before connecting RFID/audio
- if powering the LCD backpack at `5V`, use a bidirectional I2C level shifter for SDA/SCL

If the serial monitor detects `LCD:I2C_DEVICE=0x27` or `0x3F` but the screen only lights up/blinks with no visible text:

- slowly turn the small blue contrast potentiometer on the LCD I2C backpack until dark character blocks or text appear
- test the LCD with only `VCC`, `GND`, `SDA`, and `SCL` connected
- disconnect the MAX98357A temporarily to remove speaker power dips while testing the display
- many LCD1602 modules light up at `3.3V` but the LCD glass/controller still needs `5V` for readable characters
- because this LCD already worked on Arduino `5V`, power the LCD from `5V` and put a bidirectional I2C level shifter between ESP32 `GPIO8/GPIO9` and LCD `SDA/SCL`

## RC522 RFID

RC522 is a `3.3V` device.

- `RC522 3.3V` -> `ESP32 3V3`
- `RC522 GND` -> `ESP32 GND`
- `RC522 SDA / SS` -> `GPIO 10`
- `RC522 MOSI` -> `GPIO 11`
- `RC522 SCK` -> `GPIO 12`
- `RC522 MISO` -> `GPIO 13`
- `RC522 RST` -> `GPIO 14`
- `RC522 IRQ` -> leave unconnected

RFID scans are intentionally ignored until the WPF app sends `STATE:ACTIVE`, which happens when customer mode opens.

## LEDs

Use a resistor on each LED.

- `GPIO 16` -> `220 to 330 ohm resistor` -> `green LED anode`
- `GPIO 17` -> `220 to 330 ohm resistor` -> `red LED anode`
- both LED cathodes -> `GND`

## MAX98357A + 4 Ohm 3W Speaker

- `MAX98357A VIN` -> `5V`
- `MAX98357A GND` -> `GND`
- `MAX98357A BCLK` -> `GPIO 4`
- `MAX98357A LRC / WS` -> `GPIO 5`
- `MAX98357A DIN` -> `GPIO 6`
- speaker wires -> amplifier output terminals

For the purple MAX98357A breakout with the green screw terminal:

- the small header pins labeled `VIN`, `GND`, `BCLK`, `DIN`, and `LRC` go to the ESP32-S3 and power rails
- the green 2-screw terminal is only for the speaker
- speaker red wire -> green terminal side marked `+` / `SPK+`
- speaker black wire -> green terminal side marked `-` / `SPK-`
- if the speaker has no clear polarity, either direction will still make sound, but use red as `+` and black as `-` when available
- do not connect either speaker wire directly to ESP32 `GND`

Add your `470uF 16V` electrolytic capacitor across the amplifier power rail:

- capacitor positive leg -> `5V`
- capacitor negative leg -> `GND`
- place it physically near the `MAX98357A VIN` and `GND` pins if possible

Capacitor polarity:

- the negative side is usually marked by a stripe with `-` symbols on the capacitor body
- the positive leg is usually the longer leg when the capacitor is new
- the negative striped side must go to `GND`

Power/capacitor/speaker schematic:

```text
ESP32-S3                         MAX98357A AMP
--------                         ------------
5V  ---------------------------> VIN
 |
 +---- capacitor +

GND ---------------------------> GND
 |
 +---- capacitor -  (striped side)

GPIO4 -------------------------> BCLK
GPIO5 -------------------------> LRC / WS
GPIO6 -------------------------> DIN

                                  GREEN SCREW TERMINAL
                                  --------------------
Speaker red wire   ------------> SPK+ / +
Speaker black wire ------------> SPK- / -
```

Same idea using breadboard rails:

```text
5V rail:   ESP32 5V, MAX98357A VIN, capacitor +
GND rail:  ESP32 GND, MAX98357A GND, capacitor -
```

The firmware plays short generated tones for ready, scan, valid, invalid, error, and dispense events. Voice clips can be added later through LittleFS WAV playback.

## First Power-On Checklist

1. Flash the ESP32-S3 sketch over USB.
2. Open Serial Monitor at `115200`.
3. Confirm the serial log shows `LCD:USING_ADDRESS`.
4. Confirm the LCD shows stable idle text.
5. Open customer mode in WPF and confirm the LCD switches to ready text.
6. Tap an RFID card in customer mode and check for `RFID:<UID>` in serial.
7. Verify LEDs respond for valid, invalid, and error states.
8. Connect MAX98357A last and confirm the speaker plays tones without resetting the ESP32.
