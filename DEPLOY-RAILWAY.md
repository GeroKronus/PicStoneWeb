# 🚀 Deploy no Railway - PicStone

## 📋 Pré-requisitos

- Conta no Railway: https://railway.app
- Repositório GitHub: https://github.com/GeroKronus/PicStoneWeb
- Acesso ao SQL Server (se usar SQL Server em vez de SQLite)

---

## 🎯 Opção 1: Deploy via Dashboard Railway (Recomendado)

### **Passo 1: Criar Novo Projeto**

1. Acesse: https://railway.app
2. Clique em **"New Project"**
3. Selecione **"Deploy from GitHub repo"**
4. Escolha o repositório: **GeroKronus/PicStoneWeb**
5. Aguarde a detecção automática do Dockerfile

### **Passo 2: Configurar Variáveis de Ambiente**

No painel do Railway, vá em **Variables** e adicione:

#### **Opção A: Usar SQLite (Desenvolvimento/Teste)**

```env
USE_SQLITE=true
JWT_SECRET=SuaChaveSecretaSuperSegura123!@#$%
UPLOAD_PATH=/app/uploads
```

#### **Opção B: Usar SQL Server (Produção)**

```env
USE_SQLITE=false
SQL_CONNECTION_STRING=Data Source=131.255.255.16,11433;Initial Catalog=DADOSADV_Q;User ID=PicStoneQualita;Password=@PicQualit@Stone#;TrustServerCertificate=True;Encrypt=False;
JWT_SECRET=SuaChaveSecretaSuperSegura123!@#$%
UPLOAD_PATH=/app/uploads
```

#### **Variáveis Opcionais (FTP)**

```env
FTP_SERVER=ftp.seuservidor.com
FTP_USER=usuario_ftp
FTP_PASSWORD=senha_ftp
```

### **Passo 3: Deploy**

1. Railway irá automaticamente:
   - Detectar o Dockerfile
   - Fazer build da imagem
   - Criar container
   - Expor na porta 8080
   - Gerar URL pública

2. Aguarde o deploy concluir (~3-5 minutos)

3. Acesse a URL gerada pelo Railway

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

### **Passo 3: Link com Repositório**

```bash
cd "D:\Claude Code\PicStone WEB"
railway link
```

Selecione o projeto ou crie um novo.

### **Passo 4: Configurar Variáveis**

```bash
# SQLite (desenvolvimento)
railway variables set USE_SQLITE=true
railway variables set JWT_SECRET="SuaChaveSecretaSuperSegura123"
railway variables set UPLOAD_PATH="/app/uploads"

# OU SQL Server (produção)
railway variables set USE_SQLITE=false
railway variables set SQL_CONNECTION_STRING="Data Source=..."
railway variables set JWT_SECRET="SuaChaveSecretaSuperSegura123"
```

### **Passo 5: Deploy**

```bash
railway up
```

---

## 📊 Configuração do Banco de Dados

### **Opção A: SQLite (Mais Simples)**

✅ **Vantagens:**
- Sem configuração adicional
- Banco criado automaticamente
- Ideal para desenvolvimento/teste
- Sem custo adicional

⚠️ **Limitações:**
- Dados são perdidos se container reiniciar
- Não recomendado para produção crítica

**Para persistir dados:**
Railway oferece volumes persistentes:
1. Settings → Volumes
2. Add Volume: `/app/data`
3. Ajustar `appsettings.json` para salvar DB em `/app/data/picstone.db`

### **Opção B: SQL Server (Produção)**

✅ **Vantagens:**
- Dados persistentes
- Melhor performance
- Produção-ready

**Requisitos:**
1. DBA deve executar: `criar-tabelas.sql`
2. Configurar `SQL_CONNECTION_STRING`
3. Definir `USE_SQLITE=false`

---

## 🔐 Criar Usuário Admin Após Deploy

### **Método 1: Automático (SQLite)**

O sistema cria automaticamente:
- **Usuário:** admin
- **Senha:** admin123

⚠️ **ALTERE A SENHA EM PRODUÇÃO!**

### **Método 2: Manual (SQL Server)**

Se usar SQL Server, o usuário é criado pelo script `criar-tabelas.sql`.

---

## 🌐 Configurar Domínio Customizado (Opcional)

1. No Railway, vá em **Settings**
2. Clique em **Generate Domain** (Railway fornece gratuitamente)
3. Ou adicione seu domínio customizado:
   - Settings → Custom Domain
   - Adicione seu domínio
   - Configure DNS conforme instruções

---

## 📱 Testar o Deploy

### **1. Acessar a Aplicação**

```
https://seu-projeto.railway.app
```

### **2. Fazer Login**

- **Usuário:** admin
- **Senha:** admin123

### **3. Testar Upload de Foto**

- Tire uma foto
- Preencha: Lote, Chapa, Processo
- Envie

### **4. Verificar Logs**

No Railway Dashboard:
- View Logs
- Ou via CLI: `railway logs`

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

---

## 🐛 Troubleshooting

### **Problema: Build Falhou**

**Solução:**
```bash
# Ver logs do build
railway logs --deployment

# Verificar Dockerfile
# Certificar que todos os arquivos necessários existem
```

### **Problema: Aplicação não inicia**

**Causa Comum:** Variáveis de ambiente faltando

**Solução:**
```bash
railway variables

# Adicionar variáveis necessárias
railway variables set USE_SQLITE=true
railway variables set JWT_SECRET="sua-chave"
```

### **Problema: Erro 500 ao fazer login**

**Causa:** Banco de dados não configurado

**Solução SQLite:**
```bash
railway variables set USE_SQLITE=true
railway restart
```

**Solução SQL Server:**
1. Verificar `SQL_CONNECTION_STRING`
2. DBA deve ter executado `criar-tabelas.sql`
3. Testar conexão com servidor

### **Problema: Frontend não carrega**

**Causa:** Arquivos frontend não copiados

**Verificar Dockerfile:**
```dockerfile
COPY Frontend/ ./wwwroot/
```

---

## 📈 Otimizações Pós-Deploy

### **1. Configurar Health Checks**

Railway oferece health checks automáticos:
- Endpoint: `/api/auth/health`

### **2. Configurar Backups (SQL Server)**

Configure backups automáticos do SQL Server.

### **3. Monitorar Uso de Recursos**

No Dashboard Railway:
- CPU Usage
- Memory Usage
- Bandwidth

### **4. Configurar Logs Externos (Opcional)**

Integre com:
- Sentry (erros)
- Logtail (logs)
- DataDog (APM)

---

## 💰 Custos

**Railway oferece:**
- $5 de crédito gratuito/mês
- Pay-as-you-go após créditos

**Estimativa mensal:**
- Aplicação básica: ~$5-10/mês
- Inclui: 512MB RAM, 1GB disco

---

## 🔐 Segurança

### **Checklist de Segurança:**

- [ ] Alterar senha padrão (admin/admin123)
- [ ] Usar JWT_SECRET forte (32+ caracteres)
- [ ] Configurar CORS apropriadamente
- [ ] Usar HTTPS (Railway fornece automaticamente)
- [ ] Não commitar .env ou credenciais
- [ ] Configurar backups regulares

---

## 📞 Suporte

### **Documentação Railway:**
- https://docs.railway.app

### **Logs da Aplicação:**
```bash
railway logs
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
[ ] Variáveis de ambiente definidas

Deploy:
[ ] Projeto criado no Railway
[ ] Repositório conectado
[ ] Variáveis configuradas
[ ] Build concluído com sucesso
[ ] Aplicação acessível via URL

Pós-Deploy:
[ ] Login testado (admin/admin123)
[ ] Upload de foto testado
[ ] Histórico funcionando
[ ] Logs verificados
[ ] Senha padrão alterada

Produção (se usar SQL Server):
[ ] DBA executou criar-tabelas.sql
[ ] Conexão SQL testada
[ ] USE_SQLITE=false configurado
```

---

## 🎉 Deploy Concluído!

Sua aplicação estará disponível em:
```
https://seu-projeto.railway.app
```

**Login:**
- Usuário: admin
- Senha: admin123

⚠️ **Lembre-se de alterar a senha em produção!**

---

**Desenvolvido para PicStone Qualidade**
