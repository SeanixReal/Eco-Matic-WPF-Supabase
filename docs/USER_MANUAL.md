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
- examine items
- buy available products
- add recycle points during the session
- receive a receipt and change

### Important current behavior

- purchases are still cash-based
- recycle points are tracked separately from inserted cash
- RFID is used for saving recycle points to a customer account, not for paying for products

## 3. RFID Customer Flow

When an RFID card is scanned:

- if the card is already registered, the customer dashboard opens
- if the card is unknown, the registration window opens
- if there are pending recycle points, they can be saved to the customer account

## 4. Admin Mode

The admin side is controlled through `AdminWindow`.

### Admin capabilities

- view dashboard metrics
- manage the global item catalog
- manage machine inventory
- restock items
- view logs and sales
- manage vending machines
- manage users
- manage RFID customers

### Role behavior

- `Admin`: full access
- `Inventory Manager`: inventory-only style access with restricted views

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
- the code now blocks adding more than 12 inventory entries to one machine

## 6. Dashboard and Reports

### Dashboard cards

- total sales
- items sold
- low stock alerts
- active machines

### Low stock logic

The current code marks low-stock alerts when stock is `3` or below.

## 7. Known Practical Limitations

- customer purchases do not yet deduct from RFID credits
- the app is designed around a fixed 12-slot customer UI
- images are intentionally local-first rather than cloud-dependent for reliable classroom demos
- the system is not yet true offline-first; it does not persist a local database snapshot and sync queued changes later
- if your live Supabase schema is older, run `docs/migration_increment3.sql` before expecting per-machine price overrides to work

## 8. Where To Read More

- architecture: `docs/CODEBASE_ARCHITECTURE.md`
- diagrams: `docs/DIAGRAMS.md`
- maintainer notes: `docs/MAINTAINER_GUIDE.md`
- professor guide: `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
