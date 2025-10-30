# 🛠️ Scripts de Automação (Arquivos .BAT)

Este projeto inclui diversos scripts `.bat` para facilitar o desenvolvimento e teste local no Windows.

## 📋 Menu Principal

### **MENU.bat**
Menu interativo centralizado com todas as funcionalidades.

```
╔════════════════════════════════════════╗
║  📸 PicStone - Menu Principal          ║
╠════════════════════════════════════════╣
║  [1] 🚀 Iniciar Servidor Local         ║
║  [2] 🛑 Parar Servidor                 ║
║  [3] 🔌 Testar Conexão SQL Server      ║
║  [4] 📱 Descobrir IP para Celular      ║
║  [5] 🧹 Limpar Build                   ║
║  [6] 💻 Abrir no VS Code               ║
║  [7] 📚 Abrir Swagger                  ║
║  [8] 🌐 Abrir Aplicação                ║
║  [9] 📖 Ver Documentação               ║
║  [0] ❌ Sair                           ║
╚════════════════════════════════════════╝
```

**Como usar:** Clique duas vezes em `MENU.bat`

---

## 🚀 Scripts Individuais

### **iniciar-local.bat**
Inicia o servidor de desenvolvimento local.

**O que faz:**
1. ✅ Verifica se o .NET SDK está instalado
2. ✅ Restaura dependências do NuGet
3. ✅ Compila o projeto
4. ✅ Inicia o servidor na porta 5000

**Resultado:**
```
🌐 URL Local:        http://localhost:5000
📱 Celular:          http://SEU_IP:5000
👤 Usuário:          admin
🔑 Senha:            admin123
📚 Swagger:          http://localhost:5000/swagger
```

---

### **parar-servidor.bat**
Para todos os processos do servidor ASP.NET Core.

**O que faz:**
- Encerra todos os processos `dotnet.exe` em execução

**Quando usar:**
- Quando o servidor travou
- Para forçar reinicialização
- Quando esqueceu de fechar o terminal

---

### **testar-conexao-sql.bat**
Testa a conectividade com o SQL Server.

**O que faz:**
- Usa PowerShell `Test-NetConnection` para testar porta 11433
- Verifica se `131.255.255.16:11433` está acessível

**Resultado esperado:**
```
✅ Conexão bem-sucedida! O servidor SQL está acessível.
```

**Ou em caso de falha:**
```
❌ Falha na conexão! Verifique:
   1. Firewall está bloqueando a porta
   2. SQL Server está rodando
   3. SQL Server aceita conexões remotas
```

---

### **descobrir-ip.bat**
Mostra todos os endereços IPv4 da sua máquina.

**O que faz:**
- Lista todos os IPs IPv4 das interfaces de rede
- Explica como usar para acesso mobile

**Exemplo de saída:**
```
📱 Seus endereços IP:
   IPv4 Address: 192.168.1.100
   IPv4 Address: 10.0.0.5

Para acessar do celular:
   http://192.168.1.100:5000
```

**Requisitos:**
- Celular e PC na mesma rede Wi-Fi
- Firewall liberando porta 5000

---

### **limpar-build.bat**
Remove arquivos temporários de compilação.

**O que remove:**
- 📁 `Backend/bin/`
- 📁 `Backend/obj/`
- 📄 `Backend/logs/*.log`
- 📸 `Backend/uploads/*.jpg|jpeg|png`

**Quando usar:**
- Antes de commitar no Git
- Quando há erros de compilação estranhos
- Para forçar rebuild completo

---

### **abrir-vscode.bat**
Abre o projeto no Visual Studio Code.

**O que faz:**
- Executa `code .` no diretório raiz do projeto

**Requisitos:**
- Visual Studio Code instalado
- Comando `code` no PATH do sistema

**Se não funcionar:**
1. Abra o VSCode
2. Pressione `Ctrl+Shift+P`
3. Digite "shell command"
4. Selecione "Install 'code' command in PATH"

---

## 🔍 Casos de Uso Comuns

### Desenvolvimento Diário

```batch
1. Clique em MENU.bat
2. Opção [1] - Iniciar Servidor
3. Opção [8] - Abrir Aplicação
```

### Testar no Celular

```batch
1. Clique em MENU.bat
2. Opção [1] - Iniciar Servidor
3. Opção [4] - Descobrir IP
4. No celular: http://SEU_IP:5000
```

### Resolver Problemas de Conexão

```batch
1. Clique em MENU.bat
2. Opção [3] - Testar Conexão SQL
3. Se falhar, verifique firewall e credenciais
```

### Limpar e Recompilar

```batch
1. Clique em MENU.bat
2. Opção [2] - Parar Servidor
3. Opção [5] - Limpar Build
4. Opção [1] - Iniciar Servidor (recompila)
```

---

## 🐛 Troubleshooting

### "dotnet: command not found"
**Causa:** .NET SDK não instalado

**Solução:**
1. Baixe o .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instale e reinicie o terminal
3. Execute novamente

### "Porta 5000 já está em uso"
**Causa:** Outro processo usando a porta

**Solução:**
```batch
# Use parar-servidor.bat
# Ou manualmente:
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

### "Não é possível conectar ao SQL Server"
**Causa:** Firewall, credenciais ou servidor offline

**Solução:**
1. Execute `testar-conexao-sql.bat`
2. Verifique as credenciais em `Backend/appsettings.json`
3. Verifique se o servidor SQL está rodando

### Scripts não funcionam no Windows 11
**Causa:** Política de execução

**Solução:**
1. Clique com botão direito no arquivo .bat
2. Selecione "Executar como administrador"
3. Ou desabilite SmartScreen temporariamente

---

## 📝 Notas

- Todos os scripts usam codificação UTF-8 (`chcp 65001`)
- Cores personalizadas para cada script
- Mensagens em português
- Validações antes de executar operações

---

## 🔧 Personalização

Para modificar os scripts:

1. Clique com botão direito no arquivo `.bat`
2. Selecione "Editar" ou "Abrir com Notepad"
3. Salve as alterações
4. Execute novamente

**Exemplo - Mudar porta padrão:**

Edite `iniciar-local.bat` e adicione antes de `dotnet run`:
```batch
set ASPNETCORE_URLS=http://localhost:8080
```

---

**Desenvolvido para facilitar o desenvolvimento do PicStone! 🚀**
