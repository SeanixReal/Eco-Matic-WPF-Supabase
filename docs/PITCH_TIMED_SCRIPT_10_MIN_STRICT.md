# Eco-Matic Client Pitch Script (Strict 10-Minute Timing)

Use this script exactly for the 10-minute presentation block.
Goal: client-style pitch with technical confidence for professor Q and A.

## Time Map

- 0:00-0:40: Slide 1
- 0:40-1:35: Slide 2
- 1:35-2:30: Slide 3
- 2:30-3:45: Slide 4
- 3:45-4:45: Slide 5
- 4:45-5:55: Slide 6
- 5:55-7:10: Slide 7
- 7:10-8:15: Slide 8
- 8:15-9:10: Slide 9
- 9:10-10:00: Slide 10

## Slide 1 (0:00-0:40) - Opening and Promise

"Good day, Professor and panel. I am Seani, and I will pitch Eco-Matic as a smart vending solution for potential clients. Eco-Matic combines customer vending, recycling incentives, admin machine management, cloud data, and Arduino RFID integration in one platform. My objective today is to show the value first, then prove technical feasibility with the running system and architecture."

## Slide 2 (0:40-1:35) - Client Problem

"Traditional vending operations often suffer from manual stock checking, delayed restocking decisions, and low customer engagement. Operators usually react only after stock-outs happen. Eco-Matic addresses these gaps by giving structured machine management, machine-specific inventory tracking, and a customer experience that includes recycling credits. So the problem we solve is both operational and engagement-related."

## Slide 3 (1:35-2:30) - Solution and Value

"Eco-Matic gives two clear value tracks. For customers, it delivers a smooth buying flow with receipt support and optional RFID-linked eco points. For operators, it provides inventory and machine control through role-based admin views. Under the hood, this is supported by a layered architecture so updates can be made without rewriting the whole app."

## Slide 4 (2:30-3:45) - Live Workflow Proof

"Here is the end-to-end workflow. Customer selects a machine, purchases an item, and receives a receipt. In the same flow, stock is updated and sale and event records are stored. On the admin side, item catalog and machine slot assignments can be updated quickly. This demonstrates complete operational coverage, not isolated features."

## Slide 5 (3:45-4:45) - Experience and Usability

"The customer UI is optimized for fast action using a clear 12-slot layout. The admin UI is structured by responsibilities: inventory, catalog, machines, users, sales, logs, and customers. This design reduces confusion, improves speed for frequent tasks, and supports role-based restrictions for controlled access."

## Slide 6 (4:45-5:55) - PC to Arduino Communication

"PC and Arduino communicate through USB serial using ArduinoService. Arduino sends RFID messages formatted as RFID colon UID. The desktop app listens for scan events, validates the RFID against Supabase, and replies with explicit hardware commands like VALID or INVALID. It also sends LCD and state messages such as MSG text, STATE ACTIVE, and STATE AFK. Validation runs asynchronously to keep UI responsiveness and hardware feedback speed."

## Slide 7 (5:55-7:10) - Database and ERD Logic

"The backend is Supabase PostgreSQL accessed through REST. The UI calls SupabaseStore methods, and SupabaseStore uses SupabaseClient to call PostgREST endpoints. In the ERD, items is the global product catalog, while machine_inventory is machine-slot stock and pricing. This separation supports reuse of one item definition across multiple machines with independent stock and slot control."

## Slide 8 (7:10-8:15) - Engineering Trust Points

"The project uses MVVM-lite with service separation. UI code does not directly implement backend and hardware networking details. Complex concerns like serial communication, payment intent handling, receipt printing, and sync routing are isolated in dedicated classes. This reduces coupling and makes phased improvements realistic for deployment growth."

## Slide 9 (8:15-9:10) - Current Scope and Roadmap

"Current-ready scope includes customer vending, admin management, and reporting with documented architecture and diagrams. Known limitations are transparent: RFID is currently used for registration and credit-saving workflows, while purchase payment remains cash or QR flow. Next improvements include stronger credential security and broader offline and telemetry depth."

## Slide 10 (9:10-10:00) - Close and Q and A Transition

"Eco-Matic is a practical smart-vending platform that combines customer experience, machine operations, and technical scalability. It is already structured for pilot-style deployment and iterative expansion. Thank you. I am ready for questions on communication flow, database design, and implementation details."

## Safety Buffer Notes

If you are running late:

- shorten Slide 8 to two sentences
- shorten Slide 9 to one current scope sentence plus one roadmap sentence

If demo stalls:

"I will continue with architecture and sequence flow while the app reloads."
