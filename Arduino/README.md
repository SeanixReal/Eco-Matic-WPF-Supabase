# Eco-Matic RFID Scanner

This folder contains the Arduino source code and documentation for the Eco-Matic Trash-to-Credit system's RFID module.

## Hardware Requirements
*   Arduino Uno (or Nano)
*   RFID-RC522 Module
*   Jumper wires
*   RFID Cards or Key Fobs (13.56 MHz)

## Wiring Guide (Arduino Uno to RC522)
**WARNING:** The RC522 operates on 3.3V. Do NOT connect it to the 5V pin or you may permanently damage the module.

| RC522 Pin | Arduino Uno Pin | Description |
| :--- | :--- | :--- |
| **SDA (SS)** | Pin 10 | Slave Select |
| **SCK** | Pin 13 | Serial Clock |
| **MOSI** | Pin 11 | Master Out Slave In |
| **MISO** | Pin 12 | Master In Slave Out |
| **IRQ** | Unconnected | Interrupt (Not needed) |
| **GND** | GND | Ground |
| **RST** | Pin 9 | Reset |
| **3.3V** | 3.3V | Power Supply |

## Arduino IDE Setup
Before uploading the code, you need to install the library to interact with the module:
1. Open the Arduino IDE.
2. Go to **Sketch** -> **Include Library** -> **Manage Libraries...**
3. Search for **"MFRC522"** (by GithubCommunity) and click **Install**.

## How it works with Eco-Matic
1. The Arduino constantly polls for an RFID card.
2. Once a card is tapped, it reads the card's UID.
3. The UID is sent over the USB serial connection in the format: `RFID:A1B2C3D4`.
4. The Eco-Matic WPF application uses `System.IO.Ports.SerialPort` to listen for data on the COM port.
5. When the WPF app sees `RFID:xxxx`, it will cross-reference the database to load the user's credit profile.