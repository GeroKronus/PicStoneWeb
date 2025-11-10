# Deployment no Railway

## Variáveis de Ambiente Necessárias

Configure estas variáveis no Railway (Settings → Variables):

```bash
# DATABASE (Railway injeta automaticamente ao adicionar PostgreSQL)
# DATABASE_URL=postgres://user:pass@host:port/db

# SMTP (Zoho)
SMTP_HOST=smtppro.zoho.com
SMTP_PORT=465
SMTP_USER=contato@picstone.com.br
SMTP_PASSWORD=<sua-senha-zoho>
EMAIL_FROM=contato@picstone.com.br
ADMIN_EMAIL=rogerio@picstone.com.br

# URLs
NEXTAUTH_URL=https://mobile.picstone.com.br
PUBLIC_URL=https://mobile.picstone.com.br

# JWT (IMPORTANTE: Use uma chave forte diferente desta)
JWT_SECRET=<gere-uma-chave-secreta-forte>

# Upload
UPLOAD_PATH=./uploads
```

## Passos para Deploy

1. **Conectar Repositório GitHub ao Railway**
   - Faça push do código para o GitHub
   - No Railway, crie novo projeto e conecte ao repositório

2. **Adicionar PostgreSQL**
   - No Railway, clique em "+ New" → "Database" → "PostgreSQL"
   - Isso criará automaticamente a variável `DATABASE_URL`

3. **Configurar Variáveis de Ambiente**
   - Vá em Settings → Variables
   - Adicione todas as variáveis listadas acima

4. **Deploy Automático**
   - O Railway fará deploy automaticamente a cada push no GitHub

## Observações

- O código detecta automaticamente se está no Railway via `DATABASE_URL`
- Usa PostgreSQL no Railway e SQLite no desenvolvimento local
- As migrations são aplicadas automaticamente no startup (EnsureCreatedAsync)
- Arquivos de upload NÃO são persistentes entre deploys (use S3 ou volume persistente se necessário)

## Email

O serviço de email funciona com Zoho SMTP (porta 465, SSL).
- Envio de verificação de email no cadastro
- Notificação ao admin sobre novas solicitações
- Email de aprovação/rejeição de usuários
