-- ============================================================================
-- SAT · Descarga automática: envío escalonado (5 min entre clientes) + diaria
-- Base de datos: PostgreSQL  (el proyecto usa UseSnakeCaseNamingConvention)
-- Ejecutar UNA vez antes de levantar la API con este cambio. Es idempotente.
-- ============================================================================

BEGIN;

-- Jobs: tipo de recurrencia y separación entre clientes
ALTER TABLE sat_jobs
    ADD COLUMN IF NOT EXISTS recurrencia                  text    NOT NULL DEFAULT 'Unica',
    ADD COLUMN IF NOT EXISTS intervalo_entre_clientes_min integer NOT NULL DEFAULT 5;

-- Clientes del job: hora (UTC) en la que le toca enviar su solicitud
ALTER TABLE sat_job_clientes
    ADD COLUMN IF NOT EXISTS fecha_envio_programada timestamp with time zone NULL;

COMMIT;

-- Nota: los jobs creados ANTES de este cambio quedan con recurrencia 'Unica' y
-- fecha_envio_programada NULL; el orquestador los escalona solo (envía el primero
-- y programa el resto de 5 en 5 minutos).
