# Сайт Лілейки

Візитка і довідка на [Astro](https://astro.build) + [Starlight](https://starlight.astro.build).

- Довідка **не пишеться тут**: джерело — `../docs/user/*.md` (формат Starlight: frontmatter з `title`,
  картинки поруч у `images/`). Скрипт `scripts/sync-docs.mjs` копіює їх у `src/content/docs/user/`
  перед `dev` і `build`; ця тека генерується і не комітиться.
- Головна сторінка — `src/content/docs/index.mdx`.
- Стиль — `src/styles/brand.css`, токени з `BRANDBOOK.md` і `lileyka-theme.css` застосунку.

```bash
npm install
npm run dev      # http://localhost:4321
npm run build    # dist/
```
