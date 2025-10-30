# 🔧 Setup do Banco de Dados - Para o DBA

## ⚠️ IMPORTANTE

O usuário **PicStoneQualita** não tem permissão para criar tabelas.

Você (DBA) precisa executar o script SQL abaixo **manualmente** no banco `DADOSADV_Q`.

---

## 📋 Script SQL para Executar

```sql
-- ============================================
-- PicStone - Criação de Tabelas
-- Banco: DADOSADV_Q
-- ============================================

USE DADOSADV_Q;
GO

-- 1. Criar tabela de Usuarios
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        NomeCompleto NVARCHAR(200) NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_Usuarios_Username UNIQUE (Username)
    );

    PRINT 'Tabela Usuarios criada';
END
ELSE
BEGIN
    PRINT 'Tabela Usuarios já existe';
END
GO

-- 2. Criar tabela de FotosMobile
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

    PRINT 'Tabela FotosMobile criada';
END
ELSE
BEGIN
    PRINT 'Tabela FotosMobile já existe';
END
GO

-- 3. Criar usuario admin padrão
-- Senha: admin123 (hasheada com BCrypt)
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

    PRINT 'Usuario admin criado';
    PRINT 'Login: admin';
    PRINT 'Senha: admin123';
END
ELSE
BEGIN
    PRINT 'Usuario admin já existe';
END
GO

-- 4. Verificar criação
PRINT '';
PRINT '========================================';
PRINT 'VERIFICACAO:';
PRINT '========================================';

SELECT 'Tabelas' AS Tipo, name AS Nome
FROM sys.tables
WHERE name IN ('Usuarios', 'FotosMobile');

SELECT 'Usuarios' AS Tipo, Username, NomeCompleto, Ativo
FROM Usuarios;

PRINT '========================================';
PRINT 'SETUP CONCLUIDO!';
PRINT '========================================';
GO
```

---

## ✅ Após Executar o Script

1. **Verifique** que as tabelas foram criadas:
   ```sql
   SELECT * FROM sys.tables WHERE name IN ('Usuarios', 'FotosMobile')
   ```

2. **Verifique** que o usuário admin existe:
   ```sql
   SELECT * FROM Usuarios WHERE Username = 'admin'
   ```

3. **Informe o desenvolvedor** que pode iniciar a aplicação

---

## 🔐 Credenciais Padrão

- **Usuário:** admin
- **Senha:** admin123

⚠️ **Altere a senha em produção!**

---

## 📊 Estrutura das Tabelas

### Tabela: Usuarios
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| Id | INT IDENTITY | Chave primária |
| Username | NVARCHAR(100) | Login (único) |
| PasswordHash | NVARCHAR(255) | Senha hasheada (BCrypt) |
| NomeCompleto | NVARCHAR(200) | Nome completo |
| Ativo | BIT | Usuário ativo? |
| DataCriacao | DATETIME | Data de criação |

### Tabela: FotosMobile
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| Id | INT IDENTITY | Chave primária |
| NomeArquivo | NVARCHAR(255) | Nome do arquivo |
| Lote | NVARCHAR(50) | Número do lote |
| Chapa | NVARCHAR(50) | Número da chapa |
| Processo | NVARCHAR(50) | Processo (Polimento/Resina/Acabamento) |
| Espessura | INT | Espessura em mm (opcional) |
| DataUpload | DATETIME | Data/hora do upload |
| Usuario | NVARCHAR(100) | Usuário que fez upload |
| CaminhoArquivo | NVARCHAR(500) | Caminho do arquivo no servidor |

---

## 🚀 Próximos Passos

Após executar o script:

1. Usuário pode executar: `.\MENU.ps1`
2. Escolher opção **[1] Iniciar Servidor Local**
3. Acessar: http://localhost:5000
4. Login: admin / admin123

---

## ❓ Dúvidas

Se houver problemas, verifique:

- ✅ Usuário `PicStoneQualita` tem permissão de **SELECT**, **INSERT**, **UPDATE**, **DELETE**
- ✅ Tabelas foram criadas no banco correto (`DADOSADV_Q`)
- ✅ Usuário admin foi inserido

---

**Arquivo também disponível em:** `criar-tabelas.sql` (mesma pasta do projeto)
