# 🔧 CONECTAR POSTGRESQL AO PICSTONEWEB NO RAILWAY

## ❌ **PROBLEMA IDENTIFICADO:**

O PostgreSQL existe no Railway, mas **NÃO ESTÁ CONECTADO** ao serviço PicStoneWeb!

Por isso:
- ❌ A variável `DATABASE_URL` não está disponível
- ❌ A aplicação está usando SQLite (fallback)
- ❌ As imagens estão sendo salvas em armazenamento efêmero
- ❌ Os dados serão perdidos no próximo redeploy

---

## ✅ **SOLUÇÃO: Conectar PostgreSQL ao Serviço**

### **Passo 1: Acessar o Railway**

1. Acesse: https://railway.app
2. Abra o projeto **PicStoneWeb**

### **Passo 2: Verificar se PostgreSQL Existe**

Você deve ver:
- 🟦 **PicStoneWeb** (seu serviço)
- 🟩 **PostgreSQL** (banco de dados)

**Se NÃO vê o PostgreSQL:**
1. Clique em **"+ New"**
2. Selecione **"Database"**
3. Escolha **"Add PostgreSQL"**
4. Aguarde a criação (~1 minuto)

### **Passo 3: Conectar PostgreSQL ao PicStoneWeb**

#### **Método 1: Via Settings (Recomendado)**

1. Clique no serviço **PicStoneWeb**
2. Vá em **"Settings"**
3. Role até **"Service Variables"** ou **"Connect"**
4. Procure por **"Connect to a service"** ou **"Add Reference"**
5. Selecione o **PostgreSQL**
6. Railway criará automaticamente a variável `DATABASE_URL`

#### **Método 2: Via Variables (Manual)**

1. Clique no serviço **PicStoneWeb**
2. Vá em **"Variables"**
3. Clique no ícone de **"Reference"** ou **"Add Variable"**
4. Selecione **"Reference from Database"**
5. Escolha: **PostgreSQL** → **DATABASE_URL**
6. Salve

### **Passo 4: Verificar Conexão**

No serviço **PicStoneWeb**, vá em **"Variables"** e confirme que existe:

```
DATABASE_URL = ${{Postgres.DATABASE_URL}}
```

Ou algo similar com referência ao PostgreSQL.

### **Passo 5: Forçar Redeploy**

1. No serviço **PicStoneWeb**
2. Menu (⋮) → **"Redeploy"**
3. Aguarde ~3-5 minutos

---

## 🔍 **COMO SABER SE ESTÁ CONECTADO:**

### **Verificar Logs:**

1. Clique em **PicStoneWeb**
2. Vá em **"Logs"**
3. Procure por:

✅ **CORRETO (PostgreSQL conectado):**
```
Usando PostgreSQL (Railway): [host].railway.internal
Criando/atualizando banco de dados PostgreSQL...
Banco de dados PostgreSQL pronto!
```

❌ **INCORRETO (SQLite sendo usado):**
```
Usando SQLite: /app/picstone.db
Criando/atualizando banco de dados SQLite...
```

---

## 📊 **ALTERNATIVA: Criar Conexão Via CLI**

Se preferir usar a CLI do Railway:

```bash
# 1. Login
railway login

# 2. Link ao projeto
cd "D:\Claude Code\PicStone WEB"
railway link

# 3. Listar serviços
railway service list

# 4. Selecionar serviço PicStoneWeb
railway service

# 5. Adicionar referência ao PostgreSQL
# (Infelizmente não há comando direto, precisa fazer via Dashboard)
```

**Recomendação:** Use o Dashboard do Railway para conectar.

---

## 🎯 **PASSO A PASSO VISUAL:**

### **1. Dashboard do Railway:**
```
┌─────────────────────────────────────┐
│  PicStoneWeb Project                │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────┐      ┌─────────┐     │
│  │ PicStone│      │Postgres │     │  ← Você deve ver estes 2
│  │  Web    │ ❌   │  SQL    │     │
│  └─────────┘      └─────────┘     │
│      ↑                              │
│      └─ PRECISA CONECTAR!          │
└─────────────────────────────────────┘
```

### **2. Após Conectar:**
```
┌─────────────────────────────────────┐
│  PicStoneWeb Project                │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────┐      ┌─────────┐     │
│  │ PicStone│──────│Postgres │     │  ← Linha conectando
│  │  Web    │  ✅  │  SQL    │     │
│  └─────────┘      └─────────┘     │
│                                     │
└─────────────────────────────────────┘
```

---

## 🐛 **TROUBLESHOOTING:**

### **Problema: Não encontro opção "Connect"**

**Solução:**
1. Clique em **PicStoneWeb**
2. Vá em **"Variables"**
3. Clique em **"+ New Variable"**
4. Procure por opção **"Reference"** ou ícone de **link/corrente**
5. Selecione o PostgreSQL

### **Problema: PostgreSQL não aparece na lista**

**Solução:**
1. Verifique se PostgreSQL foi criado no mesmo projeto
2. Se não, crie: **+ New** → **Database** → **PostgreSQL**

### **Problema: "DATABASE_URL já existe"**

**Solução:**
1. Se `DATABASE_URL` já existe mas não está conectado ao PostgreSQL
2. Delete a variável existente
3. Crie novamente como referência ao PostgreSQL

---

## ✅ **CHECKLIST:**

```
[ ] PostgreSQL criado no Railway
[ ] PicStoneWeb e PostgreSQL no mesmo projeto
[ ] DATABASE_URL aparece nas variáveis do PicStoneWeb
[ ] DATABASE_URL referencia o PostgreSQL (formato: ${{Postgres.DATABASE_URL}})
[ ] Redeploy forçado
[ ] Logs mostram "Usando PostgreSQL (Railway)"
[ ] Teste de upload de foto funciona
[ ] Dados persistem após redeploy
```

---

## 📱 **APÓS CONECTAR:**

1. Acesse: https://picstoneweb-production.up.railway.app
2. Faça login: admin/admin123
3. Tire uma nova foto
4. Veja no histórico
5. **IMPORTANTE:** As fotos antigas (SQLite) não aparecerão, apenas as novas!

---

## 💾 **SOBRE AS IMAGENS:**

### **Problema Atual:**
- Imagens estão em `/app/uploads` (armazenamento efêmero)
- Quando o container reinicia, as imagens são perdidas
- Apenas os metadados ficam no PostgreSQL

### **Soluções Futuras:**

**Opção 1: Railway Volumes (Recomendado)**
1. Settings → Volumes
2. Add Volume: `/app/uploads`
3. Persistência permanente

**Opção 2: Object Storage (S3/Cloudinary)**
- Armazenamento externo
- Mais robusto
- Custo adicional

**Por enquanto:** Aceitar que imagens são temporárias até adicionar volume.

---

## 🎉 **RESULTADO ESPERADO:**

Após conectar corretamente:

✅ PostgreSQL armazena metadados permanentemente
✅ Aplicação usa PostgreSQL em vez de SQLite
✅ Usuário admin persiste entre deploys
✅ Histórico de fotos mantido
✅ Logs mostram conexão PostgreSQL

**Imagens:** Ainda temporárias (adicione Volume para persistência).

---

**AÇÃO IMEDIATA:** Acesse Railway Dashboard e conecte o PostgreSQL ao PicStoneWeb seguindo os passos acima! 🚀
