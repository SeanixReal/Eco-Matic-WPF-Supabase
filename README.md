# Eco-Matic Vending & Recycling System

## Overview
Eco-Matic is a complete C# WPF point-of-sale and "Trash-to-Credit" loyalty system integrated with a physical Arduino-based RFID scanner. The project allows users to purchase items, while simultaneously dropping off recyclables (bottles, cans) to earn Eco-Credits.

## Features
- **Vending & Inventory Management**: Full WPF graphical interface for managing machines, stock, and purchasing catalog items.
- **Hardware Integration (Arduino)**: 
  - Uses an Arduino Uno, MFRC522 RFID reader, and 16x2 I2C LCD display.
  - Bidirectional USB Serial communication on `COM5` to handle physical hardware states (Active vs. AFK mode) and validation feedback.
- **Eco-Credits Loyalty Program**: 
  - Scan physical RFID cards to register/login.
  - E-Wallet dashboard for tracking accumulated points.
  - Pay for items using Eco-Credits.
- **Admin CRM**: 
  - Role-Based Access Control (RBAC).
  - Customer relation management (CRM) backend backed by MySQL to modify or view registered users and point balances.
- **Event Logging**: Time-based filtering (Day, Week, Month) of all machine, sales, and user events for auditing.

## Setup
- **Database**: Run the SQL scripts in `docs/database_setup.sql`. The system requires a running MySQL instance.
- **Hardware**: Wire the Arduino and MFRC522 per `Arduino/README.md` instructions and flash `RFID_Scanner.ino`.
- **Application**: Open `Eco-Matic.sln` in Visual Studio and build the project, or run `dotnet run` in the root folder.
