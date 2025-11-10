FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

# Falls @angular/cli nicht als devDependency eingetragen ist
# stellen wir hier sicher, dass "ng" im Container verfügbar ist:
RUN npm install -g @angular/cli

COPY . .

RUN npm run build -- --configuration=production

FROM nginx:1.27-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/swen-dms-ui/browser /usr/share/nginx/html
