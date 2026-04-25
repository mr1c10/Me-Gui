# MêGui 🚇

**Seu guia pessoal para as estações de metrô e trem de São Paulo.**

> _"Cheguei na estação, e agora? Qual escada? Qual corredor?"_

O **MêGui** é um chatbot inteligente para **Telegram** que resolve esse problema. Ele guia o usuário passo a passo dentro das estações usando **checkpoints visuais** — fotos de referência com instruções claras como _"vire à direita na frente da loja X"_ — eliminando a confusão de se orientar em estações grandes e movimentadas.

---

## O Problema

Milhões de pessoas usam o metrô e o trem em São Paulo diariamente. Mesmo usuários frequentes se perdem ao fazer baldeações ou usar estações desconhecidas. Os mapas tradicionais mostram **linhas e paradas**, mas não ajudam com a **navegação interna** — onde ficam as escadas, os corredores de conexão, as saídas certas.

## A Solução

O MêGui transforma a navegação interna em uma experiência guiada e visual:

1. O usuário diz de onde está saindo e para onde quer ir
2. O bot encontra a rota e oferece um **PDF offline** para consulta sem internet
3. Envia **checkpoints sequenciais** com foto + instrução textual
4. O usuário confirma a chegada em cada ponto e recebe o próximo
5. Ao final, é parabenizado e pode iniciar uma nova rota

O diferencial é o uso de **referências visuais reais** (lojas, placas, escadas) como pontos de decisão — muito mais intuitivo do que coordenadas ou descrições abstratas.

---

## Demonstração do Fluxo

```
Usuário: Quero ir da Sé para a Paulista

MêGui:  ✅ Encontrei a rota Sé → Paulista!
        Essa rota tem 5 checkpoints.
        [📄 Baixar PDF da rota]  [▶️ Iniciar navegação]

MêGui:  📍 Checkpoint 1
        Você está na plataforma da estação Sé. Siga em direção
        à saída indicada pela placa 'Linha 1 - Azul'...
        [📷 Foto do local]
        [✅ Cheguei!]

Usuário: [clica "Cheguei!"]

MêGui:  📍 Checkpoint 2
        Após subir a escada rolante, siga pelo corredor principal...
        [📷 Foto do local]
        [✅ Cheguei!]

        ... (checkpoints seguintes) ...

MêGui:  🎉 Você chegou ao destino!
```

---

## Stack Tecnológica

| Camada | Tecnologia | Propósito |
|---|---|---|
| **Interface** | Telegram Bot API | Chat, fotos, botões inline, envio de PDF |
| **Backend** | .NET 10 (C#) / Minimal API | Lógica do bot, API REST, Long Polling |
| **LLM** | OpenRouter (GPT-4o-mini) | Interpretação de linguagem natural do usuário |
| **Banco de Dados** | SQLite + EF Core | Rotas, checkpoints e sessões de usuário |
| **Geração de PDF** | QuestPDF | Guia offline completo para download |
| **Storage de Imagens** | Cloudflare R2 | Hospedagem das fotos de checkpoint |

---

## Arquitetura do Projeto

```
src/MeGui/
├── Program.cs                  # Entry point, DI e endpoints Minimal API
├── Models/
│   ├── Route.cs                # Rota (origem → destino)
│   ├── Checkpoint.cs           # Ponto de referência visual na rota
│   └── ChatSession.cs          # Estado da conversa do usuário
├── Data/
│   ├── AppDbContext.cs          # DbContext EF Core + seed data
│   └── DesignTimeDbContextFactory.cs
├── Services/
│   ├── BotService.cs           # Handler do Telegram (comandos, callbacks, navegação)
│   ├── OpenRouterService.cs    # Integração com LLM via OpenRouter
│   └── PdfService.cs           # Geração de PDF offline com QuestPDF
└── appsettings.json            # Configuração (tokens, connection string)
```

---

## Como Rodar

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Token de Bot do Telegram (via [@BotFather](https://t.me/BotFather))
- Chave de API do [OpenRouter](https://openrouter.ai/)

### Configuração

Edite `MeGui/appsettings.json`:

```json
{
  "Telegram": {
    "BotToken": "SEU_TOKEN_AQUI"
  },
  "OpenRouter": {
    "ApiKey": "SUA_CHAVE_AQUI",
    "Model": "openai/gpt-4o-mini"
  }
}
```

### Execução

```bash
cd src/MeGui
dotnet run
```

O bot inicia em modo **Long Polling** e já começa a ouvir mensagens no Telegram. A API REST fica disponível em `http://localhost:5196`.

### Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Health check — status do bot |
| `GET` | `/routes` | Lista todas as rotas com checkpoints |

---

## Comandos do Bot

| Comando | Descrição |
|---|---|
| `/start` | Inicia o bot, exibe boas-vindas e reseta a sessão |
| `/rotas` | Lista todas as rotas disponíveis no banco |
| **Mensagem livre** | A LLM interpreta a intenção e extrai origem/destino |

### Botões Inline
- **📄 Baixar PDF da rota** — Gera e envia o guia offline completo
- **▶️ Iniciar navegação** — Começa o envio dos checkpoints
- **✅ Cheguei!** — Confirma chegada e avança para o próximo checkpoint

---

## Modelo de Dados

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│    Route     │       │   Checkpoint     │       │  ChatSession    │
├──────────────┤       ├──────────────────┤       ├─────────────────┤
│ Id (Guid)    │──┐    │ Id (Guid)        │       │ ChatId (long)   │
│ OriginStation│  │    │ RouteId (FK)     │──┐    │ UserName        │
│ Destination  │  └───>│ Order (int)      │  │    │ CurrentRouteId  │──>
│   Station    │       │ ImageUrl         │  │    │ CurrentCheckpoint│
└──────────────┘       │ Instruction      │  │    │   Order         │
                       └──────────────────┘  │    └─────────────────┘
                                             │
                              1:N ───────────┘
```

---

## Roadmap (pós-MVP)

- [ ] Mais rotas e estações cadastradas com fotos reais (Cloudflare R2)
- [ ] Reconhecimento de imagem — usuário envia foto e o bot identifica onde ele está
- [ ] Navegação por voz (áudio do Telegram)
- [ ] Webhook em produção (substituir Long Polling)
- [ ] Painel admin web para cadastro de rotas e checkpoints
- [ ] Integração com dados em tempo real do Metrô/CPTM (status das linhas)
- [ ] Suporte a múltiplos idiomas (turistas)

---

## Equipe

Desenvolvido durante hackathon como **MVP/POC** para validar a viabilidade da navegação guiada por checkpoints visuais em estações de transporte público.

---

> **MêGui** — _Porque ninguém deveria se perder no metrô._ 🚇
