# Eco-Matic Documentation Set

This is the single canonical documentation folder for the current Eco-Matic codebase.

## Recommended Reading Order

1. `CODEBASE_ARCHITECTURE.md`
2. `CODE_REVIEW.md`
3. `SUPABASE_AUDIT.md`
4. `sql/README.md`
5. `DIAGRAMS.md`
6. `MAINTAINER_GUIDE.md`
7. `PROFESSOR_ARCHITECTURE_GUIDE.md`
8. `USER_MANUAL.md`

## Main Contents

- architecture and presentation docs
- review and maintenance docs
- database setup, migration, and seed SQL under `sql/`
- user manual
- archived proposal material in `archive/`

Relevant SQL docs for the current inventory model:

- `sql/README.md`
- `sql/seeds/seed_inventory.sql`
- `sql/migrations/supabase/migration_increment3.sql`
- `sql/migrations/supabase/migration_increment4.sql`

Important current note:

- `sql/archive/mysql/migration_increment2.sql` is a historical MySQL-era increment script, not the live Supabase migration path

## Current Stack

Use this folder as the source of truth for the current implementation. The running application uses:

- WPF for the desktop interface
- `SupabaseStore` and `SupabaseClient` for data access
- `ArduinoService` for RFID and LCD communication
