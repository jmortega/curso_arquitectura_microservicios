CREATE TABLE IF NOT EXISTS `students` (
    `id`                CHAR(36)     NOT NULL DEFAULT (UUID()),
    `first_name`        VARCHAR(100) NOT NULL,
    `last_name`         VARCHAR(100) NOT NULL,
    `email`             VARCHAR(150) NOT NULL UNIQUE,
    `enrollment_number` VARCHAR(20)  NOT NULL UNIQUE,
    `date_of_birth`     DATE         NULL,
    `phone`             VARCHAR(20)  NULL,
    `address`           VARCHAR(255) NULL,
    `is_active`         TINYINT(1)  NOT NULL DEFAULT 1,
    `created_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_email`       (`email`),
    INDEX `idx_enrollment`  (`enrollment_number`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `students` (`first_name`, `last_name`, `email`, `enrollment_number`, `date_of_birth`, `phone`) VALUES
    ('Ana',    'García',   'ana.garcia@university.edu',  'A2024001', '2000-03-15', '555-0101'),
    ('Carlos', 'Martínez', 'carlos.m@university.edu',   'A2024002', '1999-07-22', '555-0102'),
    ('Laura',  'López',    'laura.lopez@university.edu', 'A2024003', '2001-01-10', '555-0103');