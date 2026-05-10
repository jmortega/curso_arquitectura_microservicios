# Kubectl — Comandos Esenciales

> Referencia rápida de los comandos `kubectl` más utilizados en el día a día
> con Kubernetes on-premise.

---

## 📡 Contexto y clúster

```bash
# Información general del clúster
kubectl cluster-info

# Listar todos los nodos
kubectl get nodes

# Listar nodos con detalle (IPs, OS, versión del runtime, etc.)
kubectl get nodes -o wide
```

---

## 📄 Aplicar configuración

```bash
# Aplicar un fichero de manifiesto
kubectl apply -f deployment.yaml

# Aplicar todos los manifiestos de una carpeta
kubectl apply -f ./manifests/

# Eliminar los recursos definidos en un manifiesto
kubectl delete -f deployment.yaml
```

---

## 🐳 Pods

```bash
# Listar Pods en un namespace
kubectl get pods -n produccion

# Listar Pods con actualización en tiempo real (watch)
kubectl get pods -n produccion -w

# Ver detalle completo de un Pod (eventos, condiciones, etc.)
kubectl describe pod <nombre> -n produccion

# Ver los logs de un Pod
kubectl logs <nombre-pod> -n produccion

# Seguir los logs en tiempo real
kubectl logs <nombre-pod> -n produccion --follow

# Ver los logs del Pod anterior (útil si ha reiniciado)
kubectl logs <nombre-pod> -n produccion --previous
```

---

## 💻 Ejecutar comandos en un Pod

```bash
# Abrir una terminal interactiva dentro del contenedor
kubectl exec -it <nombre-pod> -n produccion -- /bin/bash

# Ver las variables de entorno del contenedor
kubectl exec <nombre-pod> -- env
```

---

## 🚀 Deployments

```bash
# Listar Deployments en un namespace
kubectl get deployments -n produccion

# Ver el estado de un rollout en curso
kubectl rollout status deployment/mi-app -n produccion

# Ver el historial de versiones desplegadas
kubectl rollout history deployment/mi-app -n produccion

# Hacer rollback a la versión anterior
kubectl rollout undo deployment/mi-app -n produccion
```

---

## ⚖️ Escalar manualmente

```bash
# Cambiar el número de réplicas de un Deployment
kubectl scale deployment mi-app --replicas=5 -n produccion
```

---

## 🖼️ Actualizar imagen

```bash
# Actualizar la imagen de un contenedor dentro de un Deployment
kubectl set image deployment/mi-app mi-app=mi-app:2.0.0 -n produccion
```

---

## 🌐 Servicios e Ingress

```bash
# Listar Services en un namespace
kubectl get services -n produccion

# Listar recursos Ingress en un namespace
kubectl get ingress -n produccion
```

---

## 🔍 Depuración

```bash
# Ver eventos del namespace ordenados por fecha (más recientes al final)
kubectl get events -n produccion --sort-by='.lastTimestamp'

# Ver uso de CPU y memoria por nodo
kubectl top nodes

# Ver uso de CPU y memoria por Pod
kubectl top pods -n produccion
```

---

## 📌 Referencia rápida de flags comunes

| Flag | Descripción |
|---|---|
| `-n <namespace>` | Especifica el namespace |
| `-o wide` | Muestra columnas adicionales |
| `-o yaml` | Exporta el recurso en formato YAML |
| `-o json` | Exporta el recurso en formato JSON |
| `-w` / `--watch` | Actualiza la salida en tiempo real |
| `--follow` | Sigue los logs en tiempo real |
| `--all-namespaces` | Opera sobre todos los namespaces |
| `-it` | Modo interactivo con TTY (para `exec`) |
| `--previous` | Usa el contenedor anterior (para `logs`) |

---

*Generado para Kubernetes on-premise · versión kubectl ≥ 1.28*
