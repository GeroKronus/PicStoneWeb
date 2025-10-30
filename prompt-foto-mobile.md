# Prompt para Claude Code - Aplicação Web de Captura de Fotos

Preciso criar um protótipo de aplicação web para captura de fotos via celular com integração ao meu servidor. Detalhes:

## OBJETIVO

Aplicação web responsiva que permite:
1. Tirar fotos usando a câmera do celular
2. Upload automático para o backend
3. Renomeação padronizada dos arquivos
4. Integração com servidor SQL Server existente
5. Transferência dos arquivos para servidor local

## STACK TECNOLÓGICA

- **Frontend**: HTML5, CSS3, JavaScript puro (sem frameworks)
- **Backend**: ASP.NET Core Web API (.NET 6 ou superior)
- **Banco de Dados**: SQL Server
- **Deploy**: Railway (forneça Dockerfile e railway.json)

## FUNCIONALIDADES PRINCIPAIS

### Frontend

- Interface simples e intuitiva para mobile
- Botão para acionar câmera nativa do celular
- Preview da foto antes do upload
- Campos para entrada de dados:
  * Número do Lote (obrigatório)
  * Número da Chapa (obrigatório)
  * Processo (dropdown: Polimento, Resina, Acabamento)
  * Espessura (numérico)
- Indicador visual de progresso do upload
- Mensagem de confirmação/erro
- Sistema de login simples (usuário/senha)

### Backend (API REST)

- **POST /api/auth/login**: Autenticação com JWT
- **POST /api/fotos/upload**: Recebe foto + metadados
- **GET /api/fotos/historico**: Lista últimas fotos enviadas
- Validações:
  * Tipos permitidos: JPG, JPEG, PNG
  * Tamanho máximo: 10MB
  * Campos obrigatórios

### Padrão de Nomeação

Formato: `LOTE_CHAPA_YYYYMMDD_HHMMSS.jpg`

Exemplo: `12345_001_20251030_143022.jpg`

### Integração com Servidor Existente

```csharp
// String de conexão SQL Server
"Data Source=131.255.255.16,11433;" +
"Initial Catalog=DADOSADV_Q;" +
"User ID=PicStoneQualita;" +
"Password=@PicQualit@Stone#;" +
"TrustServerCertificate=True;" +
"Encrypt=False;"
```

Salvar registro na tabela (criar se não existir):

```sql
CREATE TABLE FotosMobile (
    Id INT IDENTITY PRIMARY KEY,
    NomeArquivo NVARCHAR(255),
    Lote NVARCHAR(50),
    Chapa NVARCHAR(50),
    Processo NVARCHAR(50),
    Espessura INT,
    DataUpload DATETIME,
    Usuario NVARCHAR(100),
    CaminhoArquivo NVARCHAR(500)
)
```

### Transferência de Arquivos

Após upload, transferir arquivo via FTP ou salvar em diretório compartilhado:
- Criar configuração para path de destino (via appsettings.json)
- Implementar retry em caso de falha

## SEGURANÇA

- Autenticação JWT (token expira em 8 horas)
- Validação de tipos MIME
- Sanitização de nomes de arquivo
- CORS configurado para domínios específicos
- Senhas hasheadas (BCrypt)

## ESTRUTURA DO PROJETO

```
/ProjetoFotoMobile
  /Frontend
    - index.html
    - style.css
    - app.js
  /Backend
    /Controllers
    /Models
    /Services
    - Program.cs
    - appsettings.json
  - Dockerfile
  - railway.json
  - README.md
```

## REQUISITOS ADICIONAIS

1. Código comentado em português
2. Tratamento de erros robusto
3. Logs de todas as operações
4. Compressão de imagem no frontend (reduzir para 1920x1080 se maior)
5. README com instruções de:
   - Configuração local
   - Deploy no Railway
   - Configuração de variáveis de ambiente

## VARIÁVEIS DE AMBIENTE (Railway)

```env
SQL_CONNECTION_STRING=...
JWT_SECRET=...
UPLOAD_PATH=...
FTP_SERVER=...
FTP_USER=...
FTP_PASSWORD=...
```

---

## INSTRUÇÕES PARA O CLAUDE CODE

Por favor, crie este protótipo funcional e pronto para deploy no Railway.

**Comece criando a estrutura de pastas e me mostre o plano de implementação antes de gerar todo o código.**
