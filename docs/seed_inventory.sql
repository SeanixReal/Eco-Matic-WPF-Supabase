USE ecomatic_db;

-- Clear previous machine inventory to start fresh if needed, but not necessary since it's empty.

-- 1. Mr Chips (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Mr Chips', 'Snack', 30.50, 160, '/Assets/Images/MrChips.png');
SET @img1 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img1, 15, 15);

-- 2. Nova (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Nova', 'Snack', 40.00, 180, '/Assets/Images/Nova.png');
SET @img2 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img2, 15, 15);

-- 3. Coca Cola (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('Coca Cola', 'Drink', 30.50, 500, '/Assets/Images/CocaCola.png');
SET @img3 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img3, 15, 15);

-- 4. Pepsi (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('Pepsi', 'Drink', 30.00, 500, '/Assets/Images/Pepsi.png');
SET @img4 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img4, 15, 15);

-- 5. Bandaid Box (Misc)
INSERT INTO items (name, type, price, image_path) VALUES ('Bandaid Box', 'Misc', 20.00, '/Assets/Images/BandaidBox.png');
SET @img5 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img5, 15, 15);

-- 6. Eco Bag (Misc)
INSERT INTO items (name, type, price, image_path) VALUES ('Eco Bag', 'Misc', 30.75, '/Assets/Images/EcoBag.png');
SET @img6 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img6, 15, 15);

-- 7. Piattos (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Piattos', 'Snack', 35.00, 150, '/Assets/Images/Piattos.png');
SET @img7 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img7, 15, 15);

-- 8. Chippy (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Chippy', 'Snack', 32.00, 170, '/Assets/Images/Chippy.png');
SET @img8 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img8, 15, 15);

-- 9. Roller Coaster (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Roller Coaster', 'Snack', 28.50, 140, '/Assets/Images/RollerCoaster.png');
SET @img9 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img9, 15, 15);

-- 10. Cheese Ring (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Cheese Ring', 'Snack', 30.00, 160, '/Assets/Images/CheeseRing.png');
SET @img10 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img10, 15, 15);

-- 11. RC Cola (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('RC Cola', 'Drink', 25.00, 500, '/Assets/Images/RCCola.png');
SET @img11 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img11, 15, 15);

-- 12. Sting (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('Sting', 'Drink', 27.50, 500, '/Assets/Images/Sting.png');
SET @img12 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img12, 15, 15);

-- 13. Zest-O Orange (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('Zest-O Orange', 'Drink', 200.00, 500, '/Assets/Images/ZestOOrange.png');
SET @img13 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img13, 15, 15);

-- 14. Del Monte Pineapple Juice (Drink)
INSERT INTO items (name, type, price, volume_ml, image_path) VALUES ('Del Monte Pineapple Juice', 'Drink', 22.50, 250, '/Assets/Images/DelMontePineappleJuice.png');
SET @img14 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img14, 15, 15);

-- 15. Chocolate Bar (Snack)
INSERT INTO items (name, type, price, calories, image_path) VALUES ('Chocolate Bar', 'Snack', 40.00, 250, '/Assets/Images/ChocolateBar.png');
SET @img15 = LAST_INSERT_ID();
INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (1, @img15, 15, 15);
