-- =============================================================================
--  Academy Manager — PostgreSQL initialization
--  Ejecutado automáticamente por el contenedor en el primer arranque.
--
--  Crea el esquema completo (igual que generaría EF Core) e inserta datos
--  de prueba, evitando que db.Database.Migrate() falle al encontrar
--  tablas ya existentes.
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ── Tabla de alumnos ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS students (
    id              UUID                     NOT NULL,
    first_name      VARCHAR(100)             NOT NULL,
    last_name       VARCHAR(100)             NOT NULL,
    email           VARCHAR(200)             NOT NULL,
    date_of_birth   TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at      TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_students" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_students_email" ON students (email);

-- ── Tabla de asignaturas ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS subjects (
    id           UUID                     NOT NULL,
    name         VARCHAR(200)             NOT NULL,
    code         VARCHAR(20)              NOT NULL,
    description  VARCHAR(1000)            NULL,
    credits      INTEGER                  NOT NULL,
    max_students INTEGER                  NOT NULL,
    created_at   TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT "PK_subjects" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_subjects_code" ON subjects (code);

-- ── Tabla de matrículas ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS enrollments (
    id           UUID                     NOT NULL,
    student_id   UUID                     NOT NULL,
    subject_id   UUID                     NOT NULL,
    status       VARCHAR                  NOT NULL,
    enrolled_at  TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_enrollments" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_enrollments_student_id_subject_id"
    ON enrollments (student_id, subject_id);

-- ── Historial de migraciones EF Core ─────────────────────────────────────────
-- Registra la migración como ya aplicada para que db.Database.Migrate()
-- no intente volver a crear las tablas al arrancar la API.
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    VARCHAR(150) NOT NULL,
    "ProductVersion" VARCHAR(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
--  DATOS DE PRUEBA
-- =============================================================================

-- ── Asignaturas ───────────────────────────────────────────────────────────────
INSERT INTO subjects (id, name, code, description, credits, max_students, created_at) VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'Fundamentos de Programación',
    'FP001',
    'Introducción a los conceptos básicos de programación: variables, estructuras de control, funciones y resolución de problemas algorítmicos.',
    6, 40,
    '2024-09-01 08:00:00+00'
),
(
    'a1000000-0000-0000-0000-000000000002',
    'Bases de Datos',
    'BD002',
    'Diseño relacional, SQL avanzado, normalización, índices y fundamentos de bases de datos NoSQL.',
    6, 35,
    '2024-09-01 08:00:00+00'
),
(
    'a1000000-0000-0000-0000-000000000003',
    'Arquitectura de Software',
    'AS003',
    'Patrones de diseño, arquitectura hexagonal, CQRS, microservicios y principios SOLID.',
    4, 30,
    '2024-09-01 08:00:00+00'
),
(
    'a1000000-0000-0000-0000-000000000004',
    'Desarrollo Web con .NET',
    'DW004',
    'ASP.NET Core, Minimal APIs, Entity Framework Core, autenticación JWT y despliegue con Docker.',
    6, 30,
    '2024-09-01 08:00:00+00'
),
(
    'a1000000-0000-0000-0000-000000000005',
    'DevOps y Kubernetes',
    'DK005',
    'CI/CD, contenedores Docker, orquestación con Kubernetes, Minikube y Helm charts.',
    4, 25,
    '2024-09-01 08:00:00+00'
),
(
    'a1000000-0000-0000-0000-000000000006',
    'Matemáticas Discretas',
    'MD006',
    'Lógica proposicional, teoría de grafos, combinatoria y álgebra booleana.',
    6, 50,
    '2024-09-01 08:00:00+00'
)
ON CONFLICT DO NOTHING;

-- ── Alumnos ───────────────────────────────────────────────────────────────────
INSERT INTO students (id, first_name, last_name, email, date_of_birth, created_at, updated_at) VALUES
(
    'b2000000-0000-0000-0000-000000000001',
    'Ana', 'García López',
    'ana.garcia@universidad.es',
    '2001-03-15 00:00:00+00',
    '2024-09-10 09:00:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000002',
    'Carlos', 'Martínez Ruiz',
    'carlos.martinez@universidad.es',
    '2000-07-22 00:00:00+00',
    '2024-09-10 09:05:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000003',
    'Laura', 'Fernández Sánchez',
    'laura.fernandez@universidad.es',
    '2001-11-08 00:00:00+00',
    '2024-09-10 09:10:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000004',
    'Miguel', 'López Torres',
    'miguel.lopez@universidad.es',
    '1999-05-30 00:00:00+00',
    '2024-09-11 10:00:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000005',
    'Sofía', 'Rodríguez Pérez',
    'sofia.rodriguez@universidad.es',
    '2002-01-17 00:00:00+00',
    '2024-09-11 10:15:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000006',
    'David', 'González Moreno',
    'david.gonzalez@universidad.es',
    '2000-09-03 00:00:00+00',
    '2024-09-12 08:30:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000007',
    'Elena', 'Jiménez Castro',
    'elena.jimenez@universidad.es',
    '2001-06-25 00:00:00+00',
    '2024-09-12 08:45:00+00', NULL
),
(
    'b2000000-0000-0000-0000-000000000008',
    'Pablo', 'Díaz Morales',
    'pablo.diaz@universidad.es',
    '2000-12-14 00:00:00+00',
    '2024-09-13 09:20:00+00', NULL
)
ON CONFLICT DO NOTHING;

-- ── Matrículas ────────────────────────────────────────────────────────────────
-- Estados posibles: Active, Completed, Cancelled
INSERT INTO enrollments (id, student_id, subject_id, status, enrolled_at, completed_at) VALUES

-- Ana García — completó 2, activa en 2
('c3000000-0000-0000-0000-000000000001', 'b2000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000001', 'Completed', '2024-09-15 10:00:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000002', 'b2000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000002', 'Completed', '2024-09-15 10:05:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000003', 'b2000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000003', 'Active',    '2025-02-01 09:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000004', 'b2000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000004', 'Active',    '2025-02-01 09:05:00+00', NULL),

-- Carlos Martínez — completó 1, activo en 2
('c3000000-0000-0000-0000-000000000005', 'b2000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000001', 'Completed', '2024-09-15 11:00:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000006', 'b2000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000004', 'Active',    '2025-02-01 11:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000007', 'b2000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000005', 'Active',    '2025-02-01 11:05:00+00', NULL),

-- Laura Fernández — activa en 2, canceló 1
('c3000000-0000-0000-0000-000000000008', 'b2000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000002', 'Active',    '2025-02-01 12:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000009', 'b2000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000003', 'Active',    '2025-02-01 12:05:00+00', NULL),
('c3000000-0000-0000-0000-000000000010', 'b2000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000006', 'Cancelled', '2024-09-15 12:00:00+00', NULL),

-- Miguel López — completó 3, activo en 1
('c3000000-0000-0000-0000-000000000011', 'b2000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000001', 'Completed', '2024-09-15 13:00:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000012', 'b2000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000002', 'Completed', '2024-09-15 13:05:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000013', 'b2000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000003', 'Completed', '2024-09-15 13:10:00+00', '2025-01-20 10:00:00+00'),
('c3000000-0000-0000-0000-000000000014', 'b2000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000005', 'Active',    '2025-02-01 13:00:00+00', NULL),

-- Sofía Rodríguez — recién incorporada, activa en 2
('c3000000-0000-0000-0000-000000000015', 'b2000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000001', 'Active',    '2025-02-01 14:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000016', 'b2000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000006', 'Active',    '2025-02-01 14:05:00+00', NULL),

-- David González — activo en 2, canceló 1
('c3000000-0000-0000-0000-000000000017', 'b2000000-0000-0000-0000-000000000006', 'a1000000-0000-0000-0000-000000000002', 'Active',    '2025-02-01 15:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000018', 'b2000000-0000-0000-0000-000000000006', 'a1000000-0000-0000-0000-000000000004', 'Active',    '2025-02-01 15:05:00+00', NULL),
('c3000000-0000-0000-0000-000000000019', 'b2000000-0000-0000-0000-000000000006', 'a1000000-0000-0000-0000-000000000005', 'Cancelled', '2024-09-15 15:00:00+00', NULL),

-- Elena Jiménez — activa en 3
('c3000000-0000-0000-0000-000000000020', 'b2000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000003', 'Active',    '2025-02-01 16:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000021', 'b2000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000004', 'Active',    '2025-02-01 16:05:00+00', NULL),
('c3000000-0000-0000-0000-000000000022', 'b2000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000006', 'Active',    '2025-02-01 16:10:00+00', NULL),

-- Pablo Díaz — activo en 2
('c3000000-0000-0000-0000-000000000023', 'b2000000-0000-0000-0000-000000000008', 'a1000000-0000-0000-0000-000000000001', 'Active',    '2025-02-01 17:00:00+00', NULL),
('c3000000-0000-0000-0000-000000000024', 'b2000000-0000-0000-0000-000000000008', 'a1000000-0000-0000-0000-000000000002', 'Active',    '2025-02-01 17:05:00+00', NULL)

ON CONFLICT DO NOTHING;

-- ── Resumen de datos insertados ───────────────────────────────────────────────
DO $$
BEGIN
    RAISE NOTICE '✓ Subjects    insertadas: %', (SELECT COUNT(*) FROM subjects);
    RAISE NOTICE '✓ Students    insertados: %', (SELECT COUNT(*) FROM students);
    RAISE NOTICE '✓ Enrollments insertadas: %', (SELECT COUNT(*) FROM enrollments);
END $$;

-- =============================================================================
--  MassTransit Transactional Outbox — tablas requeridas por el Outbox Pattern
--
--  Sin estas tablas el BusOutboxDeliveryService falla al arrancar con:
--    "relation OutboxState does not exist"
--
--  Estas tablas las crearía automáticamente db.Database.Migrate() si el
--  schema fuera nuevo, pero al existir ya la BD (por el init script) EF Core
--  no ejecuta EnsureCreated() sobre una BD existente. Por eso se crean aquí
--  junto con el resto del schema, en el primer arranque del contenedor.
--
--  Tablas:
--    OutboxMessage  → eventos de dominio pendientes de enviar a RabbitMQ
--    OutboxState    → estado del Worker de entrega (advisory lock por group)
--    InboxState     → deduplicación de mensajes recibidos (idempotencia)
-- =============================================================================

-- ── InboxState: evita procesar el mismo mensaje dos veces ─────────────────────
CREATE TABLE IF NOT EXISTS "InboxState" (
    "Id"                  BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    "MessageId"           UUID                     NOT NULL,
    "ConsumerId"          UUID                     NOT NULL,
    "LockId"              UUID                     NOT NULL,
    "RowVersion"          xid                      NULL,
    "Received"            TIMESTAMP WITH TIME ZONE NOT NULL,
    "ReceiveCount"        INTEGER                  NOT NULL,
    "ExpirationTime"      TIMESTAMP WITH TIME ZONE NULL,
    "Consumed"            TIMESTAMP WITH TIME ZONE NULL,
    "Delivered"           TIMESTAMP WITH TIME ZONE NULL,
    "LastSequenceNumber"  BIGINT                   NULL,
    CONSTRAINT "PK_InboxState"
        PRIMARY KEY ("Id"),
    CONSTRAINT "AK_InboxState_MessageId_ConsumerId"
        UNIQUE ("MessageId", "ConsumerId")
);

-- ── OutboxState: estado del Worker de entrega por lock group ──────────────────
CREATE TABLE IF NOT EXISTS "OutboxState" (
    "OutboxId"            UUID                     NOT NULL,
    "LockId"              UUID                     NOT NULL,
    "RowVersion"          xid                      NULL,
    "Created"             TIMESTAMP WITH TIME ZONE NOT NULL,
    "Delivered"           TIMESTAMP WITH TIME ZONE NULL,
    "LastSequenceNumber"  BIGINT                   NULL,
    CONSTRAINT "PK_OutboxState"
        PRIMARY KEY ("OutboxId")
);

-- ── OutboxMessage: eventos pendientes de publicar en RabbitMQ ─────────────────
CREATE TABLE IF NOT EXISTS "OutboxMessage" (
    "SequenceNumber"      BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    "EnqueueTime"         TIMESTAMP WITH TIME ZONE NULL,
    "SentTime"            TIMESTAMP WITH TIME ZONE NOT NULL,
    "Headers"             TEXT                     NULL,
    "Properties"          TEXT                     NULL,
    "InboxMessageId"      UUID                     NULL,
    "InboxConsumerId"     UUID                     NULL,
    "OutboxId"            UUID                     NULL,
    "MessageId"           UUID                     NOT NULL,
    "ContentType"         VARCHAR(256)             NOT NULL,
    "MessageType"         TEXT                     NOT NULL,
    "Body"                TEXT                     NOT NULL,
    "ConversationId"      UUID                     NULL,
    "CorrelationId"       UUID                     NULL,
    "InitiatorId"         UUID                     NULL,
    "RequestId"           UUID                     NULL,
    "SourceAddress"       VARCHAR(256)             NULL,
    "DestinationAddress"  VARCHAR(256)             NULL,
    "ResponseAddress"     VARCHAR(256)             NULL,
    "FaultAddress"        VARCHAR(256)             NULL,
    "ExpirationTime"      TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_OutboxMessage"
        PRIMARY KEY ("SequenceNumber")
);

-- Índice para que el Worker localice rápido los mensajes pendientes de entrega
CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_EnqueueTime"
    ON "OutboxMessage" ("EnqueueTime")
    WHERE "EnqueueTime" IS NOT NULL;

-- Índice para correlacionar mensajes con su OutboxState (lock group)
CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_OutboxId_SequenceNumber"
    ON "OutboxMessage" ("OutboxId", "SequenceNumber")
    WHERE "OutboxId" IS NOT NULL;

-- ── Registrar la migración del Outbox en el historial de EF Core ──────────────
-- Evita que db.Database.Migrate() intente crear las tablas de nuevo al arrancar.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240101000001_AddMassTransitOutbox', '8.0.0')
ON CONFLICT DO NOTHING;

-- ── Resumen final ─────────────────────────────────────────────────────────────
DO $$
BEGIN
    RAISE NOTICE '✓ InboxState   creada: %', (SELECT COUNT(*) FROM "InboxState");
    RAISE NOTICE '✓ OutboxState  creado: %', (SELECT COUNT(*) FROM "OutboxState");
    RAISE NOTICE '✓ OutboxMessage creado: %', (SELECT COUNT(*) FROM "OutboxMessage");
END $$;
