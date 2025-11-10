-- Migration: Add Email Verification and User Approval System
-- Date: 2025-11-07
-- Description: Adds email, verification token, status and expiration fields to Usuarios table

-- Add new columns
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "Email" VARCHAR(255) NOT NULL DEFAULT '';
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "EmailVerificado" BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "TokenVerificacao" VARCHAR(255);
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "Status" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "DataExpiracao" TIMESTAMP;

-- Create unique index on Email
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_Email" ON "Usuarios" ("Email");

-- Create index on TokenVerificacao for faster lookups
CREATE INDEX IF NOT EXISTS "IX_Usuarios_TokenVerificacao" ON "Usuarios" ("TokenVerificacao");

-- Update existing admin user to have approved status and verified email
UPDATE "Usuarios"
SET
    "Email" = 'admin@picstone.com.br',
    "EmailVerificado" = TRUE,
    "Status" = 2,  -- StatusUsuario.Aprovado
    "TokenVerificacao" = NULL
WHERE "Username" = 'admin' AND "Email" = '';

-- Add comment for Status values
COMMENT ON COLUMN "Usuarios"."Status" IS '0=Pendente, 1=AguardandoAprovacao, 2=Aprovado, 3=Rejeitado, 4=Expirado';
