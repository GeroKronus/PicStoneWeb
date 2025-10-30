# 🚀 Guia Rápido - PicStone Foto Mobile

## 📋 Checklist de Deploy

### ✅ Desenvolvimento Local

```bash
# 1. Navegue até o projeto
cd "D:\Claude Code\PicStone WEB\Backend"

# 2. Instale as dependências
dotnet restore

# 3. Execute a aplicação
dotnet run

# 4. Acesse no navegador
# http://localhost:5000
# Login: admin / admin123
```

### ✅ Deploy Railway (Primeira vez)

```bash
# 1. Instale Railway CLI
npm i -g @railway/cli

# 2. Login no Railway
railway login

# 3. Crie novo projeto
cd "D:\Claude Code\PicStone WEB"
railway init

# 4. Configure as variáveis de ambiente no painel do Railway:
# - SQL_CONNECTION_STRING
# - JWT_SECRET
# - UPLOAD_PATH=/app/uploads
# - FTP_SERVER (opcional)
# - FTP_USER (opcional)
# - FTP_PASSWORD (opcional)

# 5. Faça o deploy
railway up

# 6. Pegue a URL pública
railway domain
```

### ✅ Atualizações Futuras

```bash
# Fazer deploy de novas alterações
cd "D:\Claude Code\PicStone WEB"
railway up
```

## 🔑 Credenciais Padrão

- **Usuário:** admin
- **Senha:** admin123

⚠️ **IMPORTANTE:** Altere a senha em produção!

## 📊 Testar Endpoints

### Teste de Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### Upload de Foto (com token)
```bash
curl -X POST http://localhost:5000/api/fotos/upload \
  -H "Authorization: Bearer SEU_TOKEN_AQUI" \
  -F "Arquivo=@foto.jpg" \
  -F "Lote=12345" \
  -F "Chapa=001" \
  -F "Processo=Polimento" \
  -F "Espessura=20"
```

### Histórico
```bash
curl http://localhost:5000/api/fotos/historico?limite=10 \
  -H "Authorization: Bearer SEU_TOKEN_AQUI"
```

## 🔧 Comandos Úteis

### Verificar se o SQL Server está acessível
```bash
# Windows
Test-NetConnection -ComputerName 131.255.255.16 -Port 11433

# Linux/Mac
nc -zv 131.255.255.16 11433
```

### Criar nova migration (se modificar Models)
```bash
cd Backend
dotnet ef migrations add NomeDaMigracao
dotnet ef database update
```

### Ver logs do Railway
```bash
railway logs
```

### Conectar ao shell do container Railway
```bash
railway run bash
```

## 📱 Testar no Celular

1. Execute localmente na sua máquina
2. Descubra seu IP local: `ipconfig` (Windows) ou `ifconfig` (Linux/Mac)
3. No celular, acesse: `http://SEU_IP_LOCAL:5000`
4. Certifique-se de que celular e PC estão na mesma rede Wi-Fi

## 🐛 Problemas Comuns

### Erro: "Cannot connect to SQL Server"
- Verifique firewall da máquina
- Confirme que o SQL Server aceita conexões remotas
- Valide a string de conexão

### Erro: "JWT token invalid"
- Token expirou (8 horas)
- Faça login novamente

### Frontend não carrega
- Verifique se os arquivos estão em `Backend/wwwroot/`
- No Dockerfile, veja se `COPY Frontend/ ./wwwroot/` foi executado

### Upload falha
- Verifique tamanho do arquivo (máx 10MB)
- Valide formato (JPG, JPEG, PNG)
- Confirme que diretório `uploads/` existe e tem permissões

## 📞 Próximos Passos

1. ✅ Deploy no Railway
2. ⬜ Configurar domínio customizado
3. ⬜ Adicionar mais usuários via SQL
4. ⬜ Configurar backup automático de fotos
5. ⬜ Implementar PWA (service worker)
6. ⬜ Adicionar notificações push

## 🎨 Personalização

### Mudar cores do frontend
Edite `Frontend/style.css`:
```css
:root {
    --primary-color: #2563eb;  /* Cor principal */
    --success-color: #10b981;  /* Cor de sucesso */
}
```

### Adicionar novos processos
Edite `Backend/Controllers/FotosController.cs`:
```csharp
[HttpGet("processos")]
public IActionResult Processos()
{
    return Ok(new[]
    {
        "Polimento",
        "Resina",
        "Acabamento",
        "SEU_NOVO_PROCESSO"
    });
}
```

## 📖 Documentação Completa

Veja `README.md` para documentação detalhada.
