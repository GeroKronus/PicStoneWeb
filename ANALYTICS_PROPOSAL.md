# 📊 Proposta de Sistema de Analytics e Histórico de Usuário - PicStone Mobile

**Data:** 09/11/2025
**Versão:** 1.0
**Status:** Proposta Completa para Aprovação

---

## 📋 Sumário Executivo

Esta proposta apresenta um sistema completo de **analytics, histórico e relatórios** para o PicStone Mobile, permitindo:

✅ **Rastreamento completo** de todas as ações dos usuários
✅ **Histórico detalhado** por usuário (fotos, mockups, downloads)
✅ **Relatórios administrativos** com métricas de uso
✅ **Dashboard em tempo real** para usuários e administradores
✅ **KPIs de negócio** (DAU, MAU, retenção, engagement)

**Impacto Esperado:**
- Visibilidade 360° do comportamento do usuário
- Identificação de features mais populares
- Detecção de problemas de UX e performance
- Dados para decisões de produto

---

## 🎯 Funcionalidades Propostas

### 1. **Histórico do Usuário** 📜

**O que será rastreado:**

| Categoria | Ações Rastreadas | Dados Capturados |
|-----------|------------------|------------------|
| **Fotos** | Upload, visualização, download, exclusão | Nome arquivo, tamanho, material, bloco/chapa, timestamp |
| **Mockups** | Geração (Bancadas 1-8, Nicho, BookMatch), download | Tipo mockup, parâmetros (flip, fundo), tempo geração, sucesso/falha |
| **Autenticação** | Login, logout, alteração senha | Timestamp, IP, device type, duração sessão |
| **Navegação** | Visualização de páginas, cliques em features | URL, tempo na página, fluxo de navegação |
| **Erros** | Erros de upload, geração, validação | Tipo erro, mensagem, stack trace |

**Exemplo de Timeline do Usuário:**
```
📅 09/11/2025
├─ 10:30 - Login (Desktop, Chrome, Windows)
├─ 10:32 - Upload foto "GRANITO_001.jpg" (2.5MB)
├─ 10:35 - Gerou Mockup Bancada 1 (fundo claro, 1.2s)
├─ 10:36 - Download "mockup_bancada1_claro.jpg"
├─ 10:45 - Gerou BookMatch com 4 quadrantes (3.5s)
├─ 11:00 - Logout
└─ Duração sessão: 30 minutos, 5 ações
```

---

### 2. **Dashboard do Usuário** 📊

**Visão Pessoal:**
- **Resumo de Atividades:** Hoje, Esta Semana, Este Mês
- **Estatísticas:**
  - Total de fotos enviadas
  - Total de mockups gerados
  - Feature favorita (ex: Bancada 1)
  - Tempo total de uso
- **Atividade Recente:** Últimas 10 ações
- **Status da Conta:** Dias até expiração, storage usado

**Interface Proposta:**
```
┌─────────────────────────────────────────┐
│  Meu Painel - Rogério                   │
├─────────────────────────────────────────┤
│  📈 Esta Semana                          │
│  ├─ 34 fotos enviadas                    │
│  ├─ 28 mockups gerados                   │
│  ├─ 87 downloads                         │
│  └─ 2h 15min de uso                      │
├─────────────────────────────────────────┤
│  ⭐ Sua Feature Favorita                 │
│  Bancada 1 (87 usos)                    │
├─────────────────────────────────────────┤
│  🕒 Atividade Recente                    │
│  • 10:36 - Download mockup               │
│  • 10:35 - Gerou Bancada 1               │
│  • 10:32 - Upload GRANITO_001.jpg        │
└─────────────────────────────────────────┘
```

---

### 3. **Dashboard Administrativo** 🔧

**Visão Global do Sistema:**

**Métricas em Tempo Real:**
- 👥 Usuários ativos agora: 12
- 🔥 Sessões ativas: 15
- ⚡ Requisições/min: 45
- 📊 Tempo resposta médio: 245ms

**Estatísticas do Dia:**
- ✅ Novos usuários: 2
- 📸 Fotos enviadas: 87
- 🎨 Mockups gerados: 67
- 📥 Downloads: 234

**Ações Pendentes:**
- ⏳ Aguardando aprovação: 5 usuários
- ⚠️ Usuários expirados: 3
- 🔴 Alertas do sistema: 1

**Features Mais Usadas (últimos 30 dias):**
```
Bancada 1  ████████████████████████ 870 usos (23.5%)
Bancada 2  ██████████████████ 654 usos (17.7%)
Bancada 3  ███████████████ 543 usos (14.7%)
BookMatch  ████████████ 298 usos (8.1%)
Nicho 1    ████████ 123 usos (3.3%)
```

---

### 4. **Relatórios Administrativos** 📑

**Tipos de Relatórios:**

#### A) **Relatório de Uso por Usuário**
```
┌────────────────────────────────────────────────┐
│ RELATÓRIO MENSAL DE ATIVIDADES                 │
│ Período: 01/11/2025 - 30/11/2025              │
├────────────────────────────────────────────────┤
│                                                 │
│ USUÁRIOS MAIS ATIVOS                           │
│ 1. rogerio@picstone.com.br - 1543 eventos     │
│    • 234 fotos enviadas                        │
│    • 189 mockups gerados                       │
│    • 456 downloads                             │
│    • Tempo médio de sessão: 21min             │
│                                                 │
│ 2. maria@exemplo.com.br - 892 eventos         │
│    • 156 fotos enviadas                        │
│    • 98 mockups gerados                        │
│    • 234 downloads                             │
│    • Tempo médio de sessão: 15min             │
│                                                 │
│ MÉTRICAS GERAIS                                │
│ • Total de usuários ativos: 87                │
│ • Novos usuários: 12                          │
│ • Taxa de retenção (D7): 73%                  │
│ • Taxa de conversão: 85%                      │
└────────────────────────────────────────────────┘
```

#### B) **Relatório de Performance de Features**
- Comparação de uso entre features
- Taxa de sucesso/falha por feature
- Tempo médio de geração
- Taxa de download (quantos mockups são baixados após serem gerados)

#### C) **Relatório de Erros e Saúde do Sistema**
- Erros mais frequentes
- Taxa de erro por endpoint
- Disponibilidade do sistema (uptime)
- Performance (tempos de resposta)

---

### 5. **Métricas e KPIs** 📈

**Métricas de Engajamento:**

| Métrica | Descrição | Meta |
|---------|-----------|------|
| **DAU** | Usuários ativos diários | 50+ |
| **MAU** | Usuários ativos mensais | 120+ |
| **Stickiness** | DAU/MAU (% usuários que retornam diariamente) | >20% |
| **Sessão Média** | Duração média das sessões | 15-20 min |
| **Retenção D1** | % usuários que retornam no dia seguinte | >40% |
| **Retenção D7** | % usuários que retornam em 7 dias | >30% |
| **Retenção D30** | % usuários que retornam em 30 dias | >20% |

**Métricas de Produto:**

| Métrica | Descrição | Meta |
|---------|-----------|------|
| **Conversão Upload→Mockup** | % usuários que geram mockup após upload | >80% |
| **Taxa de Download** | % mockups gerados que são baixados | >70% |
| **Features Descobertas** | Nº médio de features usadas por usuário | 3+ |
| **Tempo até 1º Mockup** | Tempo entre cadastro e 1ª geração | <5 min |

**Métricas de Performance:**

| Métrica | Descrição | Limite |
|---------|-----------|--------|
| **Tempo Resposta P95** | 95% requisições respondem em: | <2s |
| **Taxa de Erro** | % requisições com erro | <2% |
| **Uptime** | Disponibilidade do sistema | >99.5% |
| **Apdex Score** | Índice de satisfação de performance | >0.8 |

---

### 6. **Segmentação de Usuários** 🎯

**Classificação Automática:**

| Segmento | Critérios | Ações Sugeridas |
|----------|-----------|-----------------|
| **Power User** | >100 mockups/mês, >20 sessões/mês | Programa de beta testers, feedback prioritário |
| **Regular** | 20-100 mockups/mês, 5-20 sessões/mês | Manter engajamento, promoções ocasionais |
| **Casual** | <20 mockups/mês, <5 sessões/mês | Campanhas de engajamento, tutoriais |
| **Em Risco** | Queda >50% atividade vs mês anterior | Email de reengajamento, pesquisa de satisfação |
| **Inativo** | Sem login há >30 dias | Campanha de reconquista, ofertas especiais |

---

## 🗄️ Arquitetura de Banco de Dados

### Tabelas Principais:

**1. UserActivities** (Eventos de atividade)
```sql
- Id (BIGINT, auto-increment)
- UserId (INT, FK para Usuarios)
- SessionId (VARCHAR(100))
- EventType (VARCHAR(50)) -- 'foto_upload', 'mockup_generate', etc.
- EventCategory (VARCHAR(50)) -- 'foto', 'mockup', 'auth'
- EventAction (VARCHAR(100)) -- 'upload', 'download', 'generate'
- Metadata (JSON) -- Dados flexíveis
- Timestamp (DATETIME)
- IpAddress, UserAgent, DurationMs
- Indexes: UserId, EventType, Timestamp
```

**2. UserSessions** (Sessões)
```sql
- Id (VARCHAR(100), session GUID)
- UserId (INT, FK)
- StartedAt, EndedAt, DurationSeconds
- DeviceType, Browser, OS, Country, City
- EventsCount, PhotosUploaded, MockupsGenerated
- Indexes: UserId, StartedAt, IsActive
```

**3. FeatureUsage** (Uso agregado de features)
```sql
- Id (BIGINT)
- Date, Hour
- FeatureName, FeatureCategory
- UsageCount, UniqueUsers
- SuccessCount, FailureCount, SuccessRate
- AvgDurationMs, P95DurationMs
- Indexes: Date, FeatureName
```

**4. UserMetrics** (Métricas por usuário)
```sql
- Id (BIGINT)
- UserId (INT, FK)
- PeriodStart, PeriodEnd, PeriodType ('daily', 'weekly', 'monthly')
- TotalSessions, TotalEvents, TotalActiveMinutes
- PhotosUploaded, MockupsGenerated, Downloads
- EngagementScore (0-100)
- UserSegment ('power', 'regular', 'casual', 'inactive')
- Indexes: UserId+PeriodType, EngagementScore
```

**5. SystemMetrics** (Métricas do sistema)
```sql
- Id (BIGINT)
- Timestamp, Date, Hour, PeriodType
- TotalUsers, ActiveUsers, NewUsers
- TotalEvents, TotalSessions
- AvgResponseTimeMs, ErrorRate
- PhotosUploaded, MockupsGenerated
- CpuUsagePercent, MemoryUsagePercent
- Indexes: Date, PeriodType
```

---

## 🔌 API Endpoints Propostos

### **Tracking (Usuario)**
```
POST /api/analytics/track
- Registra evento de atividade
- Rate limit: 100 req/min
```

### **Histórico do Usuário**
```
GET /api/users/{id}/history
- Lista histórico de atividades
- Filtros: eventType, category, dateRange
- Paginação: page, pageSize (max 200)

GET /api/users/{id}/stats
- Estatísticas agregadas do usuário
- Filtros: period (24h, 7d, 30d, 90d, 365d, all)
- Retorna: timeline, topActions, deviceInfo
```

### **Dashboard**
```
GET /api/dashboard/metrics
- Métricas em tempo real
- view=personal: Dashboard do usuário
- view=admin: Dashboard administrativo
- Auto-refresh: 60s
```

### **Relatórios Admin**
```
GET /api/admin/reports/overview
- Visão geral do sistema
- Comparação com períodos anteriores

GET /api/admin/reports/users
- Relatório detalhado de usuários
- Filtros: status, sortBy, search
- Paginação

GET /api/admin/reports/features
- Analytics de uso de features
- Timeline, trends, comparações

GET /api/admin/reports/errors
- Log de erros do sistema
- Filtros: severity, type
```

### **Export e Geração**
```
POST /api/analytics/export
- Exporta dados (CSV, JSON, XLSX)
- Entrega: download ou email
- Status: /api/analytics/export/{id}/status

POST /api/admin/reports/generate
- Gera relatório customizado (PDF, HTML, XLSX)
- Suporta agendamento recorrente
- Status: /api/admin/reports/{id}/status
```

---

## 🚀 Roadmap de Implementação

### **Fase 1: Fundação** (Semanas 1-2)
- ✅ Criar tabelas do banco de dados
- ✅ Implementar AnalyticsService
- ✅ Middleware de tracking automático
- ✅ Endpoint POST /api/analytics/track
- ✅ Testes com dados sintéticos

**Entregável:** Sistema de tracking básico funcionando

---

### **Fase 2: Histórico e Stats** (Semanas 3-4)
- ✅ Endpoint GET /api/users/{id}/history
- ✅ Endpoint GET /api/users/{id}/stats
- ✅ Dashboard do usuário (frontend)
- ✅ Integração em controllers existentes

**Entregável:** Usuários podem ver seu próprio histórico

---

### **Fase 3: Admin Dashboard** (Semanas 5-6)
- ✅ Endpoints /api/admin/reports/*
- ✅ Dashboard administrativo (frontend)
- ✅ Jobs de agregação (hourly/daily)
- ✅ Segmentação de usuários

**Entregável:** Admin pode monitorar sistema completo

---

### **Fase 4: Avançado** (Semanas 7-8)
- ✅ Exportação de dados (CSV/XLSX)
- ✅ Geração de relatórios (PDF)
- ✅ Relatórios agendados
- ✅ Real-time streaming (opcional)
- ✅ Alertas automáticos

**Entregável:** Sistema completo de analytics enterprise

---

## 💡 Funcionalidades Extras Sugeridas

### 1. **Comparação de Mockups** ⚖️
- Permitir usuário salvar mockups favoritos
- Comparar lado a lado diferentes bancadas
- Histórico de comparações

### 2. **Compartilhamento Social** 🔗
- Gerar link compartilhável de mockup
- Tracking de compartilhamentos
- Estatísticas de visualizações

### 3. **Coleções/Projetos** 📁
- Usuário organizar mockups em projetos
- Ex: "Projeto Cozinha Casa", "Banheiro Suite"
- Analytics por projeto

### 4. **Recomendações Inteligentes** 🤖
- Baseado no histórico, sugerir próxima bancada
- "Usuários que geraram Bancada 1 também usaram Bancada 7"
- Personalização da experiência

### 5. **Metas e Conquistas** 🏆
- Gamificação: badges por uso
- "Gerou 100 mockups", "Explorador (usou todas bancadas)"
- Aumenta engajamento

### 6. **Notificações Push** 🔔
- Avisar quando mockup está pronto (se demorado)
- Lembrar usuário inativo (re-engagement)
- Novidades de features

---

## 📊 Exemplo de Uso Real

**Caso de Uso: Identificar Feature Problemática**

1. **Admin acessa:** `/api/admin/reports/features?period=7d`
2. **Nota:** Bancada 4 tem taxa de sucesso de apenas 65% (outras >95%)
3. **Investiga:** `/api/admin/reports/errors?errorType=MockupGenerationError`
4. **Descobre:** Erro específico "OutOfMemoryException" em Bancada 4 com imagens >5MB
5. **Ação:** Implementa compressão automática antes de processar
6. **Resultado:** Taxa de sucesso sobe para 98%

**Impacto:** Problema detectado e resolvido proativamente, melhorando UX

---

## 🔒 Privacidade e Segurança

### Conformidade LGPD/GDPR:
✅ **Anonimização:** IPs armazenados com último octeto zerado
✅ **Consentimento:** Banner de cookies/analytics
✅ **Direito ao Esquecimento:** Usuário pode solicitar exclusão de dados
✅ **Retenção:** Dados deletados após período (13 meses para eventos, 90 dias para performance)
✅ **Auditoria:** Log de quem acessa dados de usuários

### Segurança:
✅ **Autenticação:** Todos endpoints requerem JWT
✅ **Autorização:** Usuário só acessa seus dados, admin acessa tudo
✅ **Rate Limiting:** Previne abuso
✅ **Criptografia:** Dados sensíveis em metadata são criptografados
✅ **SQL Injection:** Uso de Entity Framework com queries parametrizadas

---

## 💰 Estimativa de Recursos

### Armazenamento:
- **1 evento = ~500 bytes** (JSON compacto)
- **100 usuários x 100 eventos/dia = 10K eventos/dia = 5MB/dia**
- **1 ano = 1.8GB** (eventos brutos)
- **Com agregação = 200MB** (DailyMetrics compactos)

### Performance:
- **Write:** 1000 eventos/segundo (batch inserts)
- **Read (dashboard):** <100ms com cache de 1min
- **Read (reports):** <1s com dados pré-agregados
- **Export:** ~1min para 100K registros

### Infraestrutura:
- **Dev/Staging:** SQLite (suficiente para testes)
- **Produção:** PostgreSQL (Railway já suporta)
- **Cache:** Redis (opcional, para dashboards)
- **Background Jobs:** Hangfire (já em .NET)

---

## 🎯 Métricas de Sucesso da Implementação

Como saberemos que o sistema de analytics está funcionando bem:

✅ **Técnico:**
- 100% dos eventos críticos sendo rastreados
- <1% de perda de eventos (reliability)
- Dashboard carrega em <500ms
- Relatórios gerados em <5s

✅ **Produto:**
- Admin usa dashboard semanalmente
- Pelo menos 1 decisão de produto baseada em dados/mês
- 3+ problemas de UX identificados e resolvidos no 1º trimestre

✅ **Negócio:**
- Identificar top 3 features mais usadas
- Calcular ROI por feature (esforço dev vs uso real)
- Aumentar retenção em 15% baseado em insights

---

## 📝 Próximos Passos

### Para Aprovação:
1. ✅ **Revisar proposta completa**
2. ⏳ **Aprovar escopo** (fase 1-4 ou subset)
3. ⏳ **Definir prioridades** (quais features primeiro)
4. ⏳ **Alocar recursos** (tempo de desenvolvimento)

### Dúvidas para Esclarecer:
- Preferência de visualização de dados (gráficos/tabelas)?
- Relatórios devem ser PDF ou basta CSV/Excel?
- Real-time streaming é necessário ou batch processing é suficiente?
- Há conformidade regulatória específica além de LGPD?

---

## 📚 Documentação Técnica Completa

Documentos gerados nesta análise:

1. **Current System Analysis** - Estrutura atual do sistema
2. **Analytics Patterns Research** - Padrões e best practices
3. **Database Architecture** - Schema completo com Entity Framework
4. **API Design** - Especificação REST completa com exemplos
5. **Implementation Guide** - Código de exemplo e integração

**Todos os detalhes técnicos estão disponíveis para consulta.**

---

## ✅ Conclusão

Este sistema de **Analytics e Histórico** transformará o PicStone Mobile em uma plataforma **data-driven**, fornecendo:

🎯 **Visibilidade total** do comportamento do usuário
📊 **Métricas acionáveis** para decisões de produto
🔍 **Detecção proativa** de problemas
📈 **Crescimento otimizado** baseado em dados reais

**Custo-Benefício:** Alto - Implementação em 8 semanas, benefícios de longo prazo imensos.

**Recomendação:** Aprovar implementação faseada começando pela Fase 1-2 (tracking básico + histórico usuário) e evoluir conforme necessidade.

---

**Prepared by:** Claude Code
**Date:** 09/11/2025
**Version:** 1.0 - Complete Proposal
**Status:** Awaiting Approval

---

