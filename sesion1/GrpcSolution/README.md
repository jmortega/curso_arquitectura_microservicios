# gRPC + Docker + Swagger — .NET 8

Solución con dos proyectos:

- **GrpcServer** — servidor gRPC que implementa el catálogo de productos
- **GrpcClient** — cliente REST con Swagger UI que llama al servidor vía gRPC

---

## Arquitectura

```
Navegador / Postman / curl
          │
          │  HTTP/1.1 + JSON
          ▼
┌─────────────────────────┐
│   GrpcClient            │  :5100
│   API REST + Swagger UI  │
└────────────┬────────────┘
             │
             │  gRPC (HTTP/2 + Protobuf)
             ▼
┌─────────────────────────┐
│   GrpcServer            │  :5200
│   CatalogoProductos     │
└─────────────────────────┘
```

---

## Estructura

```
GrpcSolution/
├── GrpcSolution.sln
├── docker-compose.yml
└── src/
    ├── GrpcServer/
    │   ├── GrpcServer.csproj
    │   ├── Dockerfile
    │   ├── Protos/
    │   │   └── catalogo.proto        ← contrato gRPC
    │   ├── Data/
    │   │   └── ProductoRepository.cs ← datos en memoria
    │   ├── Services/
    │   │   └── CatalogoProductosService.cs ← implementación gRPC
    │   └── Program.cs
    └── GrpcClient/
        ├── GrpcClient.csproj
        ├── Dockerfile
        ├── Protos/
        │   └── catalogo.proto        ← mismo contrato, compilado como cliente
        ├── Models/
        │   └── Dtos.cs               ← modelos para Swagger
        ├── Controllers/
        │   └── ProductosController.cs ← endpoints REST → gRPC
        └── Program.cs
```

---

## Comandos

### Con Docker (recomendado)

```bash
# Construir y arrancar ambos servicios
docker-compose up --build

# En segundo plano
docker-compose up --build -d

# Ver logs
docker-compose logs -f

# Detener
docker-compose down
```

**URLs tras arrancar:**
- Swagger UI:  http://localhost:5100
- gRPC Server: http://localhost:5200 (solo con cliente gRPC)

---

### Sin Docker (desarrollo local)

**Terminal 1 — Servidor:**
```bash
cd src/GrpcServer
dotnet run
# Escucha en: http://localhost:5200
```

**Terminal 2 — Cliente:**
```bash
cd src/GrpcClient
dotnet run
# Swagger en: http://localhost:5100
```

---

## Endpoints REST / Swagger

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/productos` | Listar productos (paginado + filtro categoría) |
| `GET` | `/api/productos/{id}` | Obtener un producto por ID |
| `POST` | `/api/productos` | Crear nuevo producto |
| `PUT` | `/api/productos/{id}` | Actualizar producto |
| `DELETE` | `/api/productos/{id}` | Eliminar producto |
| `GET` | `/api/productos/stream/precios?ids=1&ids=2` | Stream de precios (15s) |

---

## Servicio gRPC (catalogo.proto)

```protobuf
service CatalogoProductos {
  rpc ObtenerProducto   (ObtenerProductoRequest)   returns (ProductoResponse);
  rpc ListarProductos   (ListarProductosRequest)   returns (ListaProductosResponse);
  rpc CrearProducto     (CrearProductoRequest)     returns (ProductoResponse);
  rpc ActualizarProducto(ActualizarProductoRequest) returns (ProductoResponse);
  rpc EliminarProducto  (EliminarProductoRequest)  returns (EliminarProductoResponse);
  rpc EscucharPrecios   (EscucharPreciosRequest)   returns (stream PrecioActualizadoResponse);
}
```

---

## Probar con grpcurl (opcional)

```bash
# Listar servicios disponibles
grpcurl -plaintext localhost:5200 list

# Listar productos
grpcurl -plaintext -d '{"pagina": 1, "tamanio": 10}' \
  localhost:5200 catalogo.CatalogoProductos/ListarProductos

# Obtener un producto
grpcurl -plaintext -d '{"id": "1"}' \
  localhost:5200 catalogo.CatalogoProductos/ObtenerProducto

# Crear un producto
grpcurl -plaintext \
  -d '{"nombre":"Nuevo Producto","precio":99.99,"stock":10,"categoria":"Electrónica"}' \
  localhost:5200 catalogo.CatalogoProductos/CrearProducto
```

---

## Datos de prueba iniciales

| ID | Nombre | Precio | Categoría |
|---|---|---|---|
| 1 | Laptop Pro 15 | 1299.99€ | Electrónica |
| 2 | Teclado Mecánico | 89.99€ | Periféricos |
| 3 | Monitor 4K 27" | 449.99€ | Electrónica |
| 4 | Ratón Inalámbrico | 45.99€ | Periféricos |
| 5 | Auriculares Gaming | 79.99€ | Audio |
