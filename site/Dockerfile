# The docs site (Astro + Starlight) as a static nginx image. Build context is the repository root:
# scripts/sync-docs.mjs pulls docs/, ARCHITECTURE.md, CONTRIBUTING.md, BRANDBOOK.md and SECURITY.md
# from there at build time, so the image carries whatever the checkout says.
FROM node:26-alpine AS build
WORKDIR /src/site

COPY site/package*.json ./
RUN npm ci

COPY site/ ./
COPY docs/ /src/docs/
COPY ARCHITECTURE.md CONTRIBUTING.md BRANDBOOK.md SECURITY.md /src/
RUN npm run build

FROM nginx:1.31-alpine AS runtime
COPY docker/nginx/projectk-docs.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/site/dist /usr/share/nginx/html
EXPOSE 80
