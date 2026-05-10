# Minikube — Flujo de Trabajo Completo

> Guía paso a paso para arrancar, desplegar y gestionar
> una aplicación en un clúster Minikube local.

---

## Paso 1 — Arrancar Minikube

```bash
# Iniciar el clúster con recursos suficientes
minikube start --cpus=4 --memory=4096
```

---

## Paso 2 — Habilitar addons necesarios

```bash
# Ingress controller (NGINX) para exponer servicios HTTP/HTTPS
minikube addons enable ingress

# Servidor de métricas (necesario para kubectl top y HPA)
minikube addons enable metrics-server

# Dashboard web de Kubernetes
minikube addons enable dashboard
```

---

## Paso 3 — Apuntar Docker al daemon de Minikube

```bash
# Las imágenes que se construyan quedarán dentro de Minikube
# sin necesidad de subirlas a ningún registry externo
eval $(minikube docker-env)
```

> ⚠️ Este comando solo afecta a la terminal actual.
> Hay que ejecutarlo de nuevo en cada nueva sesión o terminal.

---

## Paso 4 — Construir la imagen localmente

```bash
# Construir la imagen dentro del daemon de Minikube
docker build -t mi-api:1.0.0 .
```

---

## Paso 5 — Desplegar la aplicación

```bash
# Aplicar todos los manifiestos YAML de la carpeta k8s/
kubectl apply -f k8s/
```

---

## Paso 6 — Verificar el despliegue

```bash
# Listar los Pods con actualización en tiempo real
kubectl get pods -w
```

---

## Paso 7 — Ver la URL del servicio

```bash
# Obtener la URL para acceder al servicio desde el navegador
minikube service mi-api-service --url
```

---

## Paso 8 — Escalar la aplicación

```bash
# Aumentar el número de réplicas del Deployment
kubectl scale deployment mi-api --replicas=3
```

---

## Paso 9 — Ver métricas

```bash
# Consultar el uso de CPU y memoria de los Pods
kubectl top pods --namespace ingress-nginx # indicar namespace
```

---

## Paso 10 — Abrir el Dashboard

```bash
# Inspección visual del clúster en el navegador
minikube dashboard
```

---

## Paso 11 — Detener el clúster

```bash
# Apagar el clúster conservando todos los datos y el estado
minikube stop
```

---

## 📋 Resumen del flujo

| Paso | Comando | Descripción |
|---|---|---|
| 1 | `minikube start --cpus=4 --memory=4096` | Arranca el clúster con recursos |
| 2 | `minikube addons enable <addon>` | Activa ingress, metrics-server y dashboard |
| 3 | `eval $(minikube docker-env)` | Apunta Docker al daemon interno |
| 4 | `docker build -t mi-api:1.0.0 .` | Construye la imagen en Minikube |
| 5 | `kubectl apply -f k8s/` | Despliega los manifiestos |
| 6 | `kubectl get pods -w` | Verifica el estado de los Pods |
| 7 | `minikube service mi-api-service --url` | Obtiene la URL del servicio |
| 8 | `kubectl scale deployment mi-api --replicas=3` | Escala la aplicación |
| 9 | `kubectl top pods` | Consulta métricas de uso |
| 10 | `minikube dashboard` | Abre el panel visual |
| 11 | `minikube stop` | Para el clúster sin perder datos |

---

*Generado para Minikube on-premise · versión ≥ 1.32*
