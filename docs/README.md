# Eco-Matic Documentation Set

This is the single canonical documentation folder for the current Eco-Matic codebase.

## Recommended Reading Order

1. `CODEBASE_ARCHITECTURE.md`
2. `CODE_REVIEW.md`
3. `DIAGRAMS.md`
4. `MAINTAINER_GUIDE.md`
5. `PROFESSOR_ARCHITECTURE_GUIDE.md`
6. `USER_MANUAL.md`

## Main Contents

- architecture and presentation docs
- review and maintenance docs
- database setup and migration SQL
- user manual
- archived proposal material in `archive/`

Relevant SQL docs for the current inventory model:

- `database_setup.sql`
- `seed_inventory.sql`
- `migration_increment3.sql`

## Current Stack

Use this folder as the source of truth for the current implementation. The running application uses:

- WPF for the desktop interface
- `SupabaseStore` and `SupabaseClient` for data access
- `ArduinoService` for RFID and LCD communication
