# Problema de Cards Retangulares

## 🔴 PROBLEMA RECORRENTE

Este problema acontece **TODAS AS VEZES** que criamos um novo grupo de cards (Living Room, Bathroom, etc.)

## Sintomas

- Cards aparecem com **thumbnails muito pequenos** no desktop
- Cards ficam **estreitos com aspecto de coluna vertical** (mesmo com apenas 2 cards)
- No mobile funciona perfeitamente
- O código JavaScript está correto (idêntico aos que funcionam)

## Causa Raiz

**Dois problemas CSS combinados:**

1. **Object-fit incorreto**: `object-fit: cover` em imagens verticais (retrato) faz elas aparecerem cortadas/pequenas
2. **Grid layout inadequado**: Grid forçando 4 colunas quando há apenas 2 cards, fazendo cada card ficar com 25% de largura

## Solução Completa (2 passos)

### Passo 1: Corrigir object-fit

**Arquivo:** `Frontend/style.css` e `Backend/wwwroot/style.css`

**Linha ~1510** - Mudar de `cover` para `contain`:

```css
.countertop-preview img {
    width: 100%;
    height: 100%;
    object-fit: contain; /* contain mostra a imagem completa, cover corta */
    object-position: center;
    border-radius: 8px;
}
```

### Passo 2: Corrigir grid layout para grupos com 2 cards

**Arquivo:** `Frontend/style.css` e `Backend/wwwroot/style.css`

**Adicionar após linha ~1537** (media query desktop):

```css
/* Living Room e Bathroom: apenas 2 cards, grid de 2 colunas (DESKTOP APENAS) */
@media (min-width: 600px) {
    #livingRoomSelectionScreen .countertop-options,
    #livingRoomTestSelectionScreen .countertop-options,
    #bathroomSelectionScreen .countertop-options {
        grid-template-columns: repeat(2, 1fr) !important;
        max-width: 600px;
        margin: 20px auto;
    }
}
```

**IMPORTANTE:**
- Quando criar um **NOVO grupo de cards** com apenas 2 opções, **adicione o ID da tela** nesta regra
- Exemplo: Se criar `#bedroomSelectionScreen`, adicione na lista acima

### Passo 3: Sincronizar arquivos

```bash
cp Frontend/style.css Backend/wwwroot/style.css
```

## Quando Aplicar Esta Solução

✅ **Aplicar quando:**
- Criar novo grupo de cards (Living Room, Bathroom, Kitchen, etc.)
- Grupo tiver **apenas 2 cards** para escolher
- Cards aparecerem estreitos/pequenos no desktop
- Mobile funcionar corretamente mas desktop não

❌ **NÃO aplicar quando:**
- Grupo tiver 3 ou mais cards (grid de 4 colunas funciona bem)
- Cards já estiverem com tamanho correto

## Exemplo de Caso Real

**Living Room e Bathroom:**
- Cada um tem apenas 2 opções de design
- Sem a correção: cards ficam em 4 colunas (25% cada), muito estreitos
- Com a correção: cards ficam em 2 colunas (50% cada), tamanho perfeito

## Referência Técnica

**Imagens verticais vs horizontais:**
- Bathroom: 300x225px e 300x200px (horizontal/landscape) ✅
- Living Room: 300x373px e 300x371px (vertical/portrait) ⚠️

Com `object-fit: cover`, imagens verticais aparecem cortadas.
Com `object-fit: contain`, imagens verticais aparecem completas.

---

## Gatilho

Quando eu disser **"problema de cards retangulares"**, você deve:
1. Aplicar o Passo 1 (object-fit: contain)
2. Aplicar o Passo 2 (grid de 2 colunas para o novo grupo)
3. Sincronizar os arquivos (Passo 3)
