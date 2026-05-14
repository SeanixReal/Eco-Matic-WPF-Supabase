# Simplified Grouped Class Diagram

This version keeps only the core classes needed to understand the system at a high level.

```mermaid
classDiagram
    namespace UI {
        class MainWindow {
            +BtnCustomer_Click()
            +BtnAdmin_Click()
            +Arduino_OnCardScanned()
            +RefreshConnectivityBadgeAsync()
        }

        class AdminWindow {
            +LoadInventoryGrid()
            +LoadCatalogItems()
            +LoadSalesData()
        }

        class CustomerWindow {
            +SelectButton_Click()
            +BtnRecycle_Click()
            +BtnPayWithPoints_Click()
        }

        class CatalogItemWindow {
            +BtnSave_Click()
        }

        class InventoryItemWindow {
            +BtnSave_Click()
        }

        class RestockWindow {
            +BtnRestock_Click()
        }
    }

    namespace Models {
        class VendingItem {
            <<abstract>>
            +Id
            +Name
            +Price
            +Stock
        }

        class Product {
            +Type
            +Create()
        }

        class SnackItem {
            +Calories
        }

        class DrinkItem {
            +VolumeMl
        }

        class MiscItem

        class RecyclableItemDefinition {
            +DisplayName
            +PointsPerUnit
        }

        class Transaction {
            +ReceiptNumber
            +TotalAmount
            +EcoPointsSpent
        }
    }

    namespace Services {
        class ArduinoService {
            +OnCardScanned
            +SendCustomerSessionActive()
            +SendCustomerSessionAfk()
            +SendResponse()
            +SendMessage()
        }

        class QrPaymentService {
            +CreateIntentAsync()
            +GetStatusAsync()
        }

        class ReceiptPrinterService {
            +TryPrintReceipt()
        }
    }

    namespace Infrastructure {
        class DataStore {
            <<static>>
            +Products
            +PendingPoints
        }

        class SupabaseSessionCoordinator {
            +PrepareCustomerModeAsync()
            +GetMachineInventory()
        }

        class SupabaseStore {
            +GetMachineInventory()
            +AddCatalogItem()
            +DeleteCatalogItem()
            +UpdateCustomerCredits()
            +RecordSale()
        }

        class SupabaseClient {
            +GetAsync()
            +PostAsync()
            +PatchAsync()
        }
    }

    VendingItem <|-- Product
    Product <|-- SnackItem
    Product <|-- DrinkItem
    Product <|-- MiscItem

    MainWindow --> AdminWindow
    MainWindow --> CustomerWindow
    MainWindow --> ArduinoService

    AdminWindow --> SupabaseStore
    AdminWindow ..> CatalogItemWindow
    AdminWindow ..> InventoryItemWindow
    AdminWindow ..> RestockWindow

    CustomerWindow --> DataStore
    CustomerWindow --> ArduinoService
    CustomerWindow --> QrPaymentService
    CustomerWindow --> ReceiptPrinterService
    CustomerWindow --> Transaction
    CustomerWindow --> RecyclableItemDefinition

    DataStore --> SupabaseSessionCoordinator
    SupabaseSessionCoordinator --> SupabaseStore
    SupabaseStore --> SupabaseClient
    SupabaseStore ..> Product
```

## How to Explain It

- `UI` contains the main WPF screens the user sees.
- `Models` are the product, recycle, and receipt objects used at runtime.
- `Services` isolate hardware, QR payment, and receipt printing.
- `Infrastructure` is the Supabase-backed data path.
- `ArduinoService` owns the customer `STATE:ACTIVE` and main-screen `STATE:AFK` hardware commands shown in the README hardware GIFs.
- The app now uses Supabase directly; the old local database fallback path has been removed.
- Catalog delete lives in `SupabaseStore`: clear vending slots first, then soft-delete the catalog row for report-safe history.
