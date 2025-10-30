-- ========================================
-- PicStone - Script de Criação de Tabelas
-- PostgreSQL (Railway)
-- ========================================

-- NOTA: Este script é OPCIONAL!
-- A aplicação cria as tabelas automaticamente no primeiro deploy.
-- Use este script apenas se precisar criar as tabelas manualmente.

-- ========================================
-- Tabela: Usuarios
-- ========================================

CREATE TABLE IF NOT EXISTS "Usuarios" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "NomeCompleto" VARCHAR(200),
    "Ativo" BOOLEAN NOT NULL DEFAULT true,
    "DataCriacao" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Criar índice no username para performance
CREATE INDEX IF NOT EXISTS "IX_Usuarios_Username" ON "Usuarios" ("Username");

-- ========================================
-- Tabela: FotosMobile
-- ========================================

CREATE TABLE IF NOT EXISTS "FotosMobile" (
    "Id" SERIAL PRIMARY KEY,
    "NomeArquivo" VARCHAR(255) NOT NULL,
    "Lote" VARCHAR(50) NOT NULL,
    "Chapa" VARCHAR(50) NOT NULL,
    "Processo" VARCHAR(50) NOT NULL,
    "Espessura" INTEGER NULL,
    "DataUpload" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Usuario" VARCHAR(100),
    "CaminhoArquivo" VARCHAR(500)
);

-- Criar índices para performance
CREATE INDEX IF NOT EXISTS "IX_FotosMobile_Lote" ON "FotosMobile" ("Lote");
CREATE INDEX IF NOT EXISTS "IX_FotosMobile_Chapa" ON "FotosMobile" ("Chapa");
CREATE INDEX IF NOT EXISTS "IX_FotosMobile_DataUpload" ON "FotosMobile" ("DataUpload" DESC);

-- ========================================
-- Inserir Usuário Admin Padrão
-- ========================================

-- Senha: admin123 (hash BCrypt)
-- ATENÇÃO: Altere a senha em produção!

INSERT INTO "Usuarios" ("Username", "PasswordHash", "NomeCompleto", "Ativo", "DataCriacao")
VALUES (
    'admin',
    '$2a$11$8P3gQfJJKfJXFZl6qXQZ1.mYH5vY5ZbYCQj8kH5KfJXFZl6qXQZ1e', -- admin123
    'Administrador',
    true,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Username") DO NOTHING;

-- ========================================
-- Verificação
-- ========================================

-- Listar todos os usuários
SELECT * FROM "Usuarios";

-- Listar todas as fotos
SELECT * FROM "FotosMobile" ORDER BY "DataUpload" DESC;

-- Contar registros
SELECT
    (SELECT COUNT(*) FROM "Usuarios") as total_usuarios,
    (SELECT COUNT(*) FROM "FotosMobile") as total_fotos;

-- ========================================
-- Consultas Úteis
-- ========================================

-- Ver fotos dos últimos 7 dias
SELECT
    "Id",
    "NomeArquivo",
    "Lote",
    "Chapa",
    "Processo",
    "Usuario",
    "DataUpload"
FROM "FotosMobile"
WHERE "DataUpload" >= CURRENT_TIMESTAMP - INTERVAL '7 days'
ORDER BY "DataUpload" DESC;

-- Estatísticas por processo
SELECT
    "Processo",
    COUNT(*) as total_fotos,
    MIN("DataUpload") as primeira_foto,
    MAX("DataUpload") as ultima_foto
FROM "FotosMobile"
GROUP BY "Processo"
ORDER BY total_fotos DESC;

-- ========================================
-- Manutenção
-- ========================================

-- Limpar fotos antigas (mais de 6 meses)
-- CUIDADO: Isso remove os registros permanentemente!
-- DELETE FROM "FotosMobile"
-- WHERE "DataUpload" < CURRENT_TIMESTAMP - INTERVAL '6 months';

-- Limpar usuários inativos
-- DELETE FROM "Usuarios"
-- WHERE "Ativo" = false;
