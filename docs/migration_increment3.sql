-- Supabase/Postgres migration for the per-machine price override refactor.
-- Apply this on older live databases that do not yet have machine_inventory.slot_price.

ALTER TABLE public.machine_inventory
ADD COLUMN IF NOT EXISTS slot_price NUMERIC(10, 2) NULL;

-- Normalize legacy slot IDs such as S1/S2 into canonical 1/2.
UPDATE public.machine_inventory
SET slot_id = regexp_replace(upper(slot_id), '^S([0-9]+)$', '\1')
WHERE upper(slot_id) ~ '^S[0-9]+$';

-- Optional verification query:
-- SELECT inventory_id, machine_id, slot_id FROM public.machine_inventory ORDER BY machine_id, slot_id;
