# Eco-Matic RFID Scanner with LCD and LEDs

This folder contains the updated Arduino source code for the Eco-Matic Trash-to-Credit system's RFID module, complete with Visual Indicators (LCD & LEDs).

## Hardware Requirements
*   Arduino Uno (or Nano)
*   RFID-RC522 Module (3.3V)
*   I2C LCD Display (16x2, 5V)
*   1x Red LED + 220 ohm resistor
*   1x Green LED + 220 ohm resistor
*   RFID Cards or Key Fobs (13.56 MHz)

## Wiring Guide
### 1. RFID-RC522
**WARNING:** The RC522 operates on 3.3V. Do NOT connect it to the 5V pin!
*   **SDA (SS)** -> Pin 10
*   **SCK** -> Pin 13
*   **MOSI** -> Pin 11
*   **MISO** -> Pin 12
*   **IRQ** -> Unconnected
*   **GND** -> GND
*   **RST** -> Pin 9
*   **3.3V** -> 3.3V

### 2. I2C LCD Display
The I2C LCD requires only 4 pins. It uses the I2C bus pins strictly defined by your Arduino board.
*   **VCC** -> 5V
*   **GND** -> GND
*   **SDA** -> A4 (Analog Pin 4)
*   **SCL** -> A5 (Analog Pin 5)

### 3. LED Confirmation Lights
Always connect LEDs with a resistor (220 to 330 ohm) in series to prevent burning them out!
*   **Green LED Anode (+)** -> Pin 6
*   **Red LED Anode (+)** -> Pin 7
*   **Both LED Cathodes (-)** -> GND

## Arduino IDE Setup
You now need to install an extra library for the LCD display:
1. Open the Arduino IDE.
2. Go to **Sketch** -> **Include Library** -> **Manage Libraries...**
3. Search for **"MFRC522"** (by GithubCommunity) and ensure it's installed.
4. Search for **"LiquidCrystal I2C"** (by Frank de Brabander) and install it.

## How it works with Eco-Matic
1. The Arduino constantly polls for an RFID card and displays "Eco-Matic Ready" / "Please Tap Card" on the LCD.
2. Once tapped, the LCD says "Checking...", and the RFID UID is sent to the PC via USB as `RFID:A1B2C3D4`.
3. The C# WPF Application checks the database.
4. **If the account exists:** The C# app sends the string `VALID` back to the Arduino. The Arduino turns on the Green LED and prints "Access Granted!".
5. **If the account doesn't exist:** The C# app sends the string `INVALID` back to the Arduino. The Arduino turns on the Red LED and prints "Unknown Card".
6. After 3 seconds, all lights turn off, the message clears, and it's ready for the next customer.
