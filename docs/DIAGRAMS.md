# Eco-Matic Diagrams

This is the diagram index for the current WPF + Supabase codebase.

Database details were verified against the live Supabase project through the Supabase MCP tool on 2026-04-30. The live public schema currently exposes the Supabase-backed vending, inventory, sales, event log, customer, receipt, recyclable item, QR payment, and staff-assignment tables used by the app.

## Diagram Files

- [Entity Relationship Diagram](diagrams/ERD.md)
- [Full Class Diagram](diagrams/FULL_CLASS_DIAGRAM.md)
- [Short Foundational Class Diagram](diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md)
- [Entire Program Flowchart](diagrams/PROGRAM_FLOWCHART.md)
- [Customer Buying Process Flowchart](diagrams/CUSTOMER_BUYING_FLOW.md)
- [Database Connection Sequence Diagram](diagrams/DATABASE_CONNECTION_FLOW.md)

## Best Presentation Order

1. Start with the [Entire Program Flowchart](diagrams/PROGRAM_FLOWCHART.md) to explain how the app behaves from startup to exit.
2. Show the [Entity Relationship Diagram](diagrams/ERD.md) to explain persistent data.
3. Show the [Short Foundational Class Diagram](diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md) for the main software structure.
4. Use the [Full Class Diagram](diagrams/FULL_CLASS_DIAGRAM.md) only when your professor asks for complete class coverage.
5. Use the [Database Connection Sequence Diagram](diagrams/DATABASE_CONNECTION_FLOW.md) when explaining Supabase/PostgREST.

## Key Explanation

Use this sentence during your defense:

> The ERD explains how persistent data is stored in the database, while the class diagrams explain how software objects collaborate at runtime inside the WPF application.
