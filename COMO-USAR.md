# 🚀 Como Usar os Scripts do PicStone

## 🔍 Você está usando PowerShell ou CMD?

### ✅ Se estiver no PowerShell (terminal azul/roxo):
Use os arquivos `.ps1`

### ✅ Se estiver no CMD (terminal preto):
Use os arquivos `.bat`

---

## 💻 PowerShell (Recomendado)

### 1️⃣ Habilitar Execução de Scripts (só precisa fazer UMA VEZ)

**Opção A: Automático**
```powershell
.\habilitar-powershell.bat
```

**Opção B: Manual**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 2️⃣ Executar o Menu

```powershell
.\MENU.ps1
```

### 3️⃣ Ou Iniciar Diretamente o Servidor

```powershell
.\iniciar-local.ps1
```

---

## 🪟 Command Prompt (CMD)

### Executar o Menu

```cmd
MENU.bat
```

### Ou Iniciar Diretamente o Servidor

```cmd
iniciar-local.bat
```

---

## ⚠️ Erro Comum: "Execução de Scripts Desabilitada"

Se aparecer este erro no PowerShell:

```
arquivo não pode ser carregado porque a execução de scripts foi desabilitada neste sistema
```

**Solução:**

1. Execute como Administrador:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

2. Ou use o arquivo de ajuda:
   ```cmd
   habilitar-powershell.bat
   ```

3. Depois execute novamente:
   ```powershell
   .\MENU.ps1
   ```

---

## 📋 Scripts Disponíveis

### Para PowerShell (.ps1)
- **MENU.ps1** - Menu interativo completo
- **iniciar-local.ps1** - Inicia o servidor

### Para CMD (.bat)
- **MENU.bat** - Menu interativo completo
- **iniciar-local.bat** - Inicia o servidor
- **parar-servidor.bat** - Para o servidor
- **testar-conexao-sql.bat** - Testa SQL Server
- **descobrir-ip.bat** - Mostra IP local
- **limpar-build.bat** - Limpa arquivos temporários
- **abrir-vscode.bat** - Abre no VS Code
- **habilitar-powershell.bat** - Habilita scripts PS

---

## 🎯 Modo Manual (Sem Scripts)

Se preferir não usar scripts, execute manualmente:

```powershell
# Navegue até o Backend
cd Backend

# Restaure dependências
dotnet restore

# Execute o servidor
dotnet run
```

Acesse: http://localhost:5000

---

## 🐛 Problemas e Soluções

### Problema 1: "dotnet: comando não encontrado"
**Solução:** Instale o .NET 8.0 SDK
- https://dotnet.microsoft.com/download/dotnet/8.0

### Problema 2: "Porta 5000 já está em uso"
**Solução PowerShell:**
```powershell
Get-Process -Name "dotnet" | Stop-Process -Force
```

**Solução CMD:**
```cmd
parar-servidor.bat
```

### Problema 3: Scripts não executam no PowerShell
**Solução:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Problema 4: Arquivos .bat dão erro estranho no PowerShell
**Solução:** Use os arquivos `.ps1` em vez dos `.bat`
```powershell
.\MENU.ps1  # ✅ Correto
# NÃO USE: .\MENU.bat no PowerShell
```

---

## 📱 Testar no Celular

### 1️⃣ Inicie o servidor
```powershell
.\MENU.ps1
# Escolha opção [1]
```

### 2️⃣ Descubra seu IP
```powershell
.\MENU.ps1
# Escolha opção [4]
```

### 3️⃣ No celular, acesse
```
http://SEU_IP:5000
```

**Exemplo:**
```
http://192.168.1.100:5000
```

⚠️ **Importante:** Celular e PC devem estar na mesma rede Wi-Fi!

---

## 🎨 Diferenças entre PowerShell e CMD

| Característica | PowerShell (.ps1) | CMD (.bat) |
|---------------|-------------------|------------|
| **Cores** | ✅ Suporta cores | ⚠️ Limitado |
| **Emojis** | ✅ Suporta | ❌ Problemas |
| **Funcionalidades** | ✅ Todas | ✅ Todas |
| **Performance** | ✅ Mais rápido | ⚠️ OK |
| **Moderno** | ✅ Sim | ❌ Legado |

**Recomendação:** Use PowerShell (`.ps1`) para melhor experiência!

---

## ✅ Checklist de Primeiro Uso

- [ ] Instalar .NET 8.0 SDK
- [ ] Habilitar scripts PowerShell (se usar .ps1)
- [ ] Executar `.\MENU.ps1` ou `MENU.bat`
- [ ] Escolher opção [1] - Iniciar Servidor
- [ ] Acessar http://localhost:5000
- [ ] Login: admin / admin123
- [ ] Testar captura de foto

---

## 📚 Mais Informações

- **README.md** - Documentação técnica completa
- **GUIA_RAPIDO.md** - Deploy no Railway
- **SCRIPTS.md** - Detalhes de cada script
- **LEIA-ME-PRIMEIRO.txt** - Início rápido

---

**Dica:** Para facilitar, adicione aos favoritos o comando:

```powershell
# PowerShell
.\MENU.ps1
```

ou

```cmd
REM CMD
MENU.bat
```

Bom desenvolvimento! 🚀
