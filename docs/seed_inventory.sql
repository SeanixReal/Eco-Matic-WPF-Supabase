USE ecomatic_db;

-- Sample machines for demo/testing.
INSERT IGNORE INTO vending_machines (machine_id, location_name, status) VALUES
(1, 'Main Hall Machine', 'Active'),
(2, 'Library Annex', 'Active');

-- Global catalog items
INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Mr Chips', 'Snack', 30.50, 160, '/Assets/Images/MrChips.png', 'Crunch away! Enjoy your Mr Chips!', 'A classic corn chip snack packed with cheesy, savory goodness.');
SET @mr_chips = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Nova', 'Snack', 40.00, 180, '/Assets/Images/Nova.png', 'Grab a multigrain bite!', 'A healthy, multigrain snack with a distinctive wave shape.');
SET @nova = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Piattos', 'Snack', 35.00, 150, '/Assets/Images/Piattos.png', 'Hexagonal crunch time! Enjoy your Piattos!', 'Savory hexagon-shaped potato crisps coated in delicious seasoning.');
SET @piattos = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Chippy', 'Snack', 32.00, 170, '/Assets/Images/Chippy.png', 'Time for a barbecue blast! Enjoy your Chippy!', 'Iconic barbecue-flavored corn chips with a hearty crunch.');
SET @chippy = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Roller Coaster', 'Snack', 28.50, 140, '/Assets/Images/RollerCoaster.png', 'Have a fun ride with Roller Coaster rings!', 'Fun, cheese-flavored potato rings that loop around your fingers.');
SET @roller_coaster = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Cheese Ring', 'Snack', 30.00, 160, '/Assets/Images/CheeseRing.png', 'Cheesy goodness coming right up!', 'Light and airy cheese-flavored puffed corn rings.');
SET @cheese_ring = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Coca Cola', 'Drink', 30.50, 0, '/Assets/Images/CocaCola.png', 'Enjoy your ice-cold Coke! Stay refreshed!', 'The classic, iconic fizzy cola drink known worldwide.');
SET @coca_cola = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Pepsi', 'Drink', 30.00, 0, '/Assets/Images/Pepsi.png', 'Pop it open and enjoy the bold taste of Pepsi!', 'A sweet, slightly citrusy classic cola beverage.');
SET @pepsi = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('RC Cola', 'Drink', 25.00, 0, '/Assets/Images/RCCola.png', 'Refresh yourself with an RC Cola!', 'A crisp, refreshing cola with a smooth finish.');
SET @rc_cola = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Sting', 'Drink', 27.50, 0, '/Assets/Images/Sting.png', 'Power up! Here is your Sting energy!', 'A bright red, strawberry-flavored energy drink to keep you energized.');
SET @sting = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Bandaid Box', 'Misc', 20.00, 0, '/Assets/Images/BandaidBox.png', 'Ouch! Hope it heals quickly!', 'A small box of sterile adhesive bandages for minor cuts.');
SET @bandaid_box = LAST_INSERT_ID();

INSERT INTO items (name, type, price, calories, image_path, dispense_message, examine_message)
VALUES ('Eco Bag', 'Misc', 30.75, 0, '/Assets/Images/EcoBag.png', 'Thank you for loving the Earth! Happy carrying!', 'A reusable, eco-friendly tote bag designed to reduce plastic waste.');
SET @eco_bag = LAST_INSERT_ID();

-- Machine 1: Main Hall Machine
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @mr_chips, '1', 14, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @coca_cola, '2', 10, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @pepsi, '3', 8, 15, 32.00);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @piattos, '4', 6, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @chippy, '5', 5, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (1, @eco_bag, '6', 4, 15, 35.00);

-- Machine 2: Library Annex
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @nova, '1', 12, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @rc_cola, '2', 9, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @sting, '3', 11, 15, 29.00);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @roller_coaster, '4', 7, 15, NULL);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @cheese_ring, '5', 5, 15, 31.50);
INSERT INTO machine_inventory (machine_id, item_id, slot_id, stock_level, max_capacity, slot_price) VALUES (2, @bandaid_box, '6', 3, 15, NULL);

-- Result:
-- The same global item catalog is shared,
-- but each machine has its own subset of items, stock values, and optional slot-specific prices.
