FROM node:22-alpine AS build
WORKDIR /app

COPY LeadManager/package.json LeadManager/pnpm-lock.yaml ./
RUN corepack enable && pnpm install --frozen-lockfile

COPY LeadManager/. .
RUN pnpm build

FROM nginx:1.27-alpine AS final
COPY LeadManager/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
