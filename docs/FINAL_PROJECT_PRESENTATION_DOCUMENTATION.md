# Final Project Presentation Documentation (Client Pitch Edition)

Project: Eco-Matic (WPF + Supabase + Arduino)
Presenter: Seani
Target format: 15 minutes total (10 minutes pitch + 5 minutes Q&A)

## 1. Required Deliverables Checklist

Use this as your final pre-defense checklist:

- [ ] Running system is prepared and tested.
- [ ] Hard copy of ERD is printed.
- [ ] Hard copy of class diagram is printed.
- [ ] PowerPoint follows rubric categories and client-pitch structure.
- [ ] You can explain PC-to-Arduino communication clearly.
- [ ] You can explain database design and flow clearly.
- [ ] You can explain business value for potential clients clearly.

Recommended printable diagrams:

- ERD: docs/diagrams/ERD.md
- Foundational class diagram: docs/diagrams/FOUNDATIONAL_CLASS_DIAGRAM.md
- Optional full class diagram backup: docs/diagrams/FULL_CLASS_DIAGRAM.md

## 2. Pitch Positioning Goal

Main message to deliver:

Eco-Matic is not only a school project. It is a deployable smart-vending platform that combines:

- WPF desktop application
- Supabase PostgreSQL backend through REST
- Arduino RFID and LCD messaging over serial communication
- Session state and Supabase-only customer mode behavior

Business-facing value to emphasize:

- better inventory visibility for operators
- faster restocking decisions per machine
- sustainability engagement through recycling credits
- extensible foundation for QR, telemetry, and multi-machine scaling

## 3. Rubric Mapping for a Client-Style Pitch

| Rubric Area | What to Show | What to Say |
| --- | --- | --- |
| Functionality | Live flow from machine selection to purchase, receipt, and admin inventory update | "Eco-Matic solves day-to-day vending operations from customer purchase to admin control." |
| User Interface | Customer window, admin tabs, machine selection, registration/dashboard windows | "The interface is practical for real users: customers act quickly and operators manage clearly." |
| Code Quality | Layered architecture: windows -> DataStore/SupabaseStore -> SupabaseClient, plus ArduinoService | "The design lowers maintenance cost because UI, backend, and hardware logic are separated." |
| Documentation | ERD and class diagram hard copies; architecture/flow docs | "Decision-makers can see technical traceability from workflow to schema and classes." |
| Presentation | Problem-first story, value proposition, demo proof, technical trust, call-to-action | "I will pitch the business value, then prove feasibility with implementation details." |

## 4. Recommended 10-Minute Client Pitch Flow

- 0:00 - 0:40: Opening and market problem
- 0:40 - 1:40: Solution and value proposition
- 1:40 - 3:30: Running system demo
- 3:30 - 4:40: Customer and operator outcomes
- 4:40 - 5:50: PC-to-Arduino communication trust point
- 5:50 - 7:10: ERD and database reliability trust point
- 7:10 - 8:30: Scalability, maintainability, and deployment readiness
- 8:30 - 9:20: Limitations and roadmap
- 9:20 - 10:00: Client-facing close and Q&A transition

## 5. Live Demo Sequence (Safe, Fast, and Client-Focused)

Use this exact order to avoid confusion:

1. Launch app from MainWindow.
2. Show Customer flow:
   - select machine
   - buy one item
   - show receipt
   - mention customer convenience and brand experience
3. Trigger RFID flow:
   - show existing user path OR registration path
   - mention eco-credit engagement value
4. Show Admin flow:
   - login
   - update catalog or inventory slot
   - show sales/logs quickly
   - mention operational visibility value

Fallback line if internet is unstable:

"Customer mode, admin mode, and RFID persistence now use the live Supabase path. If Supabase is unavailable, the app shows a connectivity message instead of using a local database fallback."

## 6. Technical Trust Points You Must Memorize

### A. How PC communicates with Arduino

- The app uses SerialPort (USB serial), default COM5, 9600 baud.
- Arduino sends line-based RFID payloads in format: RFID:<UID>.
- ArduinoService listens to serial data and raises OnCardScanned events.
- MainWindow handles RFID checks asynchronously (background task) to avoid UI blocking.
- App replies quickly with VALID or INVALID.
- App can also send LCD/status commands such as MSG:<text>, STATE:ACTIVE, and STATE:AFK.

Strong answer line:

"Communication is event-driven over serial. Arduino publishes RFID data, the WPF app validates against Supabase, and responds with explicit validation and LCD status commands."

### B. How database works

- Supabase PostgreSQL is the cloud database.
- WPF windows call SupabaseStore methods (service layer), not raw SQL in UI code.
- SupabaseStore uses SupabaseClient to call PostgREST endpoints at /rest/v1.
- Environment variables in .env provide the Supabase URL and key.
- Core normalized design: items is global catalog, machine_inventory is per-machine slot stock and optional machine item price.

Strong answer line:

"The data flow is UI -> SupabaseStore -> SupabaseClient -> Supabase PostgREST -> PostgreSQL tables. This keeps networking and table details out of the UI layer."

## 7. Client Value Lines You Can Reuse

Use these short lines throughout your pitch:

- "Eco-Matic helps operators reduce manual inventory guesswork."
- "Eco-Matic improves customer engagement by rewarding recycling behavior."
- "Eco-Matic supports multi-machine operations through machine-specific slot and stock control."
- "Eco-Matic is built on a modular architecture, so enhancements can be added without rewriting the whole system."

## 8. Important Corrections vs Your Old Deck

Your previous deck is readable, but it is based on the old console/file-handling version. For final defense, remove or replace:

- Old "console app" framing
- CSV/file-handling mechanics as the main data architecture
- .NET v9 console-centric tooling section

Replace with:

- WPF desktop architecture
- Supabase relational backend + ERD focus
- Arduino serial communication flow
- Customer/admin integrated runtime demonstration
- clear client value proposition per feature

## 9. Final Night-Before Checklist

- [ ] Run a full dry run with timer (target 9:30 to 9:50 speaking time).
- [ ] Rehearse value proposition + database + Arduino answers out loud 3 times.
- [ ] Keep one backup screenshot per critical flow in case live demo fails.
- [ ] Print ERD and class diagram.
- [ ] Keep this script file open during practice.
