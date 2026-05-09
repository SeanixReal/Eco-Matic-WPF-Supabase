# Final Project PowerPoint Contents (Client Pitch Version)

Use this as direct copy material for your slides.
Target: 10 slides total, around 55-65 seconds per slide.

## Slide 1 - Title and Client Promise

Title:
Eco-Matic: Smart Vending with RFID and Recycling Credits

Subtitle:
WPF Desktop System with Supabase Backend and Arduino Integration

On-slide bullets:

- Smart vending with recycling engagement
- Real-time machine operations visibility
- Scalable software and hardware integration

Visuals to place:

- System screenshot collage (MainWindow, CustomerWindow, AdminWindow)
- Your name, section, and date

Speaker cue:

"Eco-Matic helps vending operators improve service quality and sustainability by combining customer vending, eco-credit engagement, and centralized machine management in one platform."

## Slide 2 - Client Pain Points

Title:
The Problem We Solve

On-slide bullets:

- Manual stock checks waste operator time
- Stock-outs reduce customer trust and sales opportunities
- Limited sustainability features reduce brand engagement
- Multi-machine operations need clearer control and accountability

Visuals to place:

- 3 objective cards: Sustainability, Automation, Maintainability

Speaker cue:

"We focused on real operational pain points: inventory uncertainty, slow restocking decisions, and low sustainability engagement in traditional vending workflows."

## Slide 3 - Solution and Value Proposition

Title:
Eco-Matic Solution

On-slide bullets:

- Customer experience: fast selection, receipt, and RFID-linked recycling credits
- Operator experience: role-based admin console for items, inventory, and reports
- Connected reliability: Arduino RFID + Supabase cloud backend
- Scalable model: machine-specific slot inventory for multi-machine growth

Visuals to place:

- Layered architecture diagram
- Arrows from UI to services to backend/hardware

Speaker cue:

"Eco-Matic delivers value on both sides: a better customer experience and better operational control for machine operators."

## Slide 4 - Live Product Walkthrough

Title:
How It Works in Real Use

On-slide bullets:

- Customer selects machine and buys item
- Inventory updates and sale/log records are saved
- Receipt session is generated and can be printed
- Admin can update items, slots, and machine settings

Visuals to place:

- 4-step flow images from your running app

Speaker cue:

"This flow shows how Eco-Matic converts a normal vending transaction into a trackable, manageable, and service-improving workflow."

## Slide 5 - Customer and Operator Experience

Title:
Designed for Daily Use

On-slide bullets:

- 12-slot customer vending layout for fast selection
- Dedicated admin tabs for catalog, inventory, machines, sales, logs
- Consistent controls and visual feedback
- Dialog-based workflows reduce user confusion

Visuals to place:

- Side-by-side screenshots: CustomerWindow and AdminWindow

Speaker cue:

"The interface is designed for speed and clarity because both customers and operators need low-friction daily interactions."

## Slide 6 - Reliability: PC to Arduino Communication

Title:
Hardware Integration: PC and Arduino

On-slide bullets:

- USB serial communication via SerialPort
- Arduino sends RFID data in RFID:<UID> format
- App validates RFID and replies VALID or INVALID
- App sends LCD/state messages: MSG:<text>, STATE:ACTIVE, STATE:AFK

Visuals to place:

- Sequence diagram: Arduino -> ArduinoService -> MainWindow -> SupabaseStore -> response

Speaker cue:

"This hardware link is event-driven and responsive: Arduino sends scans, the app validates asynchronously, and sends immediate commands back so user feedback stays fast and reliable."

## Slide 7 - Data Backbone and ERD

Title:
Database Design (Supabase PostgreSQL)

On-slide bullets:

- items = global product catalog
- machine_inventory = per-machine slot stock and machine item price
- sales_transactions and event_logs support reporting/auditing
- customers stores RFID accounts and eco-credit balances

Visuals to place:

- Printed ERD photo or rendered ERD snippet

Speaker cue:

"Our ERD design supports growth: one global item can be reused across many machines, while each machine keeps its own slot, stock, and pricing control."

## Slide 8 - Why Clients Can Trust This Build

Title:
Engineering Decisions That Reduce Risk

On-slide bullets:

- MVVM-lite with reusable service classes
- UI avoids direct backend/network implementation details
- Core integrations isolated: database, hardware, image loading, payments
- Source structure supports incremental improvements

Visuals to place:

- Foundational class diagram
- Folder map screenshot

Speaker cue:

"We intentionally separated responsibilities so future updates, debugging, and new features can be added with lower disruption to existing operations."

## Slide 9 - Deployment Path and Roadmap

Title:
What We Can Deploy Now and Improve Next

On-slide bullets:

- Current-ready value: customer vending, admin machine control, and reporting
- Submission-ready documentation: ERD, class diagram, architecture flow
- Known limits are transparent and manageable
- Next upgrades: stronger security, broader offline support, deeper telemetry integration

Visuals to place:

- Documentation index screenshot
- Small roadmap timeline

Speaker cue:

"This is not positioned as perfect today, but as a practical, expandable platform with clear next-step engineering priorities."

## Slide 10 - Client Ask and Closing

Title:
Why Eco-Matic Is Worth Adopting

On-slide bullets:

- Delivers operational clarity and better customer engagement
- Built on a scalable architecture for multi-machine deployments
- Ready for pilot deployment and iterative enhancements
- Thank you - Questions and partnership discussions

Visuals to place:

- Clean summary graphic with three pillars: Software, Data, Hardware

Speaker cue:

"Eco-Matic offers a practical path from prototype to deployable smart-vending operations. Thank you, and I am ready for your questions."

## Presenter Notes (Timing Quick Guide)

- Slides 1-2: 1:40
- Slides 3-5: 3:00
- Slide 6: 1:10
- Slide 7: 1:20
- Slides 8-9: 1:40
- Slide 10: 1:10
Total: about 10:00
