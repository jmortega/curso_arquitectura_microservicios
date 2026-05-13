# Checklist de Diagnóstico de Escalado en Kubernetes

> Guía paso a paso para detectar, identificar, clasificar,
> resolver y prevenir problemas de escalado en microservicios.

---

## Paso 1 — Detectar el síntoma

```bash
# ¿Hay Pods no Ready o en estado de error?
kubectl get pods -n <name_space>

# ¿Hay nodos al 80%+ de CPU o RAM?
kubectl top nodes

# ¿Hay Pods al límite de recursos?
kubectl top pods -n <name_space>

# ¿Está el HPA en maxReplicas?
kubectl get hpa -n <name_space>
```

---

## Paso 2 — Identificar la causa

```bash
# Ver Events y Last State del Pod con problemas
kubectl describe pod <pod> -n <name_space>

# Ver los logs antes del último reinicio
kubectl logs <pod> --previous

# Ver eventos de Warning recientes ordenados por tiempo
kubectl get events --sort-by=time -n <name_space>

# Ver los recursos disponibles en un nodo concreto
kubectl describe node <node>
```

---

## Paso 3 — Clasificar el problema

| Síntoma | Causa probable | Acción inmediata |
|---|---|---|
| `OOMKilled` | Falta de memoria | Subir `limits` o corregir leak |
| `Pending` | Falta de CPU/RAM en el clúster | Añadir nodos |
| `CrashLoopBackOff` | Error de la aplicación | Revisar logs |
| HPA en `maxReplicas` | Techo de escalado alcanzado | Aumentar `maxReplicas` o recursos |
| Latencia alta | Dependencias lentas o cuello de botella | Revisar trazas y dependencias |
| Errores `5xx` | Fallo en la app o saturación | Revisar logs y Circuit Breakers |

---

## Paso 4 — Aplicar solución

- [ ] Ajustar `requests` y `limits` del Deployment
- [ ] Ajustar `initialDelaySeconds` de los probes
- [ ] Aumentar `maxReplicas` del HPA
- [ ] Añadir nodos al clúster (Cluster Autoscaler)
- [ ] Aumentar `stabilizationWindowSeconds` del HPA
- [ ] Corregir el código (leak de memoria, timeout corto)

---

## Paso 5 — Prevenir la recurrencia

- [ ] Crear alerta en Prometheus / Grafana
- [ ] Documentar el incidente y la solución aplicada
- [ ] Revisar los SLO / SLI del servicio afectado
- [ ] Añadir test de carga al pipeline de CI/CD

---

*Referencia para equipos DevOps · Kubernetes on-premise y cloud*
