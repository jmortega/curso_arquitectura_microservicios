
Docker Compose los scripts se montan como volúmenes locales, pero en K8s los manifiestos no los montan en ningún sitio.
La solución es crear ConfigMaps con el contenido de los scripts y montarlos en el directorio docker-entrypoint-initdb.d de cada contenedor.

En K8s no hay acceso al sistema de ficheros del host desde un Deployment. La solución es guardar el contenido de los scripts en ConfigMaps y montarlos como volúmenes. Ambos motores de base de datos ejecutan automáticamente cualquier fichero .sql o .js que encuentren en /docker-entrypoint-initdb.d/ durante la primera inicialización, igual que con Docker Compose, pero ahora el origen es el ConfigMap en lugar del disco local.


# Borrar los PVCs existentes — los scripts solo se ejecutan
# la primera vez que el contenedor inicializa el directorio de datos
kubectl delete namespace academy
kubectl apply -f k8s/namespace.yaml

# 1. Crear los ConfigMaps con el contenido de los scripts
kubectl apply -f k8s/postgres/postgres-init-configmap.yaml
kubectl apply -f k8s/mongodb/mongo-init-configmap.yaml

# 2. Desplegar las bases de datos (ahora montan los ConfigMaps)
kubectl apply -f k8s/postgres/postgres.yaml
kubectl apply -f k8s/mongodb/mongodb.yaml

# 3. Verificar que los datos se cargaron
kubectl wait --for=condition=ready pod -l app=postgres-write -n academy --timeout=120s

kubectl exec -n academy \
  $(kubectl get pod -n academy -l app=postgres-write -o jsonpath='{.items[0].metadata.name}') \
  -- psql -U academy -d academy_write \
  -c "SELECT COUNT(*) FROM students; SELECT COUNT(*) FROM subjects; SELECT COUNT(*) FROM enrollments;"

# 4. Aplicar el resto
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/api/
