# MêGui - Guia de Estações de SP 🚇

Chatbot para Telegram que auxilia a navegação interna em estações de metrô/trem em São Paulo usando checkpoints visuais (fotos).

## Stack
- **.NET 10** (C# / Minimal API)
- **Telegram Bot API** (Telegram.Bot v22)
- **OpenRouter** (LLM - GPT-4o-mini)
- **SQLite** (Entity Framework Core)
- **QuestPDF** (Geração de PDF offline)

## Como rodar

```bash
cd src/MeGui
dotnet run
```

## Configuração

Edite `src/MeGui/appsettings.json` com:
- `Telegram:BotToken` — Token do bot do Telegram
- `OpenRouter:ApiKey` — Chave da API do OpenRouter

## Comandos do Bot
- `/start` — Inicia o bot e reseta a sessão
- `/rotas` — Lista rotas disponíveis
- Mensagem livre — A LLM interpreta origem/destino e inicia a navegação

## Fluxo
1. Usuário diz de onde sai e para onde vai
2. Bot encontra a rota e oferece PDF offline + botão para iniciar
3. Bot envia checkpoints sequenciais (foto + instrução)
4. Usuário clica "Cheguei!" → próximo checkpoint
5. Ao final, bot parabeniza e oferece nova rota
