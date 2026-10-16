// Light/dark switch shared by every status page. Without a saved choice the page follows the
// system; a click saves the opposite of what is shown now. Loaded in <head> (not deferred) so
// the saved theme applies before the first paint.

const KEY = "status-theme";
const root = document.documentElement;
const systemDark = () => matchMedia("(prefers-color-scheme: dark)").matches;
const read = () => {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
};
const isDark = () => (root.dataset.theme ? root.dataset.theme === "dark" : systemDark());

const saved = read();
if (saved === "dark" || saved === "light") root.dataset.theme = saved;

function label(button) {
  button.textContent = isDark() ? "Світла тема" : "Темна тема";
  button.setAttribute("aria-pressed", String(isDark()));
}

document.addEventListener("DOMContentLoaded", () => {
  for (const button of document.querySelectorAll("[data-theme-toggle]")) {
    label(button);
    button.addEventListener("click", () => {
      const next = isDark() ? "light" : "dark";
      root.dataset.theme = next;
      try {
        localStorage.setItem(KEY, next);
      } catch {}
      document.querySelectorAll("[data-theme-toggle]").forEach(label);
    });
  }
  matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => document.querySelectorAll("[data-theme-toggle]").forEach(label));
});
