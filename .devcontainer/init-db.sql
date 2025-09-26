-- Initialize the development database with basic setup
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create test database for integration tests
CREATE DATABASE bloggit_test;
GRANT ALL PRIVILEGES ON DATABASE bloggit_test TO bloggit_user;