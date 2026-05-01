-- Supabase/Postgres migration for idempotent app writes with client_sync_id.
-- Apply this when older schemas are missing client_sync_id on activity tables.

ALTER TABLE public.sales_transactions
ADD COLUMN IF NOT EXISTS client_sync_id UUID NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_sales_transactions_client_sync_id
ON public.sales_transactions (client_sync_id)
WHERE client_sync_id IS NOT NULL;

ALTER TABLE public.event_logs
ADD COLUMN IF NOT EXISTS client_sync_id UUID NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_event_logs_client_sync_id
ON public.event_logs (client_sync_id)
WHERE client_sync_id IS NOT NULL;
