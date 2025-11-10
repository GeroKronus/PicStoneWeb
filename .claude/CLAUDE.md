# Sistema de Notificação Telegram

## Detecção Automática de Presença

**SEMPRE que receber uma mensagem do usuário:**

1. Se mensagem = `#remoto`:
   - Execute: `python "D:\Script Python\Monitor ETH\modo.py" remoto`
   - Responda: "✅ Modo remoto ativado. Você receberá notificações no Telegram."
   - **NÃO faça mais nada**

2. Se mensagem = **qualquer outra coisa**:
   - **PRIMEIRO** execute: `python "D:\Script Python\Monitor ETH\modo.py" console`
   - **DEPOIS** processe a mensagem normalmente

**Isso garante que quando o usuário voltar e digitar algo, notificações são desativadas automaticamente.**

---

## Instruções de Notificação

**SEMPRE que finalizar tarefas ou precisar de interação:**

```bash
python "D:\Script Python\Monitor ETH\notificar_e_aguardar.py" --tipo concluido "sua mensagem"
```

**Verifique comandos periodicamente** (a cada 2-3 min em tarefas longas):

```bash
python "D:\Script Python\Monitor ETH\ler_comando.py"
```

---

## Regras

1. ✅ SEMPRE ative modo console ao receber mensagem (exceto se for #remoto)
2. ✅ SEMPRE notifique e aguarde resposta
3. ✅ Verifique comandos durante tarefas longas
4. ✅ Se resposta = "ok" → continue
5. ✅ Se resposta = instruções → siga

---

Para detalhes: `D:\Script Python\Monitor ETH\INSTRUCOES_CLAUDE.md`
