-- Supabase/Postgres migration for richer vending machine location data.
-- Adds an editable address plus optional map-picked latitude/longitude.

ALTER TABLE public.vending_machines
    ADD COLUMN IF NOT EXISTS address_text TEXT NULL,
    ADD COLUMN IF NOT EXISTS latitude DOUBLE PRECISION NULL,
    ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION NULL;
