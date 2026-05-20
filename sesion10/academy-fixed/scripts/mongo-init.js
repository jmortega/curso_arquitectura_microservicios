// =============================================================================
//  Academy Manager — MongoDB Read DB initialization
//
//  En CQRS, esta base de datos se alimenta normalmente de domain events
//  disparados desde el write side (PostgreSQL). Este script inserta los
//  read models desnormalizados correspondientes al seed de PostgreSQL,
//  necesario porque los datos de prueba se insertaron directamente en PG
//  sin pasar por los event handlers de la aplicación.
//
//  Los IDs coinciden exactamente con los del postgres-init.sql.
// =============================================================================

db = db.getSiblingDB('academy_read');

// ── Colecciones e índices ─────────────────────────────────────────────────────
db.createCollection('students');
db.createCollection('subjects');
db.createCollection('enrollments');

db.students.createIndex({ "email": 1 },    { unique: true });
db.students.createIndex({ "lastName": 1 });
db.subjects.createIndex({ "code": 1 },     { unique: true });
db.enrollments.createIndex({ "studentId": 1 });
db.enrollments.createIndex({ "subjectId": 1 });

// =============================================================================
//  SUBJECTS — read model desnormalizado
//  Incluye contador de alumnos matriculados (activos) por asignatura
// =============================================================================
db.subjects.insertMany([
  {
    _id: "a1000000-0000-0000-0000-000000000001",
    name: "Fundamentos de Programación",
    code: "FP001",
    description: "Introducción a los conceptos básicos de programación: variables, estructuras de control, funciones y resolución de problemas algorítmicos.",
    credits: 6,
    maxStudents: 40,
    enrolledStudents: 4,   // Ana, Carlos, Sofía, Pablo → Active
    createdAt: new Date("2024-09-01T08:00:00Z")
  },
  {
    _id: "a1000000-0000-0000-0000-000000000002",
    name: "Bases de Datos",
    code: "BD002",
    description: "Diseño relacional, SQL avanzado, normalización, índices y fundamentos de bases de datos NoSQL.",
    credits: 6,
    maxStudents: 35,
    enrolledStudents: 4,   // Laura, David, Elena (→ no, AS003), Pablo → Active
    createdAt: new Date("2024-09-01T08:00:00Z")
  },
  {
    _id: "a1000000-0000-0000-0000-000000000003",
    name: "Arquitectura de Software",
    code: "AS003",
    description: "Patrones de diseño, arquitectura hexagonal, CQRS, microservicios y principios SOLID.",
    credits: 4,
    maxStudents: 30,
    enrolledStudents: 3,   // Ana, Laura, Elena → Active
    createdAt: new Date("2024-09-01T08:00:00Z")
  },
  {
    _id: "a1000000-0000-0000-0000-000000000004",
    name: "Desarrollo Web con .NET",
    code: "DW004",
    description: "ASP.NET Core, Minimal APIs, Entity Framework Core, autenticación JWT y despliegue con Docker.",
    credits: 6,
    maxStudents: 30,
    enrolledStudents: 4,   // Ana, Carlos, David, Elena → Active
    createdAt: new Date("2024-09-01T08:00:00Z")
  },
  {
    _id: "a1000000-0000-0000-0000-000000000005",
    name: "DevOps y Kubernetes",
    code: "DK005",
    description: "CI/CD, contenedores Docker, orquestación con Kubernetes, Minikube y Helm charts.",
    credits: 4,
    maxStudents: 25,
    enrolledStudents: 2,   // Carlos, Miguel → Active
    createdAt: new Date("2024-09-01T08:00:00Z")
  },
  {
    _id: "a1000000-0000-0000-0000-000000000006",
    name: "Matemáticas Discretas",
    code: "MD006",
    description: "Lógica proposicional, teoría de grafos, combinatoria y álgebra booleana.",
    credits: 6,
    maxStudents: 50,
    enrolledStudents: 3,   // Sofía, Elena, Pablo → Active (Laura canceló)
    createdAt: new Date("2024-09-01T08:00:00Z")
  }
]);

// =============================================================================
//  STUDENTS — read model desnormalizado
//  Incluye el array de matrículas embebido (patrón de desnormalización CQRS)
// =============================================================================
db.students.insertMany([
  {
    _id: "b2000000-0000-0000-0000-000000000001",
    firstName:   "Ana",
    lastName:    "García López",
    fullName:    "Ana García López",
    email:       "ana.garcia@universidad.es",
    dateOfBirth: new Date("2001-03-15T00:00:00Z"),
    createdAt:   new Date("2024-09-10T09:00:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000001", subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T10:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000002", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Completed", enrolledAt: new Date("2024-09-15T10:05:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000003", subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Active",    enrolledAt: new Date("2025-02-01T09:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000004", subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T09:05:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000002",
    firstName:   "Carlos",
    lastName:    "Martínez Ruiz",
    fullName:    "Carlos Martínez Ruiz",
    email:       "carlos.martinez@universidad.es",
    dateOfBirth: new Date("2000-07-22T00:00:00Z"),
    createdAt:   new Date("2024-09-10T09:05:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000005", subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T11:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000006", subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T11:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000007", subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",          subjectCode: "DK005", status: "Active",    enrolledAt: new Date("2025-02-01T11:05:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000003",
    firstName:   "Laura",
    lastName:    "Fernández Sánchez",
    fullName:    "Laura Fernández Sánchez",
    email:       "laura.fernandez@universidad.es",
    dateOfBirth: new Date("2001-11-08T00:00:00Z"),
    createdAt:   new Date("2024-09-10T09:10:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000008", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",           subjectCode: "BD002", status: "Active",    enrolledAt: new Date("2025-02-01T12:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000009", subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software", subjectCode: "AS003", status: "Active",    enrolledAt: new Date("2025-02-01T12:05:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000010", subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",    subjectCode: "MD006", status: "Cancelled", enrolledAt: new Date("2024-09-15T12:00:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000004",
    firstName:   "Miguel",
    lastName:    "López Torres",
    fullName:    "Miguel López Torres",
    email:       "miguel.lopez@universidad.es",
    dateOfBirth: new Date("1999-05-30T00:00:00Z"),
    createdAt:   new Date("2024-09-11T10:00:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000011", subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T13:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000012", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Completed", enrolledAt: new Date("2024-09-15T13:05:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000013", subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Completed", enrolledAt: new Date("2024-09-15T13:10:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000014", subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",          subjectCode: "DK005", status: "Active",    enrolledAt: new Date("2025-02-01T13:00:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000005",
    firstName:   "Sofía",
    lastName:    "Rodríguez Pérez",
    fullName:    "Sofía Rodríguez Pérez",
    email:       "sofia.rodriguez@universidad.es",
    dateOfBirth: new Date("2002-01-17T00:00:00Z"),
    createdAt:   new Date("2024-09-11T10:15:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000015", subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Active", enrolledAt: new Date("2025-02-01T14:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000016", subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",        subjectCode: "MD006", status: "Active", enrolledAt: new Date("2025-02-01T14:05:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000006",
    firstName:   "David",
    lastName:    "González Moreno",
    fullName:    "David González Moreno",
    email:       "david.gonzalez@universidad.es",
    dateOfBirth: new Date("2000-09-03T00:00:00Z"),
    createdAt:   new Date("2024-09-12T08:30:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000017", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",          subjectCode: "BD002", status: "Active",    enrolledAt: new Date("2025-02-01T15:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000018", subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET", subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T15:05:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000019", subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",     subjectCode: "DK005", status: "Cancelled", enrolledAt: new Date("2024-09-15T15:00:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000007",
    firstName:   "Elena",
    lastName:    "Jiménez Castro",
    fullName:    "Elena Jiménez Castro",
    email:       "elena.jimenez@universidad.es",
    dateOfBirth: new Date("2001-06-25T00:00:00Z"),
    createdAt:   new Date("2024-09-12T08:45:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000020", subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",  subjectCode: "AS003", status: "Active", enrolledAt: new Date("2025-02-01T16:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000021", subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",   subjectCode: "DW004", status: "Active", enrolledAt: new Date("2025-02-01T16:05:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000022", subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",     subjectCode: "MD006", status: "Active", enrolledAt: new Date("2025-02-01T16:10:00Z") }
    ]
  },
  {
    _id: "b2000000-0000-0000-0000-000000000008",
    firstName:   "Pablo",
    lastName:    "Díaz Morales",
    fullName:    "Pablo Díaz Morales",
    email:       "pablo.diaz@universidad.es",
    dateOfBirth: new Date("2000-12-14T00:00:00Z"),
    createdAt:   new Date("2024-09-13T09:20:00Z"),
    updatedAt:   null,
    enrollments: [
      { enrollmentId: "c3000000-0000-0000-0000-000000000023", subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Active", enrolledAt: new Date("2025-02-01T17:00:00Z") },
      { enrollmentId: "c3000000-0000-0000-0000-000000000024", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Active", enrolledAt: new Date("2025-02-01T17:05:00Z") }
    ]
  }
]);

// =============================================================================
//  ENROLLMENTS — colección plana para queries por asignatura
//  Desnormalizada con nombre del alumno y de la asignatura
// =============================================================================
db.enrollments.insertMany([
  // Ana García
  { _id: "c3000000-0000-0000-0000-000000000001", studentId: "b2000000-0000-0000-0000-000000000001", studentName: "Ana García López",       studentEmail: "ana.garcia@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T10:00:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000002", studentId: "b2000000-0000-0000-0000-000000000001", studentName: "Ana García López",       studentEmail: "ana.garcia@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Completed", enrolledAt: new Date("2024-09-15T10:05:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000003", studentId: "b2000000-0000-0000-0000-000000000001", studentName: "Ana García López",       studentEmail: "ana.garcia@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Active",    enrolledAt: new Date("2025-02-01T09:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000004", studentId: "b2000000-0000-0000-0000-000000000001", studentName: "Ana García López",       studentEmail: "ana.garcia@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T09:05:00Z"), completedAt: null },
  // Carlos Martínez
  { _id: "c3000000-0000-0000-0000-000000000005", studentId: "b2000000-0000-0000-0000-000000000002", studentName: "Carlos Martínez Ruiz",   studentEmail: "carlos.martinez@universidad.es",  subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T11:00:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000006", studentId: "b2000000-0000-0000-0000-000000000002", studentName: "Carlos Martínez Ruiz",   studentEmail: "carlos.martinez@universidad.es",  subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T11:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000007", studentId: "b2000000-0000-0000-0000-000000000002", studentName: "Carlos Martínez Ruiz",   studentEmail: "carlos.martinez@universidad.es",  subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",          subjectCode: "DK005", status: "Active",    enrolledAt: new Date("2025-02-01T11:05:00Z"), completedAt: null },
  // Laura Fernández
  { _id: "c3000000-0000-0000-0000-000000000008", studentId: "b2000000-0000-0000-0000-000000000003", studentName: "Laura Fernández Sánchez", studentEmail: "laura.fernandez@universidad.es", subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Active",    enrolledAt: new Date("2025-02-01T12:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000009", studentId: "b2000000-0000-0000-0000-000000000003", studentName: "Laura Fernández Sánchez", studentEmail: "laura.fernandez@universidad.es", subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Active",    enrolledAt: new Date("2025-02-01T12:05:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000010", studentId: "b2000000-0000-0000-0000-000000000003", studentName: "Laura Fernández Sánchez", studentEmail: "laura.fernandez@universidad.es", subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",        subjectCode: "MD006", status: "Cancelled", enrolledAt: new Date("2024-09-15T12:00:00Z"), completedAt: null },
  // Miguel López
  { _id: "c3000000-0000-0000-0000-000000000011", studentId: "b2000000-0000-0000-0000-000000000004", studentName: "Miguel López Torres",    studentEmail: "miguel.lopez@universidad.es",     subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Completed", enrolledAt: new Date("2024-09-15T13:00:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000012", studentId: "b2000000-0000-0000-0000-000000000004", studentName: "Miguel López Torres",    studentEmail: "miguel.lopez@universidad.es",     subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Completed", enrolledAt: new Date("2024-09-15T13:05:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000013", studentId: "b2000000-0000-0000-0000-000000000004", studentName: "Miguel López Torres",    studentEmail: "miguel.lopez@universidad.es",     subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Completed", enrolledAt: new Date("2024-09-15T13:10:00Z"), completedAt: new Date("2025-01-20T10:00:00Z") },
  { _id: "c3000000-0000-0000-0000-000000000014", studentId: "b2000000-0000-0000-0000-000000000004", studentName: "Miguel López Torres",    studentEmail: "miguel.lopez@universidad.es",     subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",          subjectCode: "DK005", status: "Active",    enrolledAt: new Date("2025-02-01T13:00:00Z"), completedAt: null },
  // Sofía Rodríguez
  { _id: "c3000000-0000-0000-0000-000000000015", studentId: "b2000000-0000-0000-0000-000000000005", studentName: "Sofía Rodríguez Pérez",  studentEmail: "sofia.rodriguez@universidad.es",  subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Active",    enrolledAt: new Date("2025-02-01T14:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000016", studentId: "b2000000-0000-0000-0000-000000000005", studentName: "Sofía Rodríguez Pérez",  studentEmail: "sofia.rodriguez@universidad.es",  subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",        subjectCode: "MD006", status: "Active",    enrolledAt: new Date("2025-02-01T14:05:00Z"), completedAt: null },
  // David González
  { _id: "c3000000-0000-0000-0000-000000000017", studentId: "b2000000-0000-0000-0000-000000000006", studentName: "David González Moreno",  studentEmail: "david.gonzalez@universidad.es",   subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Active",    enrolledAt: new Date("2025-02-01T15:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000018", studentId: "b2000000-0000-0000-0000-000000000006", studentName: "David González Moreno",  studentEmail: "david.gonzalez@universidad.es",   subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T15:05:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000019", studentId: "b2000000-0000-0000-0000-000000000006", studentName: "David González Moreno",  studentEmail: "david.gonzalez@universidad.es",   subjectId: "a1000000-0000-0000-0000-000000000005", subjectName: "DevOps y Kubernetes",          subjectCode: "DK005", status: "Cancelled", enrolledAt: new Date("2024-09-15T15:00:00Z"), completedAt: null },
  // Elena Jiménez
  { _id: "c3000000-0000-0000-0000-000000000020", studentId: "b2000000-0000-0000-0000-000000000007", studentName: "Elena Jiménez Castro",   studentEmail: "elena.jimenez@universidad.es",    subjectId: "a1000000-0000-0000-0000-000000000003", subjectName: "Arquitectura de Software",     subjectCode: "AS003", status: "Active",    enrolledAt: new Date("2025-02-01T16:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000021", studentId: "b2000000-0000-0000-0000-000000000007", studentName: "Elena Jiménez Castro",   studentEmail: "elena.jimenez@universidad.es",    subjectId: "a1000000-0000-0000-0000-000000000004", subjectName: "Desarrollo Web con .NET",      subjectCode: "DW004", status: "Active",    enrolledAt: new Date("2025-02-01T16:05:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000022", studentId: "b2000000-0000-0000-0000-000000000007", studentName: "Elena Jiménez Castro",   studentEmail: "elena.jimenez@universidad.es",    subjectId: "a1000000-0000-0000-0000-000000000006", subjectName: "Matemáticas Discretas",        subjectCode: "MD006", status: "Active",    enrolledAt: new Date("2025-02-01T16:10:00Z"), completedAt: null },
  // Pablo Díaz
  { _id: "c3000000-0000-0000-0000-000000000023", studentId: "b2000000-0000-0000-0000-000000000008", studentName: "Pablo Díaz Morales",     studentEmail: "pablo.diaz@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000001", subjectName: "Fundamentos de Programación", subjectCode: "FP001", status: "Active",    enrolledAt: new Date("2025-02-01T17:00:00Z"), completedAt: null },
  { _id: "c3000000-0000-0000-0000-000000000024", studentId: "b2000000-0000-0000-0000-000000000008", studentName: "Pablo Díaz Morales",     studentEmail: "pablo.diaz@universidad.es",       subjectId: "a1000000-0000-0000-0000-000000000002", subjectName: "Bases de Datos",               subjectCode: "BD002", status: "Active",    enrolledAt: new Date("2025-02-01T17:05:00Z"), completedAt: null }
]);

print('✓ Subjects    insertadas: ' + db.subjects.countDocuments());
print('✓ Students    insertados: ' + db.students.countDocuments());
print('✓ Enrollments insertadas: ' + db.enrollments.countDocuments());
print('MongoDB academy_read inicializado correctamente.');
