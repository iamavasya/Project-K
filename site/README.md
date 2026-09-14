# Сайт Лілейки

Візитка і довідка на [Astro](https://astro.build) + [Starlight](https://starlight.astro.build).

- Довідка **не пишеться тут**: джерело — `../docs/user/*.md` (формат Starlight: frontmatter з `title`,
  картинки поруч у `images/`). Скрипт `scripts/sync-docs.mjs` копіює їх у `src/content/docs/user/`
  перед `build`, а в `npm run dev` ще й стежить за `docs/` і кореневими документами: збережений
  файл зʼявляється на сторінці за мить. Ця тека генерується і не комітиться.
- Головна сторінка — `src/content/docs/index.mdx`.
- Стиль — `src/styles/brand.css`, токени з `BRANDBOOK.md` і `lileyka-theme.css` застосунку.

```bash
npm install
npm run dev      # http://localhost:4321, з живим оновленням з docs/
npm run build    # dist/
```

## Демо

`/demo/` — статична збірка застосунку (`ng build --configuration demo`), у якій замість API працює
`DemoApiInterceptor` із записаними відповідями. Фікстури лежать у
`Frontend/projectk-frontend/public/assets/demo/*.json` і перезаписуються з docker-стеку `demo`
командою `node scripts/record-demo-fixtures.mjs` (з теки фронтенду). Збірка сайту разом із демо —
`bash scripts/build-site.sh` з кореня репозиторію; результат у `dist/`. Для Cloudflare Pages: build
command `bash scripts/build-site.sh`, output `site/dist`, версія Node з `.node-version` у корені
(без змінної `NODE_VERSION`); `public/_redirects`
веде глибокі посилання `/demo/*` на оболонку застосунку.
