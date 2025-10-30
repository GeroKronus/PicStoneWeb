-- Script para criar tabelas do PicStone no SQL Server
-- Execute este script no banco DADOSADV_Q

USE DADOSADV_Q;
GO

-- Verifica se a tabela Usuarios já existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        NomeCompleto NVARCHAR(200) NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE()
    );

    PRINT 'Tabela Usuarios criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Tabela Usuarios já existe.';
END
GO

-- Verifica se a tabela FotosMobile já existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FotosMobile')
BEGIN
    CREATE TABLE FotosMobile (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NomeArquivo NVARCHAR(255) NOT NULL,
        Lote NVARCHAR(50) NOT NULL,
        Chapa NVARCHAR(50) NOT NULL,
        Processo NVARCHAR(50) NOT NULL,
        Espessura INT NULL,
        DataUpload DATETIME NOT NULL DEFAULT GETDATE(),
        Usuario NVARCHAR(100) NULL,
        CaminhoArquivo NVARCHAR(500) NULL
    );

    PRINT 'Tabela FotosMobile criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Tabela FotosMobile já existe.';
END
GO

-- Cria usuário admin padrão (senha: admin123)
-- Hash BCrypt da senha "admin123"
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Username = 'admin')
BEGIN
    INSERT INTO Usuarios (Username, PasswordHash, NomeCompleto, Ativo, DataCriacao)
    VALUES (
        'admin',
        '$2a$11$1O8pKjQQqGvPJE7YqGvZ8O0yYJZ1mKQMWB5g5c.TLHqZN6YvH7jbC',
        'Administrador',
        1,
        GETDATE()
    );

    PRINT 'Usuario admin criado com sucesso!';
    PRINT 'Login: admin';
    PRINT 'Senha: admin123';
END
ELSE
BEGIN
    PRINT 'Usuario admin já existe.';
END
GO

-- Verifica criação
SELECT 'Tabelas criadas:' AS Status;
SELECT name AS NomeTabela
FROM sys.tables
WHERE name IN ('Usuarios', 'FotosMobile');

SELECT 'Usuarios cadastrados:' AS Status;
SELECT Id, Username, NomeCompleto, Ativo, DataCriacao
FROM Usuarios;
GO

PRINT '';
PRINT '========================================';
PRINT 'SETUP CONCLUIDO COM SUCESSO!';
PRINT '========================================';
PRINT 'Tabelas: Usuarios e FotosMobile';
PRINT 'Usuario padrao: admin / admin123';
PRINT '========================================';
