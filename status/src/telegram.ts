// Alerts to a Telegram chat; Lilejka's DevAlerts bot works here too. Off while the token or the
// chat id is empty, and a failed send never fails the cron.

import type { Env } from "./env";

export async function notify(env: Env, text: string): Promise<void> {
  if (!env.TELEGRAM_BOT_TOKEN || !env.TELEGRAM_CHAT_ID) return;
  try {
    const res = await fetch(`https://api.telegram.org/bot${env.TELEGRAM_BOT_TOKEN}/sendMessage`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ chat_id: env.TELEGRAM_CHAT_ID, text, disable_web_page_preview: true }),
    });
    if (!res.ok) console.error(`telegram: HTTP ${res.status}`);
  } catch (e) {
    console.error("telegram:", e);
  }
}
