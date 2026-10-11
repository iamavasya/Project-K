FROM node:26-alpine AS build
# Angular build configuration: production | staging | development | tailscale.
# apiUrl is still overridable at runtime via env.js, but production flag,
# codeName and tier are baked per configuration at build time.
ARG NG_CONFIG=production
WORKDIR /src/Frontend/projectk-frontend

COPY Frontend/projectk-frontend/package*.json ./
RUN npm ci --ignore-scripts

COPY Frontend/projectk-frontend/ ./
RUN npm run build -- --configuration "${NG_CONFIG}"

# The phone PWA, served by the same nginx under /m/ (mobile/README.md). Always a production
# build: the API it talks to comes from the same runtime env.js as the web app.
FROM node:26-alpine AS mobile
WORKDIR /src/mobile
COPY mobile/package*.json ./
RUN npm ci --ignore-scripts
COPY mobile/ ./
RUN npm run build:pwa

FROM nginx:1.31-alpine AS runtime
ENV PROJECTK_API_URL=http://localhost:5205/api
COPY docker/nginx/projectk-frontend.conf /etc/nginx/conf.d/default.conf
COPY docker/nginx/40-projectk-env.sh /docker-entrypoint.d/40-projectk-env.sh
RUN chmod +x /docker-entrypoint.d/40-projectk-env.sh
COPY --from=build /src/Frontend/projectk-frontend/dist/projectk-frontend/browser /usr/share/nginx/html
COPY --from=mobile /src/mobile/www /usr/share/nginx/html/m
COPY Frontend/projectk-frontend/public/env.template.js /usr/share/nginx/html/env.template.js
EXPOSE 80
