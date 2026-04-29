# Final Project Presentation Script (Client Pitch + Technical Q&A)

Presenter: Seani
Language: English
Delivery style: Clear, calm, confident, persuasive, and technically grounded

## 0. Entrance Script (20-30 seconds)

"Good day, Professor and panel. I am Seani, and I will pitch Eco-Matic as a smart vending solution for potential clients. Eco-Matic combines customer vending, recycling incentives, admin machine management, cloud data, and Arduino RFID integration in one platform. I will present the business value first, then prove technical feasibility through our architecture and live flow."

## 1. Slide-by-Slide Speaker Script

### Slide 1 - Title and Client Promise (0:00-0:45)

"Eco-Matic is positioned as a practical smart-vending platform. It improves daily operations for operators while creating a better and more engaging customer experience through recycling incentives and RFID-assisted interaction."

### Slide 2 - Client Pain Points (0:45-1:40)

"Traditional vending operations often rely on manual checks and delayed updates, which cause stock issues and missed sales opportunities. At the same time, sustainability engagement is usually absent. Eco-Matic addresses these pain points with machine-level visibility, role-based management, and eco-credit features."

### Slide 3 - Solution and Value Proposition (1:40-2:40)

"Our value proposition is simple: better service quality, better operational control, and a scalable software base. Customers get a smooth vending and receipt flow. Operators get inventory and machine management tools. Technically, this is backed by a layered architecture that keeps the system maintainable as it grows."

### Slide 4 - Live Product Walkthrough (2:40-3:45)

"This is the live end-to-end workflow. A customer selects a machine, purchases an item, and receives a receipt. In parallel, inventory and logs are updated for operator visibility. Admin users can adjust catalog and slot assignments quickly. This is the core value loop for both customer service and operations."

### Slide 5 - Customer and Operator Experience (3:45-4:45)

"The interface is intentionally practical for daily use. The customer side is straightforward and fast. The operator side groups management tasks into clear tabs to reduce confusion and speed up decision-making."

### Slide 6 - Reliability: How PC Communicates with Arduino (4:45-5:55)

"PC-to-Arduino communication is implemented through USB serial using ArduinoService. Arduino sends RFID scan lines in RFID:<UID> format. The desktop app listens for this event, validates the RFID in the database, then quickly responds with VALID or INVALID. The app can also send state and LCD messages like MSG:<text>, STATE:ACTIVE, and STATE:AFK. The RFID check runs asynchronously so the UI does not freeze and hardware receives timely feedback."

### Slide 7 - Reliability: How the Database Works (5:55-7:20)

"The backend is Supabase PostgreSQL, accessed through REST. The UI does not call the database directly. Instead, UI windows call SupabaseStore, then SupabaseStore uses SupabaseClient to call PostgREST endpoints. The normalized design is important: items stores global product definitions, while machine_inventory stores per-machine slot stock and optional slot-specific price. This is why the same item can exist in multiple machines with different stock and pricing."

### Slide 8 - Why Clients Can Trust This Build (7:20-8:20)

"From a client perspective, risk reduction matters. We separated UI logic from backend and hardware services, so the system is easier to maintain and extend. This design supports staged deployment and future features without rewriting the full application."

### Slide 9 - Deployment Path and Roadmap (8:20-9:15)

"We document the system transparently. Current functionality is ready for pilot-style demonstrations, and known limitations are clearly identified. For example, RFID is used for registration, recycle-credit saving, point-payment identity, and transaction history, while next steps focus on stronger security and broader integration depth."

### Slide 10 - Client Ask and Exit (9:15-10:00)

"To conclude, Eco-Matic is a credible smart-vending platform that balances customer experience, operator control, and technical feasibility. It is designed to support multi-machine growth and incremental upgrades. Thank you, and I am ready for your questions."

## 2. Q&A Defense Script (5 Minutes)

Use this section to answer confidently.

### Q1: How did your PC communicate with Arduino?

Answer:

"The app uses SerialPort over USB serial, with configurable COM port and baud rate. Arduino sends RFID events as RFID:<UID>. ArduinoService parses that input and raises OnCardScanned. MainWindow validates the RFID using SupabaseStore and sends immediate response commands back to Arduino, such as VALID, INVALID, and LCD/status messages using MSG and STATE commands."

### Q2: How does your database architecture work?

Answer:

"I used a service-layer approach. UI windows call SupabaseStore methods. SupabaseStore translates app operations into REST calls through SupabaseClient. SupabaseClient sends HTTP requests to Supabase PostgREST, which reads and writes PostgreSQL tables. This keeps database and networking details out of the UI."

### Q3: Why separate items and machine_inventory?

Answer:

"Because product identity and machine stock are different concerns. items is the global catalog, while machine_inventory stores slot assignment, stock, capacity, and slot-specific price per machine. This supports one item across many machines without duplication."

### Q4: How do you enforce machine-specific staff access?

Answer:

"Authentication checks role and assigned machine IDs. AdminWindow then restricts inventory managers to assigned machines and hides non-allowed pages."

### Q5: Is your app fully offline-capable?

Answer:

"Customer mode has offline-aware behavior after initial sync through local cache and replay mechanisms. Admin mode and RFID account persistence are currently online-oriented."

### Q6: Why not full MVVM?

Answer:

"Given project scope and timeline, I used MVVM-lite for practicality. However, critical reusable logic is still separated into services to preserve maintainability and clear responsibilities."

### Q7: What are your current known limitations?

Answer:

"Key limitations include credential-security hardening needs and the current separation where customer transaction history is application-layer event-log matching rather than a dedicated sales foreign key. These are identified as next engineering priorities."

### Q8: Why should a potential client adopt Eco-Matic instead of staying manual?

Answer:

"Eco-Matic improves operational visibility and customer engagement in one system. Operators can manage machine-specific inventory with better traceability, while customers get a smoother vending and recycling experience. The architecture also supports phased improvements, so adoption can start small and scale."

### Q9: What is your realistic rollout strategy?

Answer:

"Start with a pilot deployment on a small set of machines, observe stock, usage, and workflow metrics, then iterate. Because the system is modular, improvements can be introduced per phase without replacing the whole platform."

### Q10: What makes this solution trustworthy technically?

Answer:

"We can trace each major feature from UI flow to service layer to data schema and hardware behavior. PC-to-Arduino communication is explicit and event-driven, and database interactions are isolated through service classes for maintainability."

## 3. Emergency Recovery Lines During Defense

If demo stalls:

"I will continue with the architecture and sequence diagrams while the app reloads. The core flow is documented and matches the implementation."

If asked something you cannot recall exactly:

"I want to answer precisely, so I will refer to the corresponding architecture file and then give the exact implementation detail."

## 4. Quick Confidence Formula (Before You Start)

Use this mental structure for every answer:

1. State the layer or component.
2. Explain the runtime flow in one sentence.
3. Give one concrete implementation detail.
4. End with design reason or tradeoff.

Example:

"In the hardware layer, ArduinoService receives RFID serial messages, raises an event to MainWindow, and sends back VALID/INVALID responses. I designed it this way to keep serial logic isolated from UI code and keep responses fast."
