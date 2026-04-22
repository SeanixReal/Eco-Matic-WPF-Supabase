-- Final robust migration script for Eco-Matic Increment 2
USE ecomatic_db;

-- 1. Safely add columns to 'items'
DROP PROCEDURE IF EXISTS AddItemsColumns;
DELIMITER //
CREATE PROCEDURE AddItemsColumns()
BEGIN
    IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='items' AND COLUMN_NAME='dispense_message') THEN
        ALTER TABLE items ADD COLUMN dispense_message VARCHAR(255) DEFAULT 'Enjoy your item!';
    END IF;
    IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='items' AND COLUMN_NAME='examine_message') THEN
        ALTER TABLE items ADD COLUMN examine_message TEXT;
    END IF;
END //
DELIMITER ;
CALL AddItemsColumns();
DROP PROCEDURE AddItemsColumns;

-- 2. Safely update 'machine_inventory'
-- Drop foreign keys first to allow index changes
SET @fk1 = (SELECT CONSTRAINT_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='machine_inventory' AND COLUMN_NAME='machine_id' AND REFERENCED_TABLE_NAME='vending_machines' LIMIT 1);
SET @drop_fk1 = IF(@fk1 IS NOT NULL, CONCAT('ALTER TABLE machine_inventory DROP FOREIGN KEY ', @fk1), 'SELECT "No FK1"');
PREPARE stmt1 FROM @drop_fk1; EXECUTE stmt1; DEALLOCATE PREPARE stmt1;

SET @fk2 = (SELECT CONSTRAINT_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='machine_inventory' AND COLUMN_NAME='item_id' AND REFERENCED_TABLE_NAME='items' LIMIT 1);
SET @drop_fk2 = IF(@fk2 IS NOT NULL, CONCAT('ALTER TABLE machine_inventory DROP FOREIGN KEY ', @fk2), 'SELECT "No FK2"');
PREPARE stmt2 FROM @drop_fk2; EXECUTE stmt2; DEALLOCATE PREPARE stmt2;

-- Add slot_id if missing
DROP PROCEDURE IF EXISTS AddSlotColumn;
DELIMITER //
CREATE PROCEDURE AddSlotColumn()
BEGIN
    IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='machine_inventory' AND COLUMN_NAME='slot_id') THEN
        ALTER TABLE machine_inventory ADD COLUMN slot_id VARCHAR(10) NOT NULL AFTER item_id;
        UPDATE machine_inventory SET slot_id = CONCAT('S', inventory_id);
    END IF;
END //
DELIMITER ;
CALL AddSlotColumn();
DROP PROCEDURE AddSlotColumn;

-- Drop old unique index
SET @idx = (SELECT INDEX_NAME FROM information_schema.STATISTICS WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='machine_inventory' AND INDEX_NAME='unique_machine_item' LIMIT 1);
SET @drop_idx = IF(@idx IS NOT NULL, 'ALTER TABLE machine_inventory DROP INDEX unique_machine_item', 'SELECT "No old index"');
PREPARE stmt3 FROM @drop_idx; EXECUTE stmt3; DEALLOCATE PREPARE stmt3;

-- Add new unique index
SET @idx2 = (SELECT INDEX_NAME FROM information_schema.STATISTICS WHERE TABLE_SCHEMA='ecomatic_db' AND TABLE_NAME='machine_inventory' AND INDEX_NAME='unique_machine_slot' LIMIT 1);
SET @add_idx = IF(@idx2 IS NULL, 'ALTER TABLE machine_inventory ADD UNIQUE KEY unique_machine_slot (machine_id, slot_id)', 'SELECT "New index exists"');
PREPARE stmt4 FROM @add_idx; EXECUTE stmt4; DEALLOCATE PREPARE stmt4;

-- Restore foreign keys
ALTER TABLE machine_inventory ADD CONSTRAINT machine_inventory_ibfk_1 FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE;
ALTER TABLE machine_inventory ADD CONSTRAINT machine_inventory_ibfk_2 FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE;
