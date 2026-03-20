-- 1) Databases
CREATE DATABASE fcg_payments_db;

-- 2) Users (roles)
CREATE USER payments WITH PASSWORD 'paymentspw';

-- 3) Grants no nível do DATABASE (conexão)
GRANT CONNECT, TEMPORARY ON DATABASE fcg_payments_db TO payments;

-- 4) Grants no schema public (é isso que resolve o EF migrations)
\connect fcg_payments_db
GRANT USAGE, CREATE ON SCHEMA public TO payments;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO payments;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO payments;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO payments;
