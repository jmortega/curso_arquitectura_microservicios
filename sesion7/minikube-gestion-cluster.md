# Minikube — Gestión del Clúster

> Referencia rápida de comandos para controlar el ciclo de vida
> e inspeccionar un clúster Minikube local.

---

## ♻️ Ciclo de vida

```bash
# Pausar el clúster (guarda estado, libera recursos de CPU y RAM)
minikube pause

# Reanudar el clúster pausado
minikube unpause

# Detener el clúster (apaga la VM pero conserva los datos)
minikube stop

# Eliminar el clúster completamente
minikube delete

# Eliminar todos los perfiles y clústeres existentes
minikube delete --all
```

---

## 🔍 Información y diagnóstico

```bash
# Ver la IP del nodo de Minikube
minikube ip

# Acceder por SSH al nodo de Minikube
minikube ssh

# Ver los logs internos de Minikube
minikube logs

# Abrir el Dashboard web (métricas, Pods, Deployments, etc.)
minikube dashboard
```

---

## 📌 Referencia rápida — estados del clúster

| Comando | Estado resultante | Datos conservados |
|---|---|---|
| `minikube pause` | Pausado | ✅ Sí |
| `minikube unpause` | En ejecución | ✅ Sí |
| `minikube stop` | Detenido | ✅ Sí |
| `minikube delete` | Eliminado | ❌ No |
| `minikube delete --all` | Todos eliminados | ❌ No |

---

*Generado para Minikube on-premise · versión ≥ 1.32*
