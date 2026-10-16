// /admin: telemetry from the instances and the notices shown on the public page. Cloudflare
// Access signs the owner in before this page loads; the API checks the same token.

const KIND_TEXT = { incident: "Збій", maintenance: "Планові роботи", notice: "Оголошення" };
const number = new Intl.NumberFormat("uk");
const dateTime = new Intl.DateTimeFormat("uk", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
const shortDate = new Intl.DateTimeFormat("uk", { day: "numeric", month: "short", timeZone: "UTC" });

function el(tag, props = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (key === "text") node.textContent = value;
    else if (key === "class") node.className = value;
    else node.setAttribute(key, value);
  }
  for (const child of children) if (child !== null && child !== undefined) node.append(child);
  return node;
}

async function api(path, init = {}) {
  const res = await fetch(path, { ...init, headers: { Accept: "application/json", ...(init.body ? { "Content-Type": "application/json" } : {}) } });
  if (res.status === 204) return null;
  const body = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(body.error ?? `HTTP ${res.status}`);
  return body;
}

function showError(e) {
  const box = document.getElementById("error");
  box.textContent = e ? `Помилка: ${e.message}` : "";
  box.hidden = !e;
}

/** "v1.10.2" > "v1.9.0"; null when either is not a plain release. */
function older(version, latest) {
  const parse = (v) => /^v?(\d+)\.(\d+)\.(\d+)$/.exec(v ?? "")?.slice(1).map(Number);
  const a = parse(version);
  const b = parse(latest);
  if (!a || !b) return false;
  for (let i = 0; i < 3; i++) if (a[i] !== b[i]) return a[i] < b[i];
  return false;
}

function renderStats(s) {
  const t = s.totals;
  const stat = (label, value, hint) => el("div", { class: "stat" }, el("div", { class: "label", text: label }), el("div", { class: "value", text: value }), hint ? el("div", { class: "hint", text: hint }) : null);
  document.getElementById("stats").replaceChildren(
    stat("Живі інстанси", number.format(t.instances), "пінгували за 7 днів"),
    stat("Курені", number.format(t.kurins)),
    stat("Члени", number.format(t.members)),
    stat("Активні користувачі", number.format(t.active7d), `${number.format(t.active30d)} за 30 днів`),
    stat("Запити за добу", number.format(t.requests24h), `${number.format(t.errors24h)} з них 5xx`),
    stat("Остання версія", s.latest ?? "невідомо", "з релізів GitHub"),
  );
}

function chart(title, rows, key) {
  const values = rows.map((r) => r[key] ?? 0);
  const box = el("div", { class: "chart" }, el("h3", { text: title }));
  if (values.length === 0) {
    box.append(el("p", { class: "muted small", text: "Ще немає пінгів." }));
    return box;
  }
  const max = Math.max(1, ...values);
  const w = 300;
  const h = 96;
  const x = (i) => (values.length === 1 ? w / 2 : (i / (values.length - 1)) * w);
  const y = (v) => h - 4 - (v / max) * (h - 12);
  const points = values.map((v, i) => `${x(i).toFixed(1)},${y(v).toFixed(1)}`).join(" ");
  const ns = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(ns, "svg");
  svg.setAttribute("viewBox", `0 0 ${w} ${h}`);
  svg.setAttribute("preserveAspectRatio", "none");
  svg.setAttribute("role", "img");
  svg.setAttribute("aria-label", `${title}: зараз ${number.format(values.at(-1))}, максимум ${number.format(max)}`);
  const area = document.createElementNS(ns, "polygon");
  area.setAttribute("class", "area");
  area.setAttribute("points", `${x(0)},${h} ${points} ${x(values.length - 1)},${h}`);
  const line = document.createElementNS(ns, "polyline");
  line.setAttribute("class", "line");
  line.setAttribute("points", points);
  svg.append(area, line);
  box.append(
    svg,
    el("div", { class: "axis" }, el("span", { text: shortDate.format(new Date(`${rows[0].day}T00:00:00Z`)) }), el("span", { text: `зараз ${number.format(values.at(-1))}` })),
  );
  return box;
}

function counts(id, rows, label, latest) {
  const max = Math.max(1, ...rows.map((r) => r.count));
  document.getElementById(id).replaceChildren(
    ...(rows.length
      ? rows.map((r) => {
          const bar = el("span");
          bar.style.width = `${(r.count / max) * 100}%`;
          const name = label(r);
          return el(
            "li",
            { class: latest && older(name, latest) ? "old" : "" },
            el("span", { text: latest && older(name, latest) ? `${name} (застаріла)` : name }),
            el("span", { text: number.format(r.count) }),
            el("div", { class: "bar" }, bar),
          );
        })
      : [el("li", { class: "muted", text: "—" })]),
  );
}

function renderInstances(s) {
  document.getElementById("instances").replaceChildren(
    ...s.instances.map((i) => {
      const remove = el("button", { class: "btn btn-text", type: "button", text: "Прибрати" });
      remove.addEventListener("click", () => {
        if (!confirm(`Прибрати інстанс ${i.id.slice(0, 8)} і його історію?`)) return;
        void removeInstance(i.id);
      });
      const version = el("span", { class: older(i.version, s.latest) ? "tag danger" : "tag", text: i.version });
      return el(
        "tr",
        { class: i.live ? "" : "stale" },
        el("td", {}, el("code", { title: i.id, text: i.id.slice(0, 8) })),
        el("td", { text: i.kind }),
        el("td", {}, version),
        el("td", { text: i.krais || "—" }),
        el("td", { text: i.country ?? "—" }),
        el("td", { class: "num", text: number.format(i.kurins) }),
        el("td", { class: "num", text: number.format(i.members) }),
        el("td", { class: "num", text: number.format(i.active_7d) }),
        el("td", { class: "num", text: number.format(i.requests_24h) }),
        el("td", { class: "num", text: number.format(i.errors_24h) }),
        el("td", { text: dateTime.format(i.last_seen) }),
        el("td", {}, remove),
      );
    }),
  );
  if (s.instances.length === 0) document.getElementById("instances").append(el("tr", {}, el("td", { colspan: "12", class: "muted", text: "Жоден інстанс ще не пінгував." })));
}

async function removeInstance(id) {
  try {
    await api(`/v1/admin/instances/${id}`, { method: "DELETE" });
    await loadTelemetry();
  } catch (e) {
    showError(e);
  }
}

async function loadTelemetry() {
  const s = await api("/v1/admin/telemetry");
  renderStats(s);
  document.getElementById("charts").replaceChildren(
    chart("Інстанси, що пінгували за день", s.trend, "instances"),
    chart("Активні користувачі (7 днів)", s.trend, "active7d"),
    chart("Запити за добу", s.trend, "requests24h"),
  );
  counts("by-version", s.byVersion, (r) => r.version, s.latest);
  counts("by-kind", s.byKind, (r) => r.kind);
  counts("by-krai", s.byKrai, (r) => r.krai);
  counts("by-country", s.byCountry, (r) => r.country);
  renderInstances(s);
}

let targets = [];

async function loadNotices() {
  const notices = await api("/v1/admin/incidents");
  const names = new Map(targets.map((t) => [t.id, t.name]));
  document.getElementById("admin-notices").replaceChildren(
    ...notices.map((n) => {
      const actions = [];
      if (n.resolvedAt === null) {
        const resolve = el("button", { class: "btn btn-secondary", type: "button", text: "Завершити" });
        resolve.addEventListener("click", () => {
          void act(() => api(`/v1/admin/incidents/${n.id}`, { method: "PATCH", body: JSON.stringify({ resolvedAt: Date.now() }) }));
        });
        actions.push(resolve);
      }
      const remove = el("button", { class: "btn btn-text", type: "button", text: "Видалити" });
      remove.addEventListener("click", () => {
        if (confirm(`Видалити «${n.title}»?`)) void act(() => api(`/v1/admin/incidents/${n.id}`, { method: "DELETE" }));
      });
      actions.push(remove);
      const where = n.targets.length ? n.targets.map((id) => names.get(id) ?? id).join(", ") : "усі сервіси";
      const state = n.resolvedAt ? ` – ${dateTime.format(n.resolvedAt)}` : n.startsAt > Date.now() ? ", заплановано" : ", триває";
      const when = `${dateTime.format(n.startsAt)}${state}`;
      return el("li", {}, el("span", { class: "tag", text: KIND_TEXT[n.kind] }), el("span", { class: "title", text: n.title }), el("span", { class: "meta", text: `${where} · ${when}` }), ...actions);
    }),
  );
}

async function act(fn) {
  try {
    showError(null);
    await fn();
    await loadNotices();
  } catch (e) {
    showError(e);
  }
}

document.getElementById("notice-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const startsAt = data.get("startsAt") ? new Date(data.get("startsAt")).getTime() : undefined;
  const payload = { kind: data.get("kind"), title: data.get("title"), body: data.get("body"), targets: data.getAll("target"), startsAt };
  void act(async () => {
    await api("/v1/admin/incidents", { method: "POST", body: JSON.stringify(payload) });
    form.reset();
  });
});

async function checkNow(button) {
  button.disabled = true;
  try {
    const { results } = await api("/v1/admin/check", { method: "POST" });
    const bad = results.filter((r) => r.state !== "up");
    alert(bad.length ? bad.map((r) => `${r.target}: ${r.state}${r.detail ? `, ${r.detail}` : ""}`).join("\n") : "Усі цілі відповідають.");
  } catch (e) {
    showError(e);
  } finally {
    button.disabled = false;
  }
}

document.getElementById("check-now").addEventListener("click", (event) => {
  void checkNow(event.currentTarget);
});

(async () => {
  try {
    const me = await api("/v1/admin/me");
    document.getElementById("who").textContent = `Увійшли як ${me.email}. Тут лише анонімні цифри: жодних імен, пошт чи адрес.`;
    targets = (await api("/v1/status")).targets;
    document.getElementById("targets-pick").append(...targets.map((t) => el("label", {}, el("input", { type: "checkbox", name: "target", value: t.id }), t.name)));
    await Promise.all([loadTelemetry(), loadNotices()]);
  } catch (e) {
    document.getElementById("who").textContent = "";
    showError(e);
  }
})();
