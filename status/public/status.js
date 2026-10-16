// The public status page: one fetch of /v1/status, redrawn every minute.

const STATE_TEXT = { up: "Працює", degraded: "Повільно або збої", down: "Не відповідає", none: "Ще не перевіряли" };
const OVERALL_TEXT = {
  up: "Усі сервіси працюють",
  degraded: "Частина сервісів працює з перебоями",
  down: "Є сервіс, що не відповідає",
  maintenance: "Тривають планові роботи",
  none: "Ще немає перевірок",
};
const KIND_TEXT = { incident: "Збій", maintenance: "Планові роботи", notice: "Оголошення" };

const time = new Intl.DateTimeFormat("uk", { hour: "2-digit", minute: "2-digit" });
const dateTime = new Intl.DateTimeFormat("uk", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
const date = new Intl.DateTimeFormat("uk", { day: "numeric", month: "long", timeZone: "UTC" });
const percent = (x) => (x === null ? "—" : `${(Math.floor(x * 10000) / 100).toLocaleString("uk")} %`);

function el(tag, props = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (key === "state") node.dataset.state = value;
    else if (key === "text") node.textContent = value;
    else node.setAttribute(key, value);
  }
  for (const child of children) if (child !== null && child !== undefined) node.append(child);
  return node;
}

function dayCaption(name, bar) {
  const day = date.format(new Date(`${bar.day}T00:00:00Z`));
  if (bar.state === "none") return `${day}: даних немає`;
  const parts = [`${day}: ${STATE_TEXT[bar.state].toLowerCase()}`, `доступність ${percent(bar.uptime)}`];
  if (bar.avgMs !== null) parts.push(`відповідь у середньому ${bar.avgMs} мс`);
  return parts.join(", ");
}

function renderTarget(t) {
  const caption = el("p", { class: "caption" });
  const resting = t.state === "none" ? "" : `Зараз: ${t.latencyMs !== null ? `${t.latencyMs} мс` : "немає відповіді"}${t.detail ? `, ${t.detail}` : ""}${t.since ? `. Так з ${dateTime.format(t.since)}` : ""}`;
  caption.textContent = resting;

  const bars = el("div", { class: "bars", role: "img", "aria-label": `${t.name}: 90 днів, доступність ${percent(t.uptime)}` });
  for (const bar of t.days) bars.append(el("span", { state: bar.state, title: dayCaption(t.name, bar) }));
  const show = (event) => {
    const span = event.target.closest(".bars span");
    if (!span) return;
    bars.querySelectorAll(".active").forEach((s) => s.classList.remove("active"));
    span.classList.add("active");
    caption.textContent = span.title;
  };
  bars.addEventListener("pointerover", show);
  bars.addEventListener("click", show);
  bars.addEventListener("pointerleave", () => {
    bars.querySelectorAll(".active").forEach((s) => s.classList.remove("active"));
    caption.textContent = resting;
  });

  const max = Math.max(1, ...t.hours.map((h) => h.avgMs ?? 0));
  const hours = el("div", { class: "hours", role: "img", "aria-label": `${t.name}: середній час відповіді за 24 години` });
  for (const h of t.hours) {
    const span = el("span", { state: h.state, title: `${time.format(h.at)}: ${h.avgMs !== null ? `${h.avgMs} мс` : "немає відповіді"}` });
    span.style.height = `${Math.max(8, Math.round(((h.avgMs ?? 0) / max) * 100))}%`;
    hours.append(span);
  }

  const state = t.maintenance && t.state !== "up" ? "maintenance" : t.state;
  return el(
    "li",
    { class: "target", state },
    el(
      "div",
      { class: "target-head" },
      el("h3", { text: t.name }),
      t.version ? el("span", { class: "tag", text: t.version }) : null,
      el("span", { class: "state", text: state === "maintenance" ? "Планові роботи" : STATE_TEXT[t.state] }),
    ),
    bars,
    el(
      "div",
      { class: "scale" },
      el("span", { class: "from-90", text: "90 днів тому" }),
      el("span", { class: "from-30", text: "30 днів тому" }),
      el("span", { class: "uptime", text: `${percent(t.uptime)} доступності` }),
      el("span", { text: "сьогодні" }),
    ),
    caption,
    hours,
    el("div", { class: "hours-label" }, el("span", { text: "Відповідь за 24 години" }), el("span", { text: "зараз" })),
  );
}

function renderNotice(n, names) {
  const open = n.resolvedAt === null;
  const upcoming = open && n.startsAt > Date.now();
  const where = n.targets.length ? n.targets.map((id) => names.get(id) ?? id).join(", ") : "усі сервіси";
  const when = upcoming ? `з ${dateTime.format(n.startsAt)}` : `з ${dateTime.format(n.startsAt)}${open ? "" : ` до ${dateTime.format(n.resolvedAt)}`}`;
  return el(
    "section",
    { class: "tile notice", state: n.kind === "incident" ? "down" : n.kind === "maintenance" ? "degraded" : "up" },
    el("p", { class: "meta", text: `${KIND_TEXT[n.kind]}${upcoming ? ", заплановано" : open ? "" : ", завершено"} · ${where} · ${when}` }),
    el("h2", { text: n.title }),
    n.body ? el("p", { text: n.body }) : null,
  );
}

function renderHistory(status, names) {
  const items = [
    ...status.events.map((e) => ({
      at: e.at,
      state: e.state,
      title: `${e.name}: ${e.state === "up" ? (e.previous === "down" ? "знову працює" : "знову швидко") : STATE_TEXT[e.state].toLowerCase()}`,
      detail: e.detail,
    })),
    ...status.incidents
      .filter((n) => n.resolvedAt !== null)
      .map((n) => ({ at: n.startsAt, state: n.kind === "incident" ? "down" : "degraded", title: `${KIND_TEXT[n.kind]}: ${n.title}`, detail: n.body })),
  ].sort((a, b) => b.at - a.at);

  const list = document.getElementById("history");
  list.replaceChildren(
    ...items.slice(0, 30).map((i) =>
      el("li", { state: i.state }, el("time", { datetime: new Date(i.at).toISOString(), text: dateTime.format(i.at) }), el("span", { class: "what", text: i.title }), i.detail ? el("span", { class: "detail", text: i.detail }) : null),
    ),
  );
  document.getElementById("history-empty").hidden = items.length > 0;
}

async function load() {
  try {
    const res = await fetch("/v1/status", { headers: { Accept: "application/json" } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const status = await res.json();
    const names = new Map(status.targets.map((t) => [t.id, t.name]));

    const overall = document.getElementById("overall");
    overall.dataset.state = status.overall;
    document.getElementById("overall-title").textContent = OVERALL_TEXT[status.overall];
    document.getElementById("updated").textContent = `Оновлено о ${time.format(status.generatedAt)}`;
    document.title = `${status.overall === "up" ? "" : "⚠ "}Статус · Лілейка`;

    document.getElementById("notices").replaceChildren(...status.incidents.filter((n) => n.resolvedAt === null).map((n) => renderNotice(n, names)));
    document.getElementById("targets").replaceChildren(...status.targets.map(renderTarget));
    renderHistory(status, names);
  } catch (e) {
    document.getElementById("updated").textContent = `Не вдалося оновити (${e.message}). Спробую ще раз за хвилину.`;
  }
}

load();
setInterval(() => {
  if (document.visibilityState === "visible") load();
}, 60_000);
document.addEventListener("visibilitychange", () => {
  if (document.visibilityState === "visible") load();
});
