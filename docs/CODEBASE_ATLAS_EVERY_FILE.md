# Eco-Matic Codebase Atlas (Every Meaningful File)

This atlas maps the main project files used by the Eco-Matic WPF, Supabase, and Arduino implementation. Generated folders such as `bin`, `obj`, `.git`, `.vs`, and `artifacts` are intentionally excluded.

It is meant as a practical reference for maintenance, walkthroughs, and final project review.

## 1) System Mental Model in 30 Seconds

- Entry: MainWindow
- User flows: CustomerWindow and AdminWindow
- Session state: DataStore
- Supabase session routing: SupabaseSessionCoordinator
- Cloud API: SupabaseStore and SupabaseClient
- Hardware: ArduinoService
- Payments: QrPaymentService + Supabase Edge Function
- Printing: ReceiptPrinterService + EscPosReceiptFormatter

## 2) Root-Level Files

### Configuration and Project Metadata

- .env - local runtime settings (Supabase keys, printer settings, Arduino port)
- .env.example - template for required environment variables
- .gitattributes - git file behavior rules
- .gitignore - ignored files/folders
- Eco-Matic.csproj - main .NET WPF project file
- Eco-Matic.csproj.user - machine-local project user settings
- Eco-Matic.slnx - solution/workspace entry
- LICENSE.txt - project license
- README.md - top-level project overview
- AssemblyInfo.cs - assembly metadata
- App.xaml - WPF application resources/startup declaration
- App.xaml.cs - app startup bootstrapping code

## 3) UI Window Files (Presentation and Flow Layer)

Each screen is split into .xaml (layout) and .xaml.cs (logic).

- AboutWindow.xaml - about dialog layout
- AboutWindow.xaml.cs - about dialog behavior
- AddMachineWindow.xaml - add machine form layout
- AddMachineWindow.xaml.cs - machine creation logic and data binding
- AdminWindow.xaml - admin shell layout and sections
- AdminWindow.xaml.cs - role-based routing, dashboard, inventory/catalog/users/sales/logs actions
- CatalogItemWindow.xaml - global item editor layout
- CatalogItemWindow.xaml.cs - catalog item create/edit behavior
- CustomerDashboardWindow.xaml - RFID customer dashboard layout
- CustomerDashboardWindow.xaml.cs - customer info and point-save behavior
- CustomerRegistrationWindow.xaml - RFID registration form layout
- CustomerRegistrationWindow.xaml.cs - customer registration write flow
- CustomerWindow.xaml - customer vending UI layout
- CustomerWindow.xaml.cs - purchase flow, recycle flow, QR add-funds flow, receipt finalization, dispense feedback
- EditMachineWindow.xaml - edit machine form layout
- EditMachineWindow.xaml.cs - machine update behavior
- EventLogWindow.xaml - event log dialog layout
- EventLogWindow.xaml.cs - event log display behavior
- InventoryItemWindow.xaml - machine-slot inventory item editor layout
- InventoryItemWindow.xaml.cs - slot assignment and inventory edits
- ItemDetailsWindow.xaml - item details modal layout
- ItemDetailsWindow.xaml.cs - item details presentation logic
- LoginWindow.xaml - login form layout
- LoginWindow.xaml.cs - credential input and result handling
- MachineSelectionWindow.xaml - customer machine selector layout
- MachineSelectionWindow.xaml.cs - machine list loading and chosen machine output
- MainWindow.xaml - landing screen layout
- MainWindow.xaml.cs - mode routing, Arduino scan handling, login/customer launch
- MapPickerWindow.xaml - map picker layout
- MapPickerWindow.xaml.cs - coordinate selection and reverse-geocoding integration
- PointAmountWindow.xaml - point amount dialog layout
- PointAmountWindow.xaml.cs - point quantity input behavior
- QrPaymentWindow.xaml - QR payment dialog layout
- QrPaymentWindow.xaml.cs - payment intent creation/status polling behavior
- ReadmeWindow.xaml - in-app readme dialog layout
- ReadmeWindow.xaml.cs - readme viewing behavior
- ReceiptWindow.xaml - receipt display layout
- ReceiptWindow.xaml.cs - receipt presentation and print status feedback
- RecyclableItemWindow.xaml - recyclable-item editor layout
- RecyclableItemWindow.xaml.cs - recyclable definition CRUD behavior
- RestockWindow.xaml - restock quantity dialog layout
- RestockWindow.xaml.cs - restock input behavior
- UserEditorWindow.xaml - user editor layout
- UserEditorWindow.xaml.cs - user add/edit and machine-assignment UI logic

## 4) Data Layer Files (Core Logic)

- Data/AppEnvironment.cs - loads and validates .env settings and required keys
- Data/ArduinoService.cs - serial port lifecycle, RFID parsing, command sending to Arduino LCD/state
- Data/CsvStorage.cs - legacy/simple CSV helper support
- Data/DataStore.cs - static customer-session state (products, transaction state, pending points)
- Data/Esp32SupabaseClient.ino - microcontroller-side Supabase HTTP prototype file
- Data/MapLocationService.cs - reverse geocoding service (OpenStreetMap Nominatim)
- Data/SupabaseSessionCoordinator.cs - Supabase availability checks and customer-session persistence routing
- Data/QrPaymentService.cs - calls Supabase Edge Function for QR intent/status/pay actions
- Data/ReceiptPrinterService.cs - receipt print routing (Windows queue / serial), printer setting resolution
- Data/SupabaseClient.cs - low-level HTTP wrapper for PostgREST/RPC/functions
- Data/SupabaseStore.cs - high-level application data service (auth, machines, inventory, sales, logs, receipts)
- Data/SupabaseStore_Customers.cs - customer RFID/account operations as partial class extension

## 5) Domain Model Files

- Models/VendingItem.cs - abstract vending item base
- Models/Product.cs - concrete product model and product-type structure
- Models/Transaction.cs - session transaction, line items, recycle entries, receipt-linked structures
- Models/RecyclableItemDefinition.cs - recyclable material definition model

## 6) Utilities Files

- Utilities/AudioService.cs - speech and sound effect control
- Utilities/EscPosReceiptFormatter.cs - ESC/POS byte formatting for receipts
- Utilities/ImageLoader.cs - resilient product image loading
- Utilities/ImagePathConverter.cs - image path conversion helper for WPF binding
- Utilities/SlotIdHelper.cs - slot normalization and validation helper

## 7) Supabase Runtime Extension

- supabase/functions/qr-payment-confirm/index.ts - Edge Function for QR payment intent, status, and demo pay actions

## 8) Arduino Folder

- Arduino/README.md - Arduino setup and usage notes
- Arduino/RFID_Scanner/RFID_Scanner.ino - RFID scanner firmware sketch

## 9) Documentation Files

### Main docs

- docs/README.md - docs index
- docs/FINAL_PROJECT_DOCUMENTATION.md - formal final project documentation
- docs/Eco-Matic-Final-Project-Documentation.docx - Word version of the final project documentation
- docs/CODEBASE_ARCHITECTURE.md - architecture-level breakdown
- docs/CODE_REVIEW.md - findings, limitations, and review notes
- docs/DIAGRAMS.md - diagram index and order guidance
- docs/MAINTAINER_GUIDE.md - maintenance workflow notes
- docs/PROFESSOR_ARCHITECTURE_GUIDE.md - architecture explanation guide
- docs/PROFESSOR_CLASS_DATABASE_QA.md - class/database Q and A guide
- docs/SUPABASE_AUDIT.md - Supabase audit snapshots and findings
- docs/Supabase_Migration.md - migration background
- docs/USER_MANUAL.md - end-user operation guide

### Presentation and project review docs

- docs/FINAL_PROJECT_PRESENTATION_DOCUMENTATION.md - full presentation planning guide
- docs/FINAL_PROJECT_DOCUMENTATION.md - formal project documentation in report format
- docs/FINAL_PROJECT_POWERPOINT_CONTENTS.md - slide content copy
- docs/FINAL_PROJECT_PRESENTATION_SCRIPT.md - client-pitch script with Q and A
- docs/PITCH_TIMED_SCRIPT_10_MIN_STRICT.md - strict timed script
- docs/PITCH_ONE_PAGE_CUE_CARD.md - one-page memory card
- docs/ADVANCED_QA_COMPLEX_FUNCTIONS.md - advanced technical Q and A bank
- docs/CODEBASE_ATLAS_EVERY_FILE.md - this atlas file

### Diagram docs

- docs/diagrams/ERD.md - entity relationship diagram
- docs/diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md - short class diagram
- docs/diagrams/FULL_CLASS_DIAGRAM.md - detailed class diagram
- docs/diagrams/PROGRAM_FLOWCHART.md - full runtime flow
- docs/diagrams/CUSTOMER_BUYING_FLOW.md - customer purchase flow details
- docs/diagrams/DATABASE_CONNECTION_FLOW.md - sequence diagram for DB request path

### SQL docs and migrations

- docs/sql/README.md - SQL folder guide
- docs/sql/archive/mysql/database_setup.sql - historical MySQL setup script
- docs/sql/archive/mysql/migration_increment2.sql - historical MySQL migration script
- docs/sql/migrations/supabase/migration_increment3.sql - Supabase schema migration
- docs/sql/migrations/supabase/migration_increment4.sql - Supabase schema migration
- docs/sql/migrations/supabase/migration_increment5.sql - Supabase schema migration
- docs/sql/migrations/supabase/migration_increment6.sql - Supabase schema migration
- docs/sql/migrations/supabase/migration_increment7.sql - Supabase schema migration
- docs/sql/migrations/supabase/migration_increment8_qr_payments.sql - QR payments schema migration
- docs/sql/migrations/supabase/migration_increment9_user_machine_assignments.sql - user-machine scope migration
- docs/sql/migrations/supabase/migration_increment10_catalog_soft_delete.sql - catalog soft-delete migration
- docs/sql/seeds/seed_inventory.sql - seed data script

### Archive

- docs/archive/OOP2_Project-Proposal.pdf - archived proposal document

## 10) Asset Files

### Audio assets

- Assets/Audio/coin_dispense.mp3 - coin/dispense effect
- Assets/Audio/coins.mp3 - coin insert effect
- Assets/Audio/lobby.mp3 - background/lobby audio
- Assets/Audio/success.mp3 - success/confirmation effect

### GIF demo assets

- Assets/Gifs/EcoMatic-Admin.gif - admin management demo recording
- Assets/Gifs/EcoMatic-Customer.gif - customer vending demo recording
- Assets/Gifs/EcoMatic-Inventory.gif - inventory management demo recording

### Image assets

- Assets/Images/BandaidBox.png - product image
- Assets/Images/CheeseRing.png - product image
- Assets/Images/Chippy.png - product image
- Assets/Images/CocaCola.png - product image
- Assets/Images/DelMontePineappleJuice.png - product image
- Assets/Images/EcoBag.png - product image
- Assets/Images/KitKat.png - product image
- Assets/Images/MrChips.png - product image
- Assets/Images/Nova.png - product image
- Assets/Images/Pepsi.png - product image
- Assets/Images/Piattos.png - product image
- Assets/Images/placeholder.png - fallback image
- Assets/Images/RCCola.png - product image
- Assets/Images/RollerCoaster.png - product image
- Assets/Images/Sting.png - product image
- Assets/Images/ZestOOrange.png - product image

## 11) Suggested Review Path

### Pass 1 - Runtime flow

Read in this order:

1. MainWindow.xaml.cs
2. CustomerWindow.xaml.cs
3. AdminWindow.xaml.cs
4. Data/DataStore.cs
5. Data/SupabaseSessionCoordinator.cs

### Pass 2 - Data and hardware

Read in this order:

1. Data/SupabaseClient.cs
2. Data/SupabaseStore.cs
3. Data/SupabaseStore_Customers.cs
4. Data/ArduinoService.cs
5. Data/ReceiptPrinterService.cs

### Pass 3 - Diagrams and explanation notes

Read in this order:

1. docs/diagrams/ERD.md
2. docs/diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md
3. docs/PROFESSOR_ARCHITECTURE_GUIDE.md
4. docs/ADVANCED_QA_COMPLEX_FUNCTIONS.md

## 12) Important Runtime Functions

- MainWindow Arduino_OnCardScanned
- SupabaseSessionCoordinator PrepareCustomerModeAsync
- SupabaseSessionCoordinator CanUseSupabaseFeature
- DataStore SaveCompletedReceipt
- CustomerWindow SelectButton_Click
- CustomerWindow PurchaseWithPointsAsync
- SupabaseStore AuthenticateUserAccess
- SupabaseStore GetMachineInventory
- SupabaseStore AddItemToMachineSlot
- SupabaseStore UpdateMachineInventoryAssignment
- SupabaseStore InsertQueuedReceiptSession
- ArduinoService SerialPort_DataReceived

These methods cover the main customer, admin, database, receipt, and hardware behaviors in the project.
