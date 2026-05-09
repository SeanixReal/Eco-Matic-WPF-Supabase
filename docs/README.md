# Eco-Matic Documentation Index

This folder contains the main documentation for the current Eco-Matic WPF, Supabase, and Arduino implementation.

## Suggested Reading Order

1. `CODEBASE_ARCHITECTURE.md`
2. `FINAL_PROJECT_DOCUMENTATION.md`
3. `CODE_REVIEW.md`
4. `SUPABASE_AUDIT.md`
5. `sql/README.md`
6. `DIAGRAMS.md`
7. `MAINTAINER_GUIDE.md`
8. `DEMO_CRUD_READINESS.md`
9. `PROFESSOR_ARCHITECTURE_GUIDE.md`
10. `PROFESSOR_CLASS_DATABASE_QA.md`
11. `USER_MANUAL.md`

## Main Contents

- Architecture overview and runtime flow notes
- Formal final project documentation
- Mermaid diagrams under `diagrams/`
- Class/database explanation and Q&A notes
- Code review and maintenance notes
- Demo CRUD readiness checklist
- Database setup, migration, and seed SQL under `sql/`
- User manual
- Archived proposal material in `archive/`

## Presentation Materials

- `FINAL_PROJECT_DOCUMENTATION.md`
- `PRESENTATION_READY_MASTER_INDEX.md`
- `FINAL_PROJECT_PRESENTATION_DOCUMENTATION.md`
- `FINAL_PROJECT_POWERPOINT_CONTENTS.md`
- `FINAL_PROJECT_PRESENTATION_SCRIPT.md`
- `PITCH_TIMED_SCRIPT_10_MIN_STRICT.md`
- `PITCH_ONE_PAGE_CUE_CARD.md`
- `ADVANCED_QA_COMPLEX_FUNCTIONS.md`
- `CODEBASE_ATLAS_EVERY_FILE.md`

## SQL References

- `sql/README.md`
- `sql/seeds/seed_inventory.sql`
- `sql/migrations/supabase/migration_increment3.sql`
- `sql/migrations/supabase/migration_increment4.sql`
- `sql/migrations/supabase/migration_increment5.sql`
- `sql/migrations/supabase/migration_increment6.sql`
- `sql/migrations/supabase/migration_increment7.sql`
- `sql/migrations/supabase/migration_increment8_qr_payments.sql`
- `sql/migrations/supabase/migration_increment9_user_machine_assignments.sql`
- `sql/migrations/supabase/migration_increment10_catalog_soft_delete.sql`

Historical MySQL scripts remain under `sql/archive/mysql/` for reference only. The running application uses Supabase.

## Current Stack

- WPF desktop interface
- Supabase REST access through `SupabaseStore` and `SupabaseClient`
- Arduino RFID/LCD communication through `ArduinoService`
- Local-first image loading for product assets

## Installed NuGet Packages

- `Microsoft.Web.WebView2` - embedded map picker support
- `QRCoder` - QR codes for payment/registration flows
- `System.IO.Ports` - Arduino serial communication
- `System.Speech` - text-to-speech support

## Diagram Files

- `DIAGRAMS.md`
- `diagrams/PROGRAM_FLOWCHART.md`
- `diagrams/ERD.md`
- `diagrams/FULL_CLASS_DIAGRAM.md`
- `diagrams/SIMPLIFIED_CLASS_DIAGRAM.md`
- `diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md`
- `diagrams/CUSTOMER_BUYING_FLOW.md`
- `diagrams/DATABASE_CONNECTION_FLOW.md`
