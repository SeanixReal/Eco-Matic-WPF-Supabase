import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
const publicBaseUrl = supabaseUrl.replace(/\/$/, "");
const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
const supabase = createClient(supabaseUrl, serviceRoleKey, {
  auth: { persistSession: false },
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function text(body: string, status = 200) {
  return new Response(body, {
    status,
    headers: { ...corsHeaders, "Content-Type": "text/plain; charset=utf-8" },
  });
}

function buildReference() {
  const stamp = new Date().toISOString().replace(/[-:.TZ]/g, "").slice(0, 14);
  return `QR-${stamp}-${crypto.randomUUID().slice(0, 6).toUpperCase()}`;
}

async function createIntent(body: { machine_id?: number; amount?: number }) {
  const amount = Number(body.amount ?? 0);
  const machineId = Number(body.machine_id ?? 0);

  if (!Number.isFinite(amount) || amount <= 0) {
    return json({ error: "Invalid payment amount." }, 400);
  }

  const reference = buildReference();
  const token = crypto.randomUUID().replaceAll("-", "");

  const { error } = await supabase.from("qr_payment_intents").insert({
    reference,
    token,
    machine_id: machineId > 0 ? machineId : null,
    amount,
    status: "pending",
  });

  if (error) {
    return json({ error: error.message }, 500);
  }

  const confirm_url = `${publicBaseUrl}/functions/v1/qr-payment-confirm?ref=${encodeURIComponent(reference)}&token=${encodeURIComponent(token)}`;
  return json({ reference, token, confirm_url });
}

async function getIntent(reference: string, token: string) {
  return await supabase
    .from("qr_payment_intents")
    .select("reference, machine_id, amount, status, created_at, paid_at")
    .eq("reference", reference)
    .eq("token", token)
    .maybeSingle();
}

async function getStatus(url: URL) {
  const reference = url.searchParams.get("ref") ?? "";
  const token = url.searchParams.get("token") ?? "";

  if (!reference || !token) {
    return json({ error: "Missing QR payment reference." }, 400);
  }

  const { data, error } = await getIntent(reference, token);
  if (error) {
    return json({ error: error.message }, 500);
  }
  if (!data) {
    return json({ error: "QR payment not found." }, 404);
  }

  return json(data);
}

async function confirmPayment(url: URL) {
  const reference = url.searchParams.get("ref") ?? "";
  const token = url.searchParams.get("token") ?? "";

  if (!reference || !token) {
    return text("Invalid Eco-Matic QR payment code.", 400);
  }

  const { data, error } = await getIntent(reference, token);
  if (error || !data) {
    return text("Eco-Matic QR payment not found. Please generate a new QR code.", 404);
  }

  if (data.status !== "paid") {
    const paidAt = new Date().toISOString();
    const { error: updateError } = await supabase
      .from("qr_payment_intents")
      .update({
        status: "paid",
        scanned_at: paidAt,
        paid_at: paidAt,
      })
      .eq("reference", reference)
      .eq("token", token);

    if (updateError) {
      return text("Eco-Matic could not confirm this QR payment. Please try again.", 500);
    }
  }

  return text(`Payment confirmed!\n\nReference: ${data.reference}\nAmount: PHP ${Number(data.amount).toFixed(2)}\n\nYou can return to the Eco-Matic vending machine.`);
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  const url = new URL(req.url);

  if (req.method === "GET" && url.searchParams.get("status") === "1") {
    return await getStatus(url);
  }

  if (req.method === "GET") {
    return await confirmPayment(url);
  }

  if (req.method === "POST") {
    const body = await req.json().catch(() => null) as { machine_id?: number; amount?: number } | null;
    if (!body) {
      return json({ error: "Invalid JSON body." }, 400);
    }

    return await createIntent(body);
  }

  return json({ error: "Method not allowed." }, 405);
});
