# Advanced Q and A Bank (Complex Functions and Architecture)

Use this for the 5-minute Q and A and defense practice.
Each answer has:
- short answer (what to say live)
- deep follow-up (if professor asks deeper)
- code anchor (where it happens)

## 1) How exactly does the app communicate with Arduino?

Short answer:

The app uses USB serial through ArduinoService. Arduino sends RFID lines in RFID:<UID> format, the app validates the UID in Supabase, then responds with VALID or INVALID and LCD/state messages.

Deep follow-up:

ArduinoService opens a SerialPort and listens to DataReceived. It parses line input, validates RFID-like hex UID format, and raises an OnCardScanned event. MainWindow handles that event asynchronously, checks online availability for RFID-only features, calls CustomerExists, then sends command responses and opens registration/dashboard windows.

Code anchor:

- Data/ArduinoService.cs
- MainWindow.xaml.cs
- Data/SupabaseStore_Customers.cs

## 2) Why is your RFID handling asynchronous?

Short answer:

To avoid blocking the WPF UI thread and to return hardware feedback quickly.

Deep follow-up:

MainWindow runs RFID checks in Task.Run and only marshals UI window operations back to Dispatcher. This keeps the app responsive and avoids delayed serial response when DB operations are slow.

Code anchor:

- MainWindow.xaml.cs (Arduino_OnCardScanned)

## 3) How does your database connection really work without raw PostgreSQL driver code?

Short answer:

The app uses Supabase PostgREST over HTTP, not a direct PostgreSQL socket.

Deep follow-up:

UI windows call SupabaseStore methods. SupabaseStore uses SupabaseClient methods (GetAsync/PostAsync/PatchAsync/DeleteAsync). SupabaseClient builds the /rest/v1 URL from environment config, sets apikey and Bearer headers, and sends HTTP requests to Supabase.

Code anchor:

- Data/SupabaseClient.cs
- Data/SupabaseStore.cs
- Data/AppEnvironment.cs

## 4) Why separate items and machine_inventory in your schema?

Short answer:

Because product definition and machine slot stock are different concerns.

Deep follow-up:

items is the global catalog. machine_inventory stores machine_id, slot_id, stock_level, max_capacity, and optional slot_price. This supports one item reused across many machines while preserving machine-specific stock and pricing.

Code anchor:

- docs/diagrams/ERD.md
- Data/SupabaseStore.cs (GetMachineInventory, AddItemToMachineSlot, UpdateMachineInventoryAssignment)

## 5) How do you prevent invalid inventory slot assignments?

Short answer:

The service layer validates slot range, duplicate slot use, and stock/capacity constraints.

Deep follow-up:

SupabaseStore normalizes slot IDs, enforces 1-12 slot rules, blocks duplicate slot assignment per machine, validates stock not negative, max capacity limits, and stock <= max capacity before writing to machine_inventory.

Code anchor:

- Data/SupabaseStore.cs (TryValidateSlotForMachine, TryValidateStockValues)

## 6) Explain your customer-mode offline behavior clearly.

Short answer:

Customer mode can run from local MySQL cache when Supabase is unreachable, then replay queued writes later.

Deep follow-up:

OfflineSyncCoordinator decides source at startup: Supabase, LocalMySql, or Unavailable. In local mode, inventory and machine lookups come from cached tables. Sales, event logs, and receipt sessions are queued in sync_queue and local receipt tables with client_sync_id for replay safety.

Code anchor:

- Data/OfflineSyncCoordinator.cs
- Data/OfflineMySqlStore.cs

## 7) Is your app fully offline?

Short answer:

No. Customer mode has offline-aware behavior, but admin mode and RFID account persistence are online-oriented.

Deep follow-up:

CanUseOnlineOnlyFeature gate is used before RFID account actions. If session source is LocalMySql or unavailable, RFID-specific workflows return an online-required message.

Code anchor:

- Data/OfflineSyncCoordinator.cs (CanUseOnlineOnlyFeature)
- MainWindow.xaml.cs

## 8) How does a purchase become persistent data?

Short answer:

Purchase updates inventory stock and queues or writes sale and event records, then stores receipt session details.

Deep follow-up:

In CustomerWindow item selection flow, stock is reduced in-memory, then DataStore.SaveInventory, DataStore.LogEvent, and DataStore.RecordSale run via background action. Receipt completion calls DataStore.SaveCompletedReceipt, which routes to cloud insert or local queue depending on source.

Code anchor:

- CustomerWindow.xaml.cs (SelectButton_Click, BtnBack_Click)
- Data/DataStore.cs
- Data/OfflineSyncCoordinator.cs

## 9) How does QR payment flow work?

Short answer:

The app creates and checks QR payment intents via the Supabase Edge Function.

Deep follow-up:

QrPaymentService calls the qr-payment-confirm function to create intent (reference, token, confirm URL), checks status with GET query params, and can mark paid in confirmation flow. CustomerWindow then adds paid amount to inserted funds.

Code anchor:

- Data/QrPaymentService.cs
- QrPaymentWindow.xaml.cs
- supabase/functions/qr-payment-confirm/index.ts

## 10) How are receipts generated and printed?

Short answer:

Receipt data is session-based and printing supports Windows queue and serial fallback.

Deep follow-up:

ReceiptPrinterService reads printer settings from environment, tries Windows raw queue mode first (or serial based on mode), and uses EscPosReceiptFormatter to build ESC/POS bytes. If one mode fails, auto mode can fallback.

Code anchor:

- Data/ReceiptPrinterService.cs
- Utilities/EscPosReceiptFormatter.cs
- ReceiptWindow.xaml.cs

## 11) How do role restrictions work in admin mode?

Short answer:

Role and assigned machine IDs are evaluated at login, then the UI and data filters enforce the scope.

Deep follow-up:

AuthenticateUserAccess returns role and assignedMachineIds. AdminWindow hides non-allowed navigation for Inventory Manager and forces view routing to Inventory. Data row filters limit visible machine data to assigned machine IDs.

Code anchor:

- Data/SupabaseStore.cs (AuthenticateUserAccess)
- AdminWindow.xaml.cs (SetupUIForRole, SetActiveViewAsync, BuildAssignedMachineRowFilter)

## 12) How do you handle map-based machine location?

Short answer:

MapPicker feeds coordinates, and MapLocationService reverse geocodes to address text.

Deep follow-up:

MapLocationService calls OpenStreetMap Nominatim reverse endpoint with coordinates and parses display_name into readable address. Add/Edit machine workflows allow saving coordinates plus editable address text.

Code anchor:

- Data/MapLocationService.cs
- MapPickerWindow.xaml.cs
- AddMachineWindow.xaml.cs
- EditMachineWindow.xaml.cs

## 13) What are your biggest technical risks right now?

Short answer:

Credential security hardening and policy hardening remain major next steps.

Deep follow-up:

Current implementation compares password fields directly even if some columns are named password_hash. Also, RLS tightening is constrained by current app dependence on anon-key table operations. These are acknowledged as roadmap priorities.

Code anchor:

- Data/SupabaseStore.cs
- Data/SupabaseStore_Customers.cs
- docs/CODE_REVIEW.md
- docs/SUPABASE_AUDIT.md

## 14) If a client asks why they should trust this as a pilot platform, what do you say?

Short answer:

Because features are integrated end-to-end and architecture is modular enough for phased rollout.

Deep follow-up:

The system already demonstrates complete flows across UI, data, and hardware layers, including machine-specific inventory and receipts. Services isolate complexity, which lowers change risk for iterative deployment.

Code anchor:

- MainWindow.xaml.cs
- CustomerWindow.xaml.cs
- AdminWindow.xaml.cs
- Data/SupabaseStore.cs
- Data/ArduinoService.cs

## 15) Defense line when you need a precise pause

"I will answer this in implementation order: layer, runtime flow, code anchor, and design reason."

Then answer using:

1. layer
2. flow
3. concrete method/class
4. tradeoff
