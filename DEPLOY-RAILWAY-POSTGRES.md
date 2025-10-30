# 🚀 Deploy no Railway com PostgreSQL - PicStone

## 📋 Pré-requisitos

- Conta no Railway: https://railway.app
- Repositório GitHub: https://github.com/GeroKronus/PicStoneWeb

---

## 🎯 Deploy Completo (Recomendado)

### **Passo 1: Criar Novo Projeto no Railway**

1. Acesse: https://railway.app
2. Clique em **"New Project"**
3. Selecione **"Deploy from GitHub repo"**
4. Escolha o repositório: **GeroKronus/PicStoneWeb**
5. Railway detectará automaticamente o Dockerfile

### **Passo 2: Adicionar PostgreSQL**

No painel do Railway:

1. Clique em **"+ New"** no projeto
2. Selecione **"Database"** → **"Add PostgreSQL"**
3. Railway criará automaticamente:
   - Um container PostgreSQL
   - Variável `DATABASE_URL` conectada ao seu serviço
   - Backup automático

**IMPORTANTE:** Railway conecta automaticamente o PostgreSQL ao seu aplicativo através da variável `DATABASE_URL`. Você NÃO precisa configurar nada manualmente!

### **Passo 3: Configurar Variáveis de Ambiente**

No serviço da aplicação (não no PostgreSQL), vá em **Variables** e adicione:

```env
JWT_SECRET=SuaChaveSecretaSuperSegura123!@#$%
UPLOAD_PATH=/app/uploads
```

**Nota:** A variável `DATABASE_URL` já está configurada automaticamente pelo Railway quando você adiciona o PostgreSQL.

### **Passo 4: Deploy Automático**

Railway irá automaticamente:
- ✅ Detectar o Dockerfile
- ✅ Fazer build da imagem
- ✅ Conectar ao PostgreSQL via `DATABASE_URL`
- ✅ Criar tabelas automaticamente no PostgreSQL
- ✅ Criar usuário admin (admin/admin123)
- ✅ Expor na porta 8080
- ✅ Gerar URL pública

Aguarde o deploy concluir (~3-5 minutos).

### **Passo 5: Verificar Logs**

1. Clique no serviço da aplicação
2. Vá em **Logs**
3. Verifique se aparece:
   ```
   Usando PostgreSQL (Railway): [nome-do-host].railway.app
   Criando/atualizando banco de dados PostgreSQL...
   Banco de dados PostgreSQL pronto!
   Usuario admin verificado/criado com sucesso
   Iniciando PicStone Foto API na porta 8080
   ```

---

## 🎯 Opção 2: Deploy via CLI Railway

### **Passo 1: Instalar Railway CLI**

```bash
# Windows (PowerShell)
iwr https://railway.app/install.ps1 | iex

# macOS/Linux
curl -fsSL https://railway.app/install.sh | sh

# Ou via npm
npm i -g @railway/cli
```

### **Passo 2: Login**

```bash
railway login
```

### **Passo 3: Criar Projeto e Adicionar PostgreSQL**

```bash
cd "D:\Claude Code\PicStone WEB"

# Criar novo projeto
railway init

# Adicionar PostgreSQL
railway add --database postgres
```

### **Passo 4: Configurar Variáveis**

```bash
railway variables set JWT_SECRET="SuaChaveSecretaSuperSegura123"
railway variables set UPLOAD_PATH="/app/uploads"
```

**Nota:** `DATABASE_URL` já está configurado automaticamente.

### **Passo 5: Deploy**

```bash
railway up
```

---

## 📊 Vantagens do PostgreSQL no Railway

✅ **Criação Automática:**
- Railway cria e configura o banco automaticamente
- Conexão via `DATABASE_URL` injetada automaticamente

✅ **Backup Automático:**
- Backups diários automáticos
- Restauração com um clique

✅ **Escalabilidade:**
- Melhor performance que SQLite
- Suporta múltiplas conexões simultâneas

✅ **Persistência:**
- Dados nunca são perdidos (mesmo com redeploy)
- Volume persistente gerenciado pelo Railway

✅ **Zero Configuração:**
- Não precisa criar tabelas manualmente
- Aplicação detecta PostgreSQL e cria tudo automaticamente

---

## 🔐 Criar Usuário Admin Após Deploy

### **Automático**

O sistema cria automaticamente no primeiro deploy:
- **Usuário:** admin
- **Senha:** admin123

⚠️ **ALTERE A SENHA EM PRODUÇÃO!**

Você pode alterar a senha pelo banco de dados PostgreSQL no Railway:

1. Clique no serviço PostgreSQL
2. Vá em **Data** (Query Tab)
3. Execute:
   ```sql
   UPDATE "Usuarios"
   SET "PasswordHash" = '$2a$11$seu-hash-bcrypt-aqui'
   WHERE "Username" = 'admin';
   ```

---

## 🌐 Configurar Domínio Customizado

1. No Railway, selecione o serviço da aplicação
2. Vá em **Settings**
3. Clique em **Generate Domain** (Railway fornece gratuitamente)
4. Ou adicione seu domínio customizado:
   - Settings → Networking → Custom Domain
   - Adicione seu domínio
   - Configure DNS conforme instruções

---

## 📱 Testar o Deploy

### **1. Acessar a Aplicação**

```
https://seu-projeto.up.railway.app
```

### **2. Fazer Login**

- **Usuário:** admin
- **Senha:** admin123

### **3. Testar Upload de Foto**

- Tire uma foto
- Preencha: Lote, Chapa, Processo
- Envie

### **4. Verificar Banco de Dados**

No painel PostgreSQL do Railway:
1. Clique no serviço PostgreSQL
2. Vá em **Data**
3. Execute consultas:
   ```sql
   SELECT * FROM "Usuarios";
   SELECT * FROM "FotosMobile";
   ```

### **5. Verificar Logs**

```bash
railway logs
```

Ou via Dashboard → View Logs

---

## 🔄 Atualizar o Deploy

### **Via GitHub (Automático)**

1. Faça push para o repositório:
   ```bash
   git add .
   git commit -m "Atualização"
   git push origin main
   ```

2. Railway detecta automaticamente e faz redeploy

### **Via CLI**

```bash
railway up
```

---

## 📊 Monitoramento

### **Ver Logs em Tempo Real**

```bash
railway logs --follow
```

### **Status da Aplicação**

```bash
railway status
```

### **Abrir Dashboard**

```bash
railway open
```

### **Métricas do PostgreSQL**

No painel do Railway:
1. Clique no serviço PostgreSQL
2. Veja métricas:
   - Conexões ativas
   - Uso de CPU/Memória
   - Tamanho do banco
   - Queries executadas

---

## 🐛 Troubleshooting

### **Problema: Build Falhou**

**Solução:**
```bash
# Ver logs do build
railway logs --deployment

# Verificar se Dockerfile existe
# Verificar se Backend/ e Frontend/ estão presentes
```

### **Problema: Aplicação não inicia**

**Causa Comum:** `JWT_SECRET` não configurado

**Solução:**
```bash
railway variables set JWT_SECRET="SuaChaveSecretaSuperSegura123"
railway restart
```

### **Problema: Erro de conexão com PostgreSQL**

**Causa:** `DATABASE_URL` não foi injetado

**Solução:**
1. Verifique se o PostgreSQL está no mesmo projeto
2. No serviço da aplicação, vá em **Variables**
3. Verifique se `DATABASE_URL` aparece na lista (referência ao PostgreSQL)
4. Se não aparecer, reconecte:
   - Settings → Connect → PostgreSQL

### **Problema: Tabelas não foram criadas**

**Verificar:**
```bash
railway logs | grep "PostgreSQL"
```

Deve aparecer:
```
Usando PostgreSQL (Railway): [host]
Criando/atualizando banco de dados PostgreSQL...
Banco de dados PostgreSQL pronto!
```

**Solução Manual (se necessário):**
1. Acesse o PostgreSQL Data tab
2. Execute o script `criar-tabelas-postgres.sql`

### **Problema: Erro 500 ao fazer login**

**Verificar:**
```bash
railway logs | grep "admin"
```

**Solução:**
```bash
railway restart
```

O usuário admin será criado automaticamente no próximo start.

---

## 📈 Otimizações Pós-Deploy

### **1. Configurar Health Checks**

Railway já monitora automaticamente a porta 8080.

Endpoint de health (opcional):
```
GET /api/auth/health
```

### **2. Backups do PostgreSQL**

Railway faz backups automáticos diariamente.

Para backup manual:
1. PostgreSQL service → Data
2. Export → Download SQL

### **3. Monitorar Uso de Recursos**

No Dashboard Railway:
- CPU Usage (aplicação)
- Memory Usage (aplicação)
- PostgreSQL Metrics (banco)
- Bandwidth

### **4. Configurar Logs Externos (Opcional)**

Integre com:
- Sentry (erros)
- Logtail (logs)
- DataDog (APM)

---

## 💰 Custos

**Railway Free Tier:**
- $5 de crédito gratuito/mês
- Suficiente para:
  - 1 aplicação pequena
  - 1 banco PostgreSQL

**Estimativa mensal (após créditos):**
- Aplicação: ~$3-5/mês
- PostgreSQL: ~$5-10/mês
- **Total:** ~$8-15/mês

**Para reduzir custos:**
- Use sleep mode (aplicação "dorme" após inatividade)
- Ajuste recursos do PostgreSQL conforme necessário

---

## 🔐 Segurança

### **Checklist de Segurança:**

- [ ] Alterar senha padrão (admin/admin123)
- [ ] Usar JWT_SECRET forte (32+ caracteres)
- [ ] HTTPS habilitado (Railway fornece automaticamente)
- [ ] PostgreSQL não exposto publicamente (padrão do Railway)
- [ ] Backups automáticos habilitados
- [ ] Não commitar .env ou credenciais
- [ ] Configurar CORS apropriadamente para produção

---

## 📞 Suporte

### **Documentação Railway:**
- https://docs.railway.app
- https://docs.railway.app/databases/postgresql

### **Logs da Aplicação:**
```bash
railway logs
```

### **Conectar ao PostgreSQL localmente:**
```bash
# Obter connection string
railway variables | grep DATABASE_URL

# Conectar com psql
psql [DATABASE_URL]
```

### **Status do Railway:**
- https://status.railway.app

---

## ✅ Checklist Completo de Deploy

```
Preparação:
[ ] Código commitado no GitHub
[ ] Dockerfile configurado
[ ] .gitignore atualizado
[ ] Suporte PostgreSQL adicionado

Deploy:
[ ] Projeto criado no Railway
[ ] Repositório conectado
[ ] PostgreSQL adicionado ao projeto
[ ] DATABASE_URL conectado à aplicação
[ ] JWT_SECRET configurado
[ ] Build concluído com sucesso
[ ] Aplicação acessível via URL

Verificação:
[ ] Logs mostram "Usando PostgreSQL (Railway)"
[ ] Tabelas criadas automaticamente
[ ] Login testado (admin/admin123)
[ ] Upload de foto testado
[ ] Histórico funcionando
[ ] Dados persistem após redeploy

Pós-Deploy:
[ ] Senha padrão alterada
[ ] Domínio customizado configurado (opcional)
[ ] Backups verificados
[ ] Monitoramento configurado
```

---

## 🎉 Deploy Concluído!

Sua aplicação estará disponível em:
```
https://seu-projeto.up.railway.app
```

**Login:**
- Usuário: admin
- Senha: admin123

⚠️ **Lembre-se de alterar a senha em produção!**

---

## 🔄 Diferenças entre SQLite, PostgreSQL e SQL Server

| Característica | SQLite (Local) | PostgreSQL (Railway) | SQL Server |
|---------------|----------------|----------------------|------------|
| **Setup** | Automático | Automático no Railway | Manual (DBA) |
| **Persistência** | Arquivo local | Volume Railway | Servidor remoto |
| **Backup** | Manual | Automático (Railway) | Manual/Script |
| **Performance** | Baixa | Alta | Alta |
| **Custo** | Grátis | ~$5-10/mês | Variável |
| **Escalabilidade** | Limitada | Alta | Alta |
| **Recomendado para** | Desenvolvimento | Produção no Railway | Produção enterprise |

---

**Desenvolvido para PicStone Qualidade**
