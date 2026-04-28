# Eco-Matic One-Page Cue Card

Use this during rehearsal. Memorize ideas, not exact words.

## 10-Slide Memory Anchors

1. Opening promise: value first, proof second.
2. Pain points: stock uncertainty, manual operations, weak engagement.
3. Value proposition: better customer flow + better operator control.
4. Live proof: buy item, update stock, save logs, print receipt.
5. UX quality: customer speed + admin clarity.
6. Hardware trust: serial RFID event flow and fast response.
7. Data trust: UI -> SupabaseStore -> SupabaseClient -> PostgREST -> PostgreSQL.
8. Engineering trust: service separation and maintainability.
9. Honest limits + roadmap.
10. Close: pilot-ready and scalable.

## Critical Numbers and Facts

- Total pitch time: 10 minutes
- Q and A: 5 minutes
- Customer visible slots: 12
- Max machines enforced in app flow: 4
- Arduino default connection: COM5, 9600 baud
- RFID message format: RFID:<UID>
- Response commands: VALID, INVALID
- LCD/state commands: MSG:<text>, STATE:ACTIVE, STATE:AFK

## Fast Formula for Any Technical Answer

1. Name the layer.
2. Describe runtime flow in one sentence.
3. Give one concrete implementation detail.
4. End with why the design choice matters.

Example:

"In the hardware layer, ArduinoService reads RFID serial lines and raises card-scan events. MainWindow validates in Supabase and sends back VALID or INVALID. We isolate this in a service class so UI remains responsive and hardware logic stays maintainable."

## Must-Hit Trust Lines

- "Communication is event-driven over serial with explicit response commands."
- "Database calls are centralized in service classes, not scattered in UI code."
- "items and machine_inventory are separated to support multi-machine scaling."
- "Known limitations are documented and planned, not hidden."

## If You Forget a Detail Mid-Pitch

Say:

"I want to answer precisely, so I will refer to the implementation flow, then answer directly."

Then use the 4-step formula above.

## Final 12-Second Close

"Eco-Matic is a practical, integrated smart-vending platform that improves customer engagement and operator control while staying technically scalable. Thank you, and I am ready for your questions."
