USE ecomatic_db;

-- Add slot_price for machine-specific price overrides.
ALTER TABLE machine_inventory
ADD COLUMN IF NOT EXISTS slot_price DECIMAL(10, 2) NULL AFTER max_capacity;

-- Normalize legacy slot IDs such as S1/S2 into canonical 1/2.
UPDATE machine_inventory
SET slot_id = REPLACE(UPPER(slot_id), 'S', '')
WHERE UPPER(slot_id) REGEXP '^S[0-9]+$';

-- Optional cleanup: keep only the canonical 1..12 range.
-- Review first before running on production data.
-- DELETE FROM machine_inventory WHERE CAST(slot_id AS UNSIGNED) NOT BETWEEN 1 AND 12;
