-- Supabase/Postgres migration for history-safe catalog deletion.
-- Run this before deploying the app code that filters items by is_active.
--
-- Design goal:
-- - deleting a catalog item removes it from active catalog/inventory workflows
-- - machine slots using that item are cleared by the app
-- - old sales_transactions keep joining to items for historical reports

ALTER TABLE public.items
ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL,
ADD COLUMN IF NOT EXISTS deleted_reason TEXT NULL;

UPDATE public.items
SET is_active = TRUE
WHERE is_active IS NULL;

CREATE INDEX IF NOT EXISTS idx_items_active_name
ON public.items (is_active, name);

CREATE INDEX IF NOT EXISTS idx_items_deleted_at
ON public.items (deleted_at)
WHERE is_active = FALSE;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM public.items
        WHERE is_active = TRUE
        GROUP BY lower(trim(name))
        HAVING count(*) > 1
    ) THEN
        RAISE NOTICE 'Skipped ux_items_active_name_ci because duplicate active item names exist. Resolve duplicates, then create the partial unique index.';
    ELSE
        CREATE UNIQUE INDEX IF NOT EXISTS ux_items_active_name_ci
        ON public.items (lower(trim(name)))
        WHERE is_active = TRUE;
    END IF;
END
$$;
