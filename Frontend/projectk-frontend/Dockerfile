FROM node:22-alpine AS build
# Angular build configuration: production | staging | development | tailscale.
# apiUrl is still overridable at runtime via env.js, but production flag,
# codeName and tier are baked per configuration at build time.
ARG NG_CONFIG=production
WORKDIR /src/Frontend/projectk-frontend

COPY Frontend/projectk-frontend/package*.json ./
RUN npm ci

COPY Frontend/projectk-frontend/ ./
RUN npm run build -- --configuration "${NG_CONFIG}"

FROM nginx:1.27-alpine AS runtime
ENV PROJECTK_API_URL=http://localhost:5205/api
COPY deploy/nginx/projectk-frontend.conf /etc/nginx/conf.d/default.conf
COPY deploy/nginx/40-projectk-env.sh /docker-entrypoint.d/40-projectk-env.sh
RUN chmod +x /docker-entrypoint.d/40-projectk-env.sh
COPY --from=build /src/Frontend/projectk-frontend/dist/projectk-frontend/browser /usr/share/nginx/html
COPY Frontend/projectk-frontend/public/env.template.js /usr/share/nginx/html/env.template.js
EXPOSE 80
