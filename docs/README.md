# Eco-Matic Documentation Index

This folder contains the professor-facing documentation for the current Eco-Matic WPF, Supabase, and Arduino implementation.

## Suggested Reading Order

1. `FINAL_PROJECT_DOCUMENTATION.md`
2. `Eco-Matic-Final-Project-Documentation-Formatted.docx`
3. `Eco-Matic-Final-Project-Documentation.pdf`
4. `CODEBASE_ARCHITECTURE.md`
5. `DIAGRAMS.md`
6. `PROFESSOR_ARCHITECTURE_GUIDE.md`
7. `PROFESSOR_CLASS_DATABASE_QA.md`
8. `USER_MANUAL.md`
9. `Supabase_Migration.md`
10. `sql/README.md`

## Main Contents

- Architecture overview and runtime flow notes
- Formal final project documentation
- Formatted Word and PDF versions of the final project documentation
- Mermaid diagrams under `diagrams/`
- Class/database explanation and Q&A notes
- Database setup, migration, and seed SQL under `sql/`
- User manual

## Submission Documents

- `FINAL_PROJECT_DOCUMENTATION.md`
- `Eco-Matic-Final-Project-Documentation-Formatted.docx`
- `Eco-Matic-Final-Project-Documentation.pdf`
- `CODEBASE_ARCHITECTURE.md`
- `DIAGRAMS.md`
- `PROFESSOR_ARCHITECTURE_GUIDE.md`
- `PROFESSOR_CLASS_DATABASE_QA.md`
- `USER_MANUAL.md`
- `Supabase_Migration.md`

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

Only the Supabase migration path is included in the submission. The running application uses Supabase.

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
