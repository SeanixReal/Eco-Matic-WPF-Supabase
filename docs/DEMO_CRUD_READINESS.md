# Demo CRUD Readiness

Use this checklist before the demo. The current build is Supabase-only, so all database CRUD needs live internet.

## Connectivity

- Main menu now shows a lower-left Supabase status badge.
- If it turns green, it disappears after a few seconds and the app is ready.
- If it stays red, do not start CRUD demos yet; use a hotspot or stronger network.

## Machine CRUD

- Create machine: guarded by the current 4-machine project limit.
- Edit machine: updates name, address, status, and optional coordinates.
- Delete machine: fixed for demo. It now clears staff assignments, machine inventory, sales rows, receipt sessions, QR intents, and event logs before deleting the machine.
- Demo tip: use a disposable test machine for delete demos because the delete is intentionally destructive.

## Catalog Item CRUD

- Add/edit item: duplicate global item names are blocked before saving.
- Delete item: clears any machine slot assignments first, so vending machines show those slots as empty after refresh.
- Delete item now soft-deletes the `items` row by setting `is_active = false`, `deleted_at`, and `deleted_reason`.
- Sales reports keep working because old `sales_transactions.item_id` values can still join to the inactive `items` row for historical product labels.
- Demo tip: after running `migration_increment10_catalog_soft_delete.sql`, you can delete a catalog item that is assigned or already sold; it disappears from active catalog/inventory use while old sales remain reportable.

## Inventory CRUD

- Add slot item: validates slot `1` through `12`, blocks duplicate slots, and enforces max 12 visible machine slots.
- Edit slot item: validates stock, max capacity, slot number, and duplicate slots.
- Delete inventory item: removes only the machine slot assignment; the global catalog item remains.
- Restock: blocks quantities above max capacity; selected-item restock-to-max is available.

## User and Customer CRUD

- Staff add/edit: inventory managers can be assigned to one or more machines.
- Staff delete: master admin deletion is blocked in the UI.
- Customer credit edit: writes the exact new eco-credit balance.
- Customer delete: removes the RFID customer account.

## Remaining Demo Cautions

- No internet means no real customer/admin data features.
- Very weak internet can make Supabase calls slow; test the badge and one small read action before presenting.
- Passwords are still stored and compared directly, so do not describe them as securely hashed.
