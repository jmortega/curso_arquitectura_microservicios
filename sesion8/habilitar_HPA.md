#Crear HorizontalPodAutoscaler(HPA) en el fichero de deplyment.yml

apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: todo-api-hpa
  namespace: todo-app
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: todo-api
  minReplicas: 2
  maxReplicas: 6
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70

# Opción 2 — con kubectl directamente

kubectl autoscale deployment todo-api \
  --min=2 --max=6 --cpu-percent=70 \
  -n todo-app

#Verificar
kubectl get hpa -n todo-app

# Ver métricas actuales (requiere metrics-server)
kubectl describe hpa todo-api-hpa -n todo-app

Como regla general, para ver recursos de todos los namespaces a la vez:

# Ver HPAs en todos los namespaces
kubectl get hpa --all-namespaces

# Ver absolutamente todo en todo-app
kubectl get all -n todo-app
