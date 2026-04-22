-- Supabase/Postgres migration for offline sync replay idempotency.
-- Apply this before using the local offline queue replay feature.

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
