# Gestión Académica — Arquitectura Hexagonal

Proyecto simplificado que implementa **Arquitectura Hexagonal (Ports & Adapters)**
siguiendo exactamente la estructura de tres capas.

---

## Estructura del proyecto

```
GestionAcademica/
│
├── Domain/                          ← A. Capa de Dominio (El núcleo)
│   ├── Entities/                    ← Objetos con identidad
│   │   ├── Alumno.cs
│   │   ├── Asignatura.cs
│   │   └── Matricula.cs
│   ├── ValueObjects/                ← Objetos inmutables
│   │   ├── Direccion.cs
│   │   └── Periodo.cs
│   ├── Services/                    ← Lógica que involucra varias entidades
│   │   └── ServicioMatriculacion.cs
│   └── Ports/                       ← Interfaces para la infraestructura
│       ├── IAlumnoRepository.cs
│       └── IRepositories.cs
│
├── Application/                     ← B. Capa de Aplicación (Casos de uso)
│   ├── UseCases/                    ← Comandos y consultas
│   │   ├── MatricularAlumnoHandler.cs
│   │   └── AlumnoUseCases.cs
│   └── DTOs/                        ← Objetos de transferencia entrada/salida
│       └── Dtos.cs
│
└── Infrastructure/                  ← C. Capa de Infraestructura
    └── Adapters/
        ├── Persistence/             ← Implementaciones de bases de datos
        │   ├── AcademiaDbContext.cs
        │   └── Repositories.cs
        ├── ExternalServices/        ← Clientes para APIs externas o modelos IA
        │   └── NotificacionEmailService.cs
        └── Web/                     ← Controladores API o rutas
            ├── AlumnosController.cs
            └── AsignaturasController.cs
```

---

## Ejecución

```bash
dotnet run
```

Swagger disponible en: **http://localhost:5000**

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/alumnos` | Lista todos los alumnos activos |
| GET | `/api/alumnos/{id}` | Obtiene un alumno por ID |
| POST | `/api/alumnos` | Crea un nuevo alumno |
| GET | `/api/alumnos/{id}/matriculas` | Matrículas de un alumno |
| GET | `/api/asignaturas` | Lista todas las asignaturas |
| POST | `/api/matriculas` | Matricula un alumno en una asignatura |
| GET | `/arquitectura` | Descripción de la arquitectura |

---

## Reglas de dependencia

```
Infrastructure → Application → Domain
                                  ↑
                    Nada depende de aquí hacia afuera
```

- **Domain** no importa nada externo (sin EF Core, sin ASP.NET)
- **Application** solo conoce los Ports (interfaces) del Domain
- **Infrastructure** implementa los Ports y depende de librerías externas
