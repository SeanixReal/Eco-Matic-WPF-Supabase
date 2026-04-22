# Eco-Matic Documentation Set

This is the single canonical documentation folder for the current Eco-Matic codebase.

## Recommended Reading Order

1. `CODEBASE_ARCHITECTURE.md`
2. `CODE_REVIEW.md`
3. `SUPABASE_AUDIT.md`
4. `DIAGRAMS.md`
5. `MAINTAINER_GUIDE.md`
6. `PROFESSOR_ARCHITECTURE_GUIDE.md`
7. `USER_MANUAL.md`

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
- `migration_increment4.sql`

Important current note:

- `migration_increment2.sql` is a historical MySQL-era increment script, not the live Supabase migration path

## Current Stack

Use this folder as the source of truth for the current implementation. The running application uses:

- WPF for the desktop interface
- `SupabaseStore` and `SupabaseClient` for data access
- `ArduinoService` for RFID and LCD communication
