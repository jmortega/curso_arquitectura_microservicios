Ficheros nuevos:

k8s/tracing/jaeger.yaml — Deployment + Service de Jaeger en Kubernetes
k8s/tracing/zipkin.yaml — Deployment + Service de Zipkin en Kubernetes

Ficheros modificados:

Program.cs — se añade EnrichWithIDbCommand al instrumentador de EF Core para escribir peer.service = "postgres-write" en cada span de query SQL. Sin este tag, Jaeger sabe que hay SQL pero no dibuja la flecha. Lo mismo para HttpClient hacia MongoDB.

AcademyManager.API.csproj — nuevo paquete MongoDB.Driver.Core.Extensions.DiagnosticSources que intercepta el driver a nivel de red y emite Activity por cada operación MongoDB. También se añade AddSource(...) en Program.cs para que OpenTelemetry las recoja.

AcademyManager.Infrastructure.csproj — mismo paquete añadido aquí porque DependencyInjection.cs vive en Infrastructure y es donde se configura el MongoClient.

DependencyInjection.cs — el MongoDbContext ya no se registra como AddSingleton<MongoDbContext>() simple, sino que se construye manualmente pasando un MongoClientSettings que lleva el DiagnosticsActivityEventSubscriber registrado en el ClusterConfigurator. Esto es lo que activa la instrumentación real del driver.

MongoDbContext.cs — se añade un segundo constructor que acepta MongoClientSettings ya configurado, dejando intacto el constructor original por compatibilidad.

docker-compose.yml — Añadidos servicios jaeger y zipkin + variables de entorno de tracing en la API + node-exporter en red bridge

src/AcademyManager.API/Program.cs — Configuración completa de OpenTelemetry con selección dinámica de exporter Jaeger/Zipkin

src/AcademyManager.API/AcademyManager.API.csproj — 5 paquetes NuGet de OpenTelemetry añadidos

k8s/api/deployment.yaml — Variables Tracing__* + initContainer wait-for-jaeger

monitoring/prometheus.yml — Jobs actualizados con node-exporter por nombre de servicio
