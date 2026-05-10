# app-demo — Despliegue de Nginx en Kubernetes

Guía completa del fichero `ngnix_deployment.yml` y cómo desplegarlo en un clúster Kubernetes con Minikube.

---

## Contenido del fichero

El fichero `ngnix_deployment.yml` define **dos recursos** de Kubernetes separados por `---`:

1. Un **Deployment** — gestiona los pods con Nginx
2. Un **Service** — expone esos pods al exterior

```
ngnix_deployment.yml
├── Deployment  →  crea y mantiene 2 pods con nginx:alpine
└── Service     →  expone el puerto 80 de esos pods vía NodePort
```

---

## Bloque 1 — Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
```

| Campo | Valor | Significado |
|---|---|---|
| `apiVersion` | `apps/v1` | Versión de la API de Kubernetes que gestiona Deployments |
| `kind` | `Deployment` | Tipo de recurso: gestiona réplicas de pods y sus actualizaciones |

---

```yaml
metadata:
  name: app-demo
  labels:
    app: app-demo
```

| Campo | Valor | Significado |
|---|---|---|
| `name` | `app-demo` | Nombre único del Deployment dentro del namespace |
| `labels.app` | `app-demo` | Etiqueta para identificar y agrupar este recurso |

---

```yaml
spec:
  replicas: 2
```

| Campo | Valor | Significado |
|---|---|---|
| `replicas` | `2` | Kubernetes mantendrá siempre **2 pods** en ejecución. Si uno cae, lo recrea automáticamente |

---

```yaml
  selector:
    matchLabels:
      app: app-demo
```

El Deployment usa este selector para saber **qué pods debe gestionar**. Solo controlará los pods que tengan la etiqueta `app: app-demo`. Debe coincidir exactamente con las etiquetas del `template`.

---

```yaml
  template:
    metadata:
      labels:
        app: app-demo
```

La **plantilla** de pod que Kubernetes usará para crear cada réplica. Las etiquetas aquí deben coincidir con el `selector` de arriba para que el Deployment los reconozca como propios.

---

```yaml
    spec:
      containers:
        - name: app-demo
          image: nginx:alpine
          ports:
            - containerPort: 80
```

| Campo | Valor | Significado |
|---|---|---|
| `name` | `app-demo` | Nombre del contenedor dentro del pod |
| `image` | `nginx:alpine` | Imagen Docker a usar — Nginx sobre Alpine Linux (imagen ligera, ~20 MB) |
| `containerPort` | `80` | Puerto que expone el contenedor internamente (el que usa Nginx por defecto) |

---

```yaml
          resources:
            requests:
              cpu: "100m"
              memory: "64Mi"
            limits:
              cpu: "200m"
              memory: "128Mi"
```

Los **recursos** controlan cuánta CPU y memoria puede usar cada pod:

| Campo | Valor | Significado |
|---|---|---|
| `requests.cpu` | `100m` | Mínimo garantizado de CPU — 100 milicores = 0.1 núcleos |
| `requests.memory` | `64Mi` | Mínimo garantizado de memoria — 64 Mebibytes |
| `limits.cpu` | `200m` | Máximo de CPU que puede usar — 200 milicores = 0.2 núcleos |
| `limits.memory` | `128Mi` | Máximo de memoria — si lo supera, el pod es reiniciado (OOMKilled) |

> **Requests** → lo que Kubernetes reserva para el pod al asignarlo a un nodo.  
> **Limits** → el techo que nunca puede superar.

---

## Bloque 2 — Service

```yaml
apiVersion: v1
kind: Service
```

| Campo | Valor | Significado |
|---|---|---|
| `apiVersion` | `v1` | API core de Kubernetes (Services, Pods, etc.) |
| `kind` | `Service` | Tipo de recurso: proporciona una IP/DNS estable y balancea tráfico entre pods |

---

```yaml
metadata:
  name: app-demo-service
```

| Campo | Valor | Significado |
|---|---|---|
| `name` | `app-demo-service` | Nombre del Service. También es su hostname DNS interno: `app-demo-service.default.svc.cluster.local` |

---

```yaml
spec:
  selector:
    app: app-demo
```

El Service enruta el tráfico a todos los pods que tengan la etiqueta `app: app-demo`. Si hay 2 réplicas, **balancea la carga** entre ellas automáticamente.

---

```yaml
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
```

| Campo | Valor | Significado |
|---|---|---|
| `protocol` | `TCP` | Protocolo de red (TCP es el estándar para HTTP) |
| `port` | `80` | Puerto por el que se accede **al Service** desde dentro del clúster |
| `targetPort` | `80` | Puerto al que el Service reenvía el tráfico **dentro de cada pod** |

---

```yaml
  type: NodePort
```

| Tipo | Acceso | Uso típico |
|---|---|---|
| `ClusterIP` | Solo dentro del clúster | Microservicios internos |
| **`NodePort`** | **Desde fuera, vía IP del nodo + puerto alto (30000-32767)** | **Desarrollo local / Minikube** |
| `LoadBalancer` | Desde fuera, vía IP pública | Producción en cloud |

`NodePort` asigna automáticamente un puerto en el rango 30000–32767 en el nodo, redirigiendo el tráfico al Service interno.

---

## Diagrama de flujo

```
  Navegador / curl
       │
       │  http://<minikube-ip>:<NodePort>
       ▼
┌─────────────────────┐
│  Service NodePort   │  app-demo-service (puerto 80 interno)
│  Balanceo de carga  │
└────────┬────────────┘
         │
    ┌────┴────┐
    │         │
┌───▼───┐ ┌───▼───┐
│ Pod 1 │ │ Pod 2 │   nginx:alpine — containerPort 80
└───────┘ └───────┘
    Deployment app-demo (replicas: 2)
```

---

## Despliegue en Kubernetes con Minikube

### Requisitos previos

| Herramienta | Instalación |
|---|---|
| Docker | https://docs.docker.com/get-docker |
| Minikube | https://minikube.sigs.k8s.io/docs/start |
| kubectl | https://kubernetes.io/docs/tasks/tools |

---

### Paso 1 — Iniciar Minikube

```bash
minikube start --driver=docker

# Verificar que el clúster está listo
minikube status
kubectl cluster-info
```

---

### Paso 2 — Aplicar el fichero de despliegue

```bash
kubectl apply -f ngnix_deployment.yml
```

Salida esperada:
```
deployment.apps/app-demo created
service/app-demo-service created
```

---

### Paso 3 — Verificar que los pods están Running

```bash
kubectl get all
```

Salida esperada:
```
NAME                            READY   STATUS    RESTARTS   AGE
pod/app-demo-6d9f8b7c4-abc12    1/1     Running   0          20s
pod/app-demo-6d9f8b7c4-xyz99    1/1     Running   0          20s

NAME                       TYPE       CLUSTER-IP      PORT(S)        AGE
service/app-demo-service   NodePort   10.96.145.201   80:31234/TCP   20s

NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
deployment.apps/app-demo   2/2     2            2           20s
```

> El número después de los dos puntos en `80:31234/TCP` es el NodePort asignado automáticamente.

---

### Paso 4 — Acceder a la aplicación

```bash
# Minikube abre el servicio y devuelve la URL directamente
minikube service app-demo-service --url
```

Salida ejemplo:
```
http://192.168.49.2:31234
```

```bash
# Abrir en el navegador automáticamente
minikube service app-demo-service
```

O manualmente con curl:
```bash
curl $(minikube service app-demo-service --url)
```

Deberías ver la página de bienvenida de Nginx:
```html
<!DOCTYPE html>
<html>
<head><title>Welcome to nginx!</title></head>
...
```

---

## Comandos útiles

```bash
# Ver los pods con su nodo asignado
kubectl get pods -o wide

# Ver los logs de Nginx de un pod
kubectl logs -l app=app-demo

# Describir el Deployment (eventos, estado, imagen)
kubectl describe deployment app-demo

# Describir el Service (ver el NodePort asignado)
kubectl describe service app-demo-service

# Ver los eventos del clúster
kubectl get events --sort-by='.lastTimestamp'
```

---

## Escalar las réplicas

```bash
# Aumentar a 4 réplicas
kubectl scale deployment app-demo --replicas=4
kubectl get pods -w   # ver cómo se crean en tiempo real

# Volver a 2
kubectl scale deployment app-demo --replicas=2
```

---

## Actualizar la imagen (rolling update)

```bash
# Cambiar de nginx:alpine a una versión específica
kubectl set image deployment/app-demo app-demo=nginx:1.27-alpine

# Seguir el estado del rollout
kubectl rollout status deployment/app-demo

# Revertir si algo va mal
kubectl rollout undo deployment/app-demo
```

---

## Eliminar los recursos

```bash
# Eliminar todo lo creado por el fichero
kubectl delete -f ngnix_deployment.yml

# Parar Minikube
minikube stop

# Eliminar el clúster completamente
minikube delete
```
