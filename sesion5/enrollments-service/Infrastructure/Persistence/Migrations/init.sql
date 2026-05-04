-- ── Base de datos del servicio de matrículas ─────────────────────────────────

CREATE TABLE IF NOT EXISTS `subjects` (
    `id`                   CHAR(36)     NOT NULL,
    `code`                 VARCHAR(20)  NOT NULL UNIQUE,
    `name`                 VARCHAR(150) NOT NULL,
    `description`          VARCHAR(500) NULL,
    `credits`              INT          NOT NULL DEFAULT 3,
    `max_capacity`         INT          NOT NULL DEFAULT 30,
    `current_enrollments`  INT          NOT NULL DEFAULT 0,
    `is_active`            TINYINT(1)   NOT NULL DEFAULT 1,
    `created_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_code`      (`code`),
    INDEX `idx_is_active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `enrollments` (
    `id`           CHAR(36)     NOT NULL,
    `student_id`   CHAR(36)     NOT NULL,
    `subject_id`   CHAR(36)     NOT NULL,
    `status`       ENUM('Active','Cancelled','Completed') NOT NULL DEFAULT 'Active',
    `notes`        VARCHAR(300) NULL,
    `enrolled_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `cancelled_at` DATETIME     NULL,
    `updated_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `student_name` VARCHAR(200) NULL,
    `subject_name` VARCHAR(150) NULL,
    `subject_code` VARCHAR(20)  NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_student_id`    (`student_id`),
    INDEX `idx_subject_id`    (`subject_id`),
    INDEX `idx_status`        (`status`),
    INDEX `idx_student_active` (`student_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── Asignaturas de ejemplo ────────────────────────────────────────────────────
INSERT INTO `subjects` (`id`, `code`, `name`, `description`, `credits`, `max_capacity`) VALUES
    (UUID(), 'MAT-101', 'Matemáticas I',            'Álgebra lineal y cálculo diferencial',            6, 40),
    (UUID(), 'FIS-101', 'Física I',                 'Mecánica clásica y termodinámica',                6, 35),
    (UUID(), 'INF-101', 'Programación I',            'Fundamentos de programación en Python',           4, 50),
    (UUID(), 'INF-201', 'Estructuras de Datos',      'Arrays, listas, árboles y grafos',                4, 40),
    (UUID(), 'INF-301', 'Bases de Datos',            'SQL, modelado relacional y NoSQL',                4, 45),
    (UUID(), 'INF-401', 'Arquitectura de Software',  'Patrones de diseño y arquitecturas limpias',      3, 30),
    (UUID(), 'ENG-101', 'Inglés Técnico',            'Redacción técnica y comunicación profesional',    3, 60);
