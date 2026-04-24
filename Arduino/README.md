# Eco-Matic Hardware Sketches

This folder contains the hardware sketches for the Eco-Matic RFID/LCD module.

Recommended deadline target:

- `RFID_Scanner/` for Arduino Uno/Nano, RC522 RFID, LCD1602 I2C, and green/red LEDs

Experimental/future target:

- `ESP32S3_RFID_Scanner/` for ESP32-S3, RC522 RFID, LCD1602 I2C, LEDs, and MAX98357A speaker tones

Your LCD1602 has been confirmed working on Arduino at `5V`, so the Arduino Uno/Nano path is the safer demo build.

## Current Arduino Hardware

- Arduino Uno or Nano
- RC522 RFID module
- LCD1602 I2C backpack
- passive 5V buzzer
- green LED and red LED with `220-330 ohm` resistors
- optional SG90 micro servo for a lightweight trapdoor dispenser
- RFID cards or key fobs

## Arduino Uno/Nano Wiring

### RC522 RFID

The RC522 is a `3.3V` module. Do not connect its power pin to `5V`.

- `RC522 SDA / SS` -> Arduino `D10`
- `RC522 SCK` -> Arduino `D13`
- `RC522 MOSI` -> Arduino `D11`
- `RC522 MISO` -> Arduino `D12`
- `RC522 RST` -> Arduino `D9`
- `RC522 3.3V` -> Arduino `3.3V`
- `RC522 GND` -> Arduino `GND`
- `RC522 IRQ` -> not connected

### LCD1602 I2C

- `LCD VCC` -> Arduino `5V`
- `LCD GND` -> Arduino `GND`
- `LCD SDA` -> Arduino `A4`
- `LCD SCL` -> Arduino `A5`

The sketch scans for common LCD addresses `0x27` and `0x3F`.

### LEDs

- Arduino `D6` -> `220-330 ohm resistor` -> green LED anode
- Arduino `D7` -> `220-330 ohm resistor` -> red LED anode
- both LED cathodes -> `GND`

### Passive Buzzer

Use a passive buzzer if you want different tones for scan, success, error, dispense, and change feedback.

- buzzer `+` -> Arduino `D4`
- buzzer `-` -> Arduino `GND`

Optional safer wiring:

- Arduino `D4` -> `100 ohm resistor` -> buzzer `+`
- buzzer `-` -> Arduino `GND`

The sketch uses short tone patterns:

- customer mode ready: rising two-tone beep
- RFID scanned: quick tick
- valid RFID: success beep
- invalid/new RFID: low error buzz
- dispense/lid open: playful rising tone
- cash inserted: quick confirmation tick
- receipt printing: light printer-style triple beep
- change returned: fast coin-drop pattern
- timeout/error: low warning buzz

Passive buzzer loudness depends mostly on the buzzer itself and the frequency it resonates at. If it is too quiet, try mounting it to the cardboard box so the box acts like a small resonator, or try another passive buzzer with a louder rated sound output.

### Optional SG90 Lid Servo

Use the SG90 to open a lightweight cardboard side/top flap like a small hat lid. This is easier and safer than a real item dropper.

- SG90 signal wire, usually orange/yellow -> Arduino `D5`
- SG90 red wire -> external regulated `5V`
- SG90 brown/black wire -> external supply `GND`
- external supply `GND` -> Arduino `GND`

Servo wiring diagram:

```text
Arduino Uno/Nano                  SG90 Servo
----------------                  ----------
D5 -----------------------------> orange/yellow signal

External 5V + ------------------> red
External 5V - / GND ------------> brown/black
        |
        +-----------------------> Arduino GND
```

If you do not have a separate 5V supply yet, you can test a tiny unloaded SG90 from Arduino `5V`, but use this only for quick testing:

```text
Arduino D5  -> SG90 orange/yellow signal
Arduino 5V  -> SG90 red
Arduino GND -> SG90 brown/black
```

If the LCD flickers, Arduino resets, or the servo jitters, move the servo red/brown wires to an external 5V supply and keep the external `GND` connected to Arduino `GND`.

Optional capacitor for servo power:

```text
470uF capacitor + / longer leg      -> servo 5V / red wire rail
470uF capacitor - / striped side    -> servo GND / brown-black wire rail
```

Important:

- do not power the SG90 from the Arduino `5V` pin if it jitters or resets the Arduino
- always connect the external servo power ground to Arduino ground
- add a `470uF` or larger capacitor across the servo power supply `5V` and `GND` if the servo causes resets
- the capacitor positive leg goes to `5V`; the striped negative side goes to `GND`

Recommended horn:

- use the single-arm horn for a simple cardboard lid because it is easier to tape or hot-glue to one flap
- use the dual-arm horn only if you need a wider contact area or want to push the lid from the middle
- the sketch currently uses the inside-mounted motion: closed at about `15 degrees`, open at about `95 degrees`
- for a back-mounted servo with the horn starting vertical/up-down, change `SERVO_MOUNT_MODE` in the sketch from `SERVO_MOUNT_INSIDE` to `SERVO_MOUNT_BACK`
- if the back-mounted servo opens the wrong direction, change `SERVO_BACK_OPEN_ANGLE` from `0` to `180`

Simple lid mechanism:

```text
Inside-mounted servo, current active option:

    box wall / lid
    +----------+
    |          |  <- single-arm horn taped/glued to the flap
    +----------+
         \
          SG90 inside the box

On purchase:

    servo turns -> lid opens -> waits briefly -> lid closes
```

Back-mounted servo option:

```text
Back of cardboard box

       SG90 outside/back
          |
          | horn starts vertical, up/down
          |
    +-----+----+
    | cardboard |
    | lid/flap  |
    +----------+

In code:

    SERVO_MOUNT_MODE = SERVO_MOUNT_BACK
    closed angle = 90
    open angle = 0, or 180 if the hinge moves the other way
```

The Arduino sketch opens the lid automatically when the WPF app sends a purchase display message containing `DISPENSING` or `TAKE YOUR ITEM`.

## WPF Serial Bridge

- Default port: `COM5`
- Default baud: `9600`
- Optional `.env` overrides:
  - `ECOMATIC_ARDUINO_PORT`
  - `ECOMATIC_ARDUINO_BAUD`

The Arduino sends RFID scans as `RFID:<UID>`. The WPF app sends `STATE:ACTIVE`, `STATE:AFK`, `VALID`, `INVALID`, and `MSG:<text>` commands back to the Arduino for LCD and LED feedback. A successful purchase opens the optional SG90 lid because the WPF app sends a `DISPENSING` message.

When customer mode opens, WPF sends `STATE:ACTIVE` immediately and repeats it once shortly after. This prevents the Arduino from staying in AFK mode if the board reset or was still booting when the first command arrived.

RFID scans are accepted only while customer mode is open. Customers without RFID can still earn points during the session and see them on the receipt, but only a registered RFID card can save those points to Supabase `customers.eco_credits`.

## Speaker Note

The MAX98357A speaker module is an I2S amplifier, which is a good match for ESP32-S3 but not a practical Arduino Uno/Nano deadline feature. For the Arduino build, use the LCD, LEDs, and optional SG90 trapdoor as the customer-facing feedback. Add the speaker later if you return to ESP32-S3 or use a simpler buzzer module.

## Arduino IDE Setup

Install:

1. `MFRC522`
2. `LiquidCrystal I2C`

Then flash `Arduino/RFID_Scanner/RFID_Scanner.ino` and open Serial Monitor at `9600`.
