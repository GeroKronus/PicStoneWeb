# 🚀 DEPLOY RÁPIDO NO RAILWAY

## ✅ Código já está no GitHub!

**Repositório:** https://github.com/GeroKronus/PicStoneWeb

---

## 🎯 PASSOS PARA FAZER DEPLOY AGORA

### **1. Acesse o Railway**

Vá para: https://railway.app

### **2. Criar Novo Projeto**

1. Clique em **"New Project"**
2. Selecione **"Deploy from GitHub repo"**
3. Se solicitado, autorize acesso ao GitHub
4. Selecione o repositório: **GeroKronus/PicStoneWeb**
5. Aguarde Railway detectar o Dockerfile

### **3. Adicionar PostgreSQL**

No painel do projeto:

1. Clique em **"+ New"**
2. Selecione **"Database"**
3. Escolha **"Add PostgreSQL"**
4. Railway criará o banco automaticamente

### **4. Conectar PostgreSQL ao Aplicativo**

1. Clique no serviço da **aplicação** (PicStoneWeb)
2. Vá em **"Variables"**
3. Verifique se `DATABASE_URL` aparece na lista
   - Se **SIM**: está conectado automaticamente ✅
   - Se **NÃO**: conecte manualmente:
     - Settings → Connect → Selecione o PostgreSQL

### **5. Adicionar Variáveis de Ambiente**

No serviço da aplicação, vá em **"Variables"** e adicione:

```
JWT_SECRET=PicStone2025SuperSecretKey!@#
UPLOAD_PATH=/app/uploads
```

**Importante:** NÃO adicione `DATABASE_URL` manualmente - Railway faz isso automaticamente!

### **6. Aguardar Deploy**

Railway irá automaticamente:
- ✅ Detectar Dockerfile
- ✅ Fazer build da aplicação
- ✅ Conectar ao PostgreSQL
- ✅ Criar tabelas automaticamente
- ✅ Criar usuário admin (admin/admin123)
- ✅ Expor aplicação na porta 8080
- ✅ Gerar URL pública

**Tempo estimado:** 3-5 minutos

### **7. Obter URL da Aplicação**

1. No painel da aplicação, vá em **"Settings"**
2. Clique em **"Generate Domain"**
3. Railway criará uma URL como: `https://picstone-web-production.up.railway.app`

---

## 📱 TESTAR A APLICAÇÃO

### **Acessar:**
```
https://seu-dominio.up.railway.app
```

### **Fazer Login:**
- **Usuário:** admin
- **Senha:** admin123

⚠️ **ALTERE A SENHA EM PRODUÇÃO!**

### **Testar Upload:**
1. Tire uma foto
2. Preencha: Lote, Chapa, Processo
3. Clique em "Enviar Foto"
4. Verifique no histórico

---

## 🔍 VERIFICAR LOGS

### **Via Dashboard:**
1. Clique no serviço da aplicação
2. Vá em **"Logs"**
3. Procure por:
   ```
   Usando PostgreSQL (Railway)
   Criando/atualizando banco de dados PostgreSQL...
   Banco de dados PostgreSQL pronto!
   Usuario admin verificado/criado com sucesso
   Iniciando PicStone Foto API na porta 8080
   ```

### **Via CLI:**
```bash
# Instalar Railway CLI
npm i -g @railway/cli

# Login
railway login

# Ver logs
railway logs --follow
```

---

## 🐛 TROUBLESHOOTING RÁPIDO

### **Problema: Build falhou**
**Solução:**
- Verifique logs do deploy
- Confirme que o Dockerfile existe no repositório

### **Problema: Aplicação não inicia**
**Solução:**
- Verifique se `JWT_SECRET` foi configurado
- Reinicie: Settings → Restart

### **Problema: Erro ao conectar PostgreSQL**
**Solução:**
1. Verifique se PostgreSQL está no mesmo projeto
2. Confirme que `DATABASE_URL` aparece nas variáveis
3. Se não aparecer: Settings → Connect → PostgreSQL

### **Problema: 404 na página inicial**
**Solução:**
- Aguarde alguns minutos (build pode estar em progresso)
- Verifique logs: `railway logs`
- Reinicie: Settings → Restart

---

## 📊 VERIFICAR BANCO DE DADOS

### **Acessar PostgreSQL:**
1. Clique no serviço PostgreSQL
2. Vá em **"Data"**
3. Execute consultas:

```sql
-- Ver usuários
SELECT * FROM "Usuarios";

-- Ver fotos
SELECT * FROM "FotosMobile" ORDER BY "DataUpload" DESC;

-- Contar registros
SELECT
    (SELECT COUNT(*) FROM "Usuarios") as usuarios,
    (SELECT COUNT(*) FROM "FotosMobile") as fotos;
```

---

## 🔄 ATUALIZAÇÕES FUTURAS

Quando você fizer mudanças no código:

```bash
git add .
git commit -m "Descrição da mudança"
git push origin main
```

Railway detectará automaticamente e fará redeploy! 🚀

---

## 📞 AJUDA

### **Documentação Completa:**
- **[DEPLOY-RAILWAY-POSTGRES.md](DEPLOY-RAILWAY-POSTGRES.md)** - Guia detalhado
- **[README.md](README.md)** - Documentação do projeto

### **Railway Docs:**
- https://docs.railway.app
- https://docs.railway.app/databases/postgresql

### **Suporte Railway:**
- https://railway.app/discord

---

## ✅ CHECKLIST DE DEPLOY

```
[ ] Acessei Railway (railway.app)
[ ] Criei novo projeto
[ ] Selecionei repositório GeroKronus/PicStoneWeb
[ ] Adicionei PostgreSQL
[ ] PostgreSQL conectado à aplicação (DATABASE_URL)
[ ] Configurei JWT_SECRET
[ ] Build concluído com sucesso
[ ] Gerei domínio público
[ ] Testei login (admin/admin123)
[ ] Testei upload de foto
[ ] Verifiquei histórico
[ ] ALTEREI SENHA PADRÃO
```

---

## 🎉 PRONTO!

Sua aplicação PicStone está no ar! 🚀

**URL do repositório:**
https://github.com/GeroKronus/PicStoneWeb

**Próximo passo:**
Acesse Railway e faça o deploy seguindo os passos acima.

---

**Desenvolvido para PicStone Qualidade**
