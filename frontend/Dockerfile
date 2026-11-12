# Interfaz de EduMonitor: compila React con Vite y la sirve con nginx, que pasa /api a la API por la red interna.
# VITE_API_URL vacío: la app llama a /api en la misma dirección (http://localhost:5173).

FROM node:22-alpine AS compilacion
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ENV VITE_API_URL=
ENV VITE_USE_MOCK=false
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=compilacion /app/dist /usr/share/nginx/html
EXPOSE 80
