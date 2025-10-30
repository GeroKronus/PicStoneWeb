# 📸 PicStone - Sistema de Captura de Fotos Mobile

Sistema web responsivo para captura de fotos via celular com integração ao SQL Server.

## 🚀 Características

- ✅ Interface mobile-first otimizada para celulares
- ✅ Captura de fotos usando câmera nativa
- ✅ Compressão automática de imagens (máx 1920x1080)
- ✅ Upload para backend ASP.NET Core
- ✅ Autenticação JWT com expiração de 8 horas
- ✅ **Suporte multi-banco:** PostgreSQL (Railway), SQLite (local) ou SQL Server
- ✅ Transferência FTP automática com retry
- ✅ Nomeação padronizada: `LOTE_CHAPA_YYYYMMDD_HHMMSS.jpg`
- ✅ Deploy pronto para Railway com PostgreSQL

## 🏗️ Arquitetura

```
Frontend (HTML5 + CSS3 + JavaScript)
    ↓ HTTPS
Backend (ASP.NET Core 8.0 Web API)
    ↓
Banco de Dados (PostgreSQL / SQLite / SQL Server)
    ↓ (opcional)
FTP Server (transferência de arquivos)
```

### Bancos de Dados Suportados:
- **PostgreSQL** - Recomendado para produção no Railway (detecção automática via `DATABASE_URL`)
- **SQLite** - Desenvolvimento local (arquivo `picstone.db`)
- **SQL Server** - Integração com banco existente

## 📁 Estrutura do Projeto

```
PicStone WEB/
├── Frontend/
│   ├── index.html          # Interface responsiva
│   ├── style.css           # Estilos mobile-first
│   └── app.js              # Lógica de captura e upload
├── Backend/
│   ├── Controllers/        # AuthController, FotosController
│   ├── Models/            # Entidades e DTOs
│   ├── Services/          # Lógica de negócio
│   ├── Data/              # Entity Framework Context
│   ├── Program.cs         # Configuração da API
│   ├── appsettings.json   # Configurações locais
│   └── *.csproj           # Dependências .NET
├── Dockerfile             # Container para Railway
├── railway.json           # Configuração Railway
├── .dockerignore
└── README.md
```

## 🛠️ Pré-requisitos

- .NET 8.0 SDK
- SQL Server (acesso ao servidor existente)
- Docker (para deploy)

## ⚙️ Configuração Local

### 🚀 Modo Rápido (Windows)

Execute o arquivo **`MENU.bat`** e escolha a opção **[1] Iniciar Servidor Local**

Ou clique diretamente em **`iniciar-local.bat`**

### 📋 Modo Manual

#### 1. Clone ou baixe o projeto

```bash
cd "D:\Claude Code\PicStone WEB"
```

#### 2. Configure o `appsettings.json`

Edite `Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=131.255.255.16,11433;Initial Catalog=DADOSADV_Q;User ID=PicStoneQualita;Password=@PicQualit@Stone#;TrustServerCertificate=True;Encrypt=False;"
  },
  "JWT_SECRET": "SuaChaveSecretaForte123!@#",
  "UPLOAD_PATH": "./uploads",
  "FTP_SERVER": "ftp.seuservidor.com",
  "FTP_USER": "usuario",
  "FTP_PASSWORD": "senha"
}
```

#### 3. Instale as dependências

```bash
cd Backend
dotnet restore
```

#### 4. Execute localmente

```bash
dotnet run
```

A aplicação estará disponível em: `http://localhost:5000`

#### 5. Acesse o sistema

- **URL:** http://localhost:5000
- **Usuário padrão:** `admin`
- **Senha padrão:** `admin123`

### 🛠️ Utilitários Disponíveis (Windows)

O projeto inclui arquivos `.bat` para facilitar o desenvolvimento:

| Arquivo | Descrição |
|---------|-----------|
| **MENU.bat** | Menu interativo com todas as opções |
| **iniciar-local.bat** | Inicia o servidor automaticamente |
| **parar-servidor.bat** | Para todos os processos do servidor |
| **testar-conexao-sql.bat** | Testa conectividade com SQL Server |
| **descobrir-ip.bat** | Mostra seu IP para acesso mobile |
| **limpar-build.bat** | Remove arquivos temporários |

## 🐳 Deploy no Railway com PostgreSQL

### Guia Completo de Deploy

📘 **[DEPLOY-RAILWAY-POSTGRES.md](DEPLOY-RAILWAY-POSTGRES.md)** - Guia detalhado de deploy

### Resumo Rápido:

1. **Criar projeto no Railway**
   - Deploy from GitHub repo: `GeroKronus/PicStoneWeb`

2. **Adicionar PostgreSQL**
   - Railway → New → Database → PostgreSQL
   - `DATABASE_URL` é configurado automaticamente

3. **Configurar variáveis**
   ```env
   JWT_SECRET=SuaChaveSecretaSuperSegura123!@#$%
   UPLOAD_PATH=/app/uploads
   ```

4. **Deploy automático**
   - Railway detecta Dockerfile
   - Conecta ao PostgreSQL automaticamente
   - Cria tabelas e usuário admin
   - Expõe aplicação em URL pública

**Nota:** A variável `DATABASE_URL` é injetada automaticamente pelo Railway. Você NÃO precisa configurá-la manualmente.

## 📊 Banco de Dados

### Tabelas Criadas Automaticamente

#### **FotosMobile**
```sql
CREATE TABLE FotosMobile (
    Id INT IDENTITY PRIMARY KEY,
    NomeArquivo NVARCHAR(255) NOT NULL,
    Lote NVARCHAR(50) NOT NULL,
    Chapa NVARCHAR(50) NOT NULL,
    Processo NVARCHAR(50) NOT NULL,
    Espessura INT NULL,
    DataUpload DATETIME NOT NULL,
    Usuario NVARCHAR(100),
    CaminhoArquivo NVARCHAR(500)
)
```

#### **Usuarios**
```sql
CREATE TABLE Usuarios (
    Id INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    NomeCompleto NVARCHAR(200),
    Ativo BIT NOT NULL DEFAULT 1,
    DataCriacao DATETIME NOT NULL
)
```

## 🔐 Segurança

- ✅ **Autenticação JWT** - Tokens expiram em 8 horas
- ✅ **Senhas hasheadas** - BCrypt com salt automático
- ✅ **Validação de arquivos** - Tipos MIME e tamanho máximo
- ✅ **Sanitização** - Nomes de arquivo e inputs SQL
- ✅ **CORS configurado** - Aceita qualquer origem (ajuste em produção)

## 📱 Endpoints da API

### Autenticação

**POST** `/api/auth/login`
```json
Request:
{
  "username": "admin",
  "password": "admin123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "username": "admin",
  "expiresAt": "2025-10-30T22:30:00Z"
}
```

### Fotos (requer autenticação)

**POST** `/api/fotos/upload`
```
Content-Type: multipart/form-data
Authorization: Bearer {token}

Campos:
- Arquivo: file (JPG, JPEG, PNG, máx 10MB)
- Lote: string (obrigatório)
- Chapa: string (obrigatório)
- Processo: string (obrigatório)
- Espessura: int (opcional)
```

**GET** `/api/fotos/historico?limite=50`
```
Authorization: Bearer {token}

Response:
{
  "total": 5,
  "fotos": [
    {
      "id": 1,
      "nomeArquivo": "12345_001_20251030_143022.jpg",
      "lote": "12345",
      "chapa": "001",
      "processo": "Polimento",
      "espessura": 20,
      "dataUpload": "2025-10-30T14:30:22",
      "usuario": "admin",
      "caminhoArquivo": "/app/uploads/12345_001_20251030_143022.jpg"
    }
  ]
}
```

**GET** `/api/fotos/processos`
```
Authorization: Bearer {token}

Response:
["Polimento", "Resina", "Acabamento"]
```

## 🔧 Manutenção

### Adicionar novo usuário

Execute SQL no banco:

```sql
INSERT INTO Usuarios (Username, PasswordHash, NomeCompleto, Ativo, DataCriacao)
VALUES (
    'novo_usuario',
    '$2a$11$...',  -- Use BCrypt para gerar o hash
    'Nome Completo',
    1,
    GETDATE()
)
```

### Verificar logs

Logs são salvos em:
- Local: `./logs/picstone-YYYYMMDD.log`
- Railway: Use `railway logs` no terminal

### Backup de fotos

Configure um cronjob para backup do diretório:
- Local: `./uploads/`
- Railway: `/app/uploads/`

## 📄 Swagger / Documentação da API

Quando executar localmente, acesse:

```
http://localhost:5000/swagger
```

## 🐛 Troubleshooting

### Erro de conexão com SQL Server

Verifique:
1. Servidor SQL Server está acessível
2. Firewall permite conexões na porta 11433
3. Credenciais estão corretas
4. String de conexão tem `TrustServerCertificate=True`

### Erro "Unable to upload file"

Verifique:
1. Diretório `/app/uploads` tem permissões de escrita
2. Tamanho do arquivo é menor que 10MB
3. Formato é JPG, JPEG ou PNG

### Token expirado

Tokens JWT expiram em 8 horas. Faça login novamente.

### FTP não funciona

Configure corretamente as variáveis:
- `FTP_SERVER`
- `FTP_USER`
- `FTP_PASSWORD`

Se não usar FTP, deixe vazio que o sistema apenas salvará localmente.

## 📞 Suporte

Para problemas ou dúvidas:
1. Verifique os logs em `logs/`
2. Teste endpoints via Swagger
3. Valide configurações de ambiente

## 📝 Licença

Este projeto foi desenvolvido para uso interno da PicStone.

---

**Desenvolvido com ❤️ para PicStone Qualidade**
