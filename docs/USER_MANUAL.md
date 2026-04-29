# Eco-Matic Human Guide

This guide is for a human operator, classmate, or reviewer who needs to understand how to use the current system without reading the code first.

## 1. What the App Has

Eco-Matic currently has three major user-facing flows:

- customer vending
- admin management
- RFID customer registration and recycle-credit saving

The admin side is now split into:

- a shared `Items` catalog
- a per-machine `Inventory` setup

## 2. Customer Mode

The customer flow is centered on the vending machine UI.

### What the customer can do

- choose an active machine
- insert money
- enter a QR payment amount in the app, scan the QR code, and wait for automatic confirmation
- examine items
- buy available products
- add recycle points during the session
- receive a receipt and change

### Important current behavior

- purchases can use cash buttons or the QR payment modal
- recycle points are tracked separately from inserted cash
- RFID is used for customer identity, saving recycle points, showing transaction history, and spending saved eco-points when the customer chooses point payment
- QR payment uses a Supabase Edge Function to mark the scanned payment intent as paid; the phone shows a simple confirmation message after scanning

## 3. RFID Customer Flow

When an RFID card is scanned:

- if the card is already registered, the customer dashboard opens
- if the card is unknown, the registration window opens
- if there are pending recycle points, they can be saved to the customer account
- the customer dashboard shows the current eco-credit balance and recent transaction history with item, quantity, and cash/point payment amount
- if a card is scanned after purchases in the current vending session, those purchases are attached to the first RFID used for that session
- after purchases are attached, scanning a different RFID opens that account for viewing but does not move the current session's transaction history to the new card

## 4. Admin Mode

The admin side is controlled through `AdminWindow`.

### Admin capabilities

- admin login opens on the dashboard by default
- view dashboard metrics
- manage the global item catalog, with a warning when a new or edited item name duplicates an existing catalog item
- manage machine inventory
- monitor low-stock and out-of-stock slots
- restock items by quantity
- restock the selected inventory item directly to max capacity
- view logs and sales
- review sales reports with the Week filter selected by default, plus revenue trends, product mix, best-selling items, machine revenue, category revenue, peak sales periods, transaction counts, units sold, and average sale
- review event logs with the Week filter selected by default and machine names for machine-scoped activity such as purchases, stock updates, slot assignments, and restocking
- manage vending machines; newly registered machines can start empty and be filled later from the Inventory view
- manage users through a staff editor with a scrollable machine-assignment list and always-visible Register/Cancel actions
- manage RFID customers

### Role behavior

- `Admin`: full access
- `Inventory Manager`: inventory-only access for the vending machines assigned by an admin

## 5. Inventory Rules You Should Follow

The frontend customer vending screen only shows 12 product slots.

That means:

- keep each machine at 12 visible items or fewer
- keep slot naming simple and consistent
- avoid treating slot IDs as decorative labels only

Recommended slot values:

- `1`
- `2`
- ...
- `12`

The app treats those as the canonical machine slot IDs.

Current technical note:

- the code now validates slot IDs as `1` through `12`
- the code now rejects duplicate slots on the same machine
- the code now warns before saving duplicate global item names
- the code now blocks adding more than 12 inventory entries to one machine

## 6. Dashboard and Reports

### Dashboard cards

- total sales
- items sold
- low stock alerts
- the vending machine where each low-stock alert belongs
- active machines

### Low stock logic

The current code marks low-stock alerts when stock is `3` or below.

## 7. Known Practical Limitations

- customer purchases can deduct saved RFID credits only when the customer has linked a card and toggles point payment
- the app is designed around a fixed 12-slot customer UI
- images are intentionally local-first rather than cloud-dependent for reliable classroom demos
- customer mode can now load from a locally cached MySQL snapshot and replay queued sales/logs later after one successful online sync
- admin mode and RFID account updates still require internet access
- if your live Supabase schema is older, run `docs/sql/migrations/supabase/migration_increment3.sql` before expecting per-machine price overrides to work
- run `docs/sql/migrations/supabase/migration_increment4.sql` before expecting offline replay deduplication to work safely
- see `docs/SUPABASE_AUDIT.md` for the latest live schema and security findings

## 8. Where To Read More

- architecture: `docs/CODEBASE_ARCHITECTURE.md`
- diagrams: `docs/DIAGRAMS.md`
- maintainer notes: `docs/MAINTAINER_GUIDE.md`
- professor guide: `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
