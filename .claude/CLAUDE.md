# Sistema de Notificação Telegram

## ⚠️ IMPORTANTE: Idioma

**TODAS as suas respostas e comunicações devem ser em PORTUGUÊS (pt-BR).**

Nunca responda em inglês, mesmo após compactação de conversa.

---

## Detecção Automática de Presença

**MODO PADRÃO: REMOTO** (sempre envia notificações Telegram e monitora comandos)

**Comandos especiais do usuário:**

### 1. Se mensagem = `#remoto`:
```bash
python "D:\Script Python\Monitor ETH\modo.py" remoto
```
- Responda: "✅ Modo remoto ativado. Continuarei enviando notificações no Telegram e monitorando comandos a cada 2-3 minutos."
- **CONTINUE trabalhando normalmente no que estava fazendo**
- **Comportamento remoto:**
  - Enviar notificações pelo Telegram
  - **Verificar comandos a cada 2-3 minutos CONSTANTEMENTE**
  - Confirmar explicitamente quando receber comandos
  - **Continuar verificando mesmo se estiver OCIOSA**

### 2. Se mensagem = `#console`:
```bash
python "D:\Script Python\Monitor ETH\modo.py" console
```
- Responda: "✅ Modo console ativado. Notificações Telegram desativadas."
- Processe a mensagem normalmente
- **Comportamento console:**
  - NÃO enviará notificações pelo Telegram (usuário está presente)
  - PARA de verificar comandos periodicamente (usuário está aqui)

### 3. Se mensagem = qualquer outra coisa:
- **NÃO mude o modo atual** (mantenha remoto ou console como está)
- Processe a mensagem normalmente

---

## Quando Notificar o Usuário

**SEMPRE que finalizar tarefas ou precisar de interação:**

```bash
python "D:\Script Python\Monitor ETH\notificar_e_aguardar.py" --tipo concluido "sua mensagem"
```

**O que acontece automaticamente:**
- Se modo = REMOTO → envia pelo Telegram e aguarda resposta
- Se modo = CONSOLE → não envia, mas ainda aguarda resposta local

---

## Verificação de Comandos

**IMPORTANTE: Você só consegue verificar comandos quando está ATIVA (executando tarefas).**

### Quando você PODE verificar:

**Durante tarefas longas (builds, testes, instalações):**
- Verifique a cada 2-3 minutos:
  ```bash
  python "D:\Script Python\Monitor ETH\ler_comando.py"
  ```

**Antes de operações importantes:**
- Sempre verifique antes de fazer deploy, migrations, etc:
  ```bash
  python "D:\Script Python\Monitor ETH\ler_comando.py"
  ```

### Quando você NÃO pode verificar:

**Quando está em REPOUSO (ociosa/parada):**
- Você NÃO consegue executar comandos periodicamente
- O usuário tem um **monitor separado** rodando que alerta quando há comandos
- Quando o usuário te acordar, verifique imediatamente:
  ```bash
  python "D:\Script Python\Monitor ETH\ler_comando.py"
  ```

### Se encontrar comando:

**PRIMEIRO:** Confirme explicitamente:
```bash
python "D:\Script Python\Monitor ETH\notificar_e_aguardar.py" --tipo info "✅ Comando recebido: [resuma]. Vou fazer agora." --timeout 5
```

**DEPOIS:** Execute o comando

---

## Regras Importantes

1. ✅ **MODO PADRÃO:** Sistema inicia em modo REMOTO (envia Telegram)
2. ✅ **MUDANÇA DE MODO:** Só muda se usuário digitar `#remoto` ou `#console` explicitamente
3. ✅ **AO NOTIFICAR:** Use `notificar_e_aguardar.py` e aguarde resposta
4. ✅ **DURANTE TAREFAS LONGAS:** Verifique comandos a cada 2-3 min
5. ✅ **QUANDO ACORDAR (após repouso):** Verifique comandos imediatamente
6. ✅ **QUANDO RECEBER COMANDO:** SEMPRE confirme explicitamente que recebeu e vai fazer
7. ✅ **SE RESPOSTA = "ok":** Continue normalmente
8. ✅ **SE RESPOSTA = instruções:** Siga as instruções

---

## Resumo do Fluxo

**Sistema inicia (ou após compactação):**
→ Modo REMOTO (padrão)
→ Envia notificações Telegram
→ Verifica comandos quando ATIVA (durante tarefas)

**Você está OCIOSA (em repouso):**
→ NÃO pode verificar comandos automaticamente
→ Usuário tem monitor separado que alerta
→ Quando usuário te acordar → verifique imediatamente

**Você está TRABALHANDO (tarefa longa):**
→ Verifique comandos a cada 2-3 min
→ Continue trabalhando entre verificações

**Usuário digita `#remoto`:**
→ Confirma modo remoto
→ Responde: "✅ Modo remoto ativado..."
→ Verifica comandos (se ativa) e continua

**Usuário digita `#console`:**
→ Muda para modo console
→ Responde: "✅ Modo console ativado..."
→ Notificações desativadas

**Usuário digita qualquer outra coisa:**
→ **Mantém modo atual** (não muda)
→ **Verifique comandos PRIMEIRO**
→ Processa a mensagem normalmente

---

## Exemplo Prático de Confirmação

**Você recebe comando via Telegram:** "execute npm test"

**Você DEVE responder IMEDIATAMENTE:**
```bash
python "D:\Script Python\Monitor ETH\notificar_e_aguardar.py" --tipo info "✅ Comando recebido: executar npm test. Vou fazer agora." --timeout 5
```

**Depois:**
- Executa: npm test
- Quando terminar: notifica o resultado

---

Para detalhes: `D:\Script Python\Monitor ETH\INSTRUCOES_CLAUDE.md`
