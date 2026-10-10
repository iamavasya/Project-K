# Лілейка Mobile

Ionic 9 + Angular 22 (zoneless) + Capacitor 8. План: [PLAN.md](PLAN.md).

Статус: спайк S1 (hello-оболонка з двома табами, Android-платформа, live reload).

## Вимоги

- Node.js **22.22.3+** або **24.15+** (Angular CLI 22 нижчі версії не запускає).
- Android Studio (з Android SDK 36 і емулятором), JDK 21 (іде з Android Studio).

## Команди

```bash
npm ci
npm start                 # ng serve на http://localhost:4200
npm test                  # vitest
npm run build             # прод-збірка у www/
npm run sync              # build + cap sync (копіює www/ у native-проєкти)
```

У браузері режим Ionic можна примусити: `http://localhost:4200/?ionic:mode=ios` або `?ionic:mode=md`.

## Android-емулятор з live reload (Windows)

Два термінали в `mobile/`:

```powershell
# 1. dev-сервер
npm start

# 2. емулятор має бути запущений (Android Studio → Device Manager)
npx cap run android -l --host 10.0.2.2 --port 4200
```

`10.0.2.2` — це `localhost` хост-машини зсередини емулятора. `cap run -l` сам підставляє
`server.url` і cleartext, а на Ctrl+C повертає конфіг назад. Збережи файл у `src/` — екран
на емуляторі оновиться.

Відкрити проєкт в Android Studio: `npx cap open android`.

Dev-збірка (`npm start`) ходить на бекенд `http://10.0.2.2:5205/api`, прод — у хмару.
