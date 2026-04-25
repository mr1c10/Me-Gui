using MeGui.Data;
using MeGui.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MeGui.Services;

public class BotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotService> _logger;
    private readonly TelegramBotClient _botClient;

    private const string SystemPrompt = """
        Você é o MêGui, um assistente amigável e prestativo que ajuda pessoas a se locomoverem 
        pelas estações de metrô e trem de São Paulo. Você é simpático, usa linguagem simples e direta.
        Quando o usuário pedir ajuda com uma rota, extraia a estação de ORIGEM e DESTINO da mensagem.
        Responda SEMPRE em português brasileiro.
        
        Se o usuário informar origem e destino, responda EXATAMENTE no formato:
        ROTA:origem|destino
        
        Onde 'origem' e 'destino' são os nomes das estações, sem acentos desnecessários.
        
        Se o usuário disser que chegou em um checkpoint ou algo similar (ex: "cheguei", "estou aqui", "próximo"), 
        responda exatamente: PROXIMO_CHECKPOINT
        
        Para qualquer outra conversa, responda normalmente sendo prestativo e amigável.
        """;

    public BotService(IServiceProvider serviceProvider, ILogger<BotService> logger, TelegramBotClient botClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MêGui Bot iniciando...");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(stoppingToken);

        _logger.LogInformation("Banco de dados migrado com sucesso.");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("MêGui Bot está ouvindo mensagens...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text is not null)
            {
                await HandleMessageAsync(update.Message, ct);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar update {UpdateId}", update.Id);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Erro no polling do Telegram");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var userName = message.From?.FirstName ?? message.Chat.FirstName ?? "Viajante";
        var text = message.Text!.Trim();

        _logger.LogInformation("Mensagem de {User} ({ChatId}): {Text}", userName, chatId, text);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var openRouter = scope.ServiceProvider.GetRequiredService<OpenRouterService>();
        var pdfService = scope.ServiceProvider.GetRequiredService<PdfService>();

        var session = await db.ChatSessions.FindAsync([chatId], ct);

        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            if (session == null)
            {
                session = new ChatSession { ChatId = chatId, UserName = userName };
                db.ChatSessions.Add(session);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                session.CurrentRouteId = null;
                session.CurrentCheckpointOrder = 0;
                await db.SaveChangesAsync(ct);
            }

            await _botClient.SendMessage(
                chatId,
                $"Olá {userName}, sou o **MêGui**! 🚇\n\n" +
                "Seu guia pessoal para as estações de metrô e trem de São Paulo.\n\n" +
                "Me diga: **de qual estação você está saindo** e **para qual estação deseja ir**?\n\n" +
                "Exemplo: _\"Quero ir da Sé para a Paulista\"_",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            return;
        }

        if (text.Equals("/rotas", StringComparison.OrdinalIgnoreCase))
        {
            var routes = await db.Routes.ToListAsync(ct);
            if (routes.Count == 0)
            {
                await _botClient.SendMessage(chatId, "Nenhuma rota cadastrada no momento.", cancellationToken: ct);
                return;
            }

            var routeList = string.Join("\n", routes.Select(r => $"• {r.OriginStation} → {r.DestinationStation}"));
            await _botClient.SendMessage(
                chatId,
                $"📋 **Rotas disponíveis:**\n\n{routeList}\n\nDiga o trajeto desejado!",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            return;
        }

        // Ensure session exists
        if (session == null)
        {
            session = new ChatSession { ChatId = chatId, UserName = userName };
            db.ChatSessions.Add(session);
            await db.SaveChangesAsync(ct);
        }

        // If user is in an active route, check for checkpoint confirmation
        if (session.CurrentRouteId.HasValue)
        {
            var llmResponse = await openRouter.ChatAsync(SystemPrompt, text, ct);

            if (llmResponse.Contains("PROXIMO_CHECKPOINT"))
            {
                await SendNextCheckpointAsync(db, session, ct);
                return;
            }

            // User might want to do something else while in route
            await _botClient.SendMessage(
                chatId,
                llmResponse + "\n\n_Use o botão \"Cheguei!\" ou me diga quando chegar no ponto indicado._",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            return;
        }

        // Use LLM to extract route info
        var response = await openRouter.ChatAsync(SystemPrompt, text, ct);
        _logger.LogInformation("Resposta do LLM: {Response}", response);

        if (response.Contains("ROTA:"))
        {
            var routePart = response.Split("ROTA:").Last().Trim().Split('\n').First().Trim();
            var parts = routePart.Split('|');

            if (parts.Length == 2)
            {
                var origin = parts[0].Trim();
                var destination = parts[1].Trim();

                var routes = await db.Routes
                    .Include(r => r.Checkpoints)
                    .ToListAsync(ct);

                var route = routes.FirstOrDefault(r =>
                    NormalizeString(r.OriginStation).Contains(NormalizeString(origin)) &&
                    NormalizeString(r.DestinationStation).Contains(NormalizeString(destination)));

                if (route != null)
                {
                    session.CurrentRouteId = route.Id;
                    session.CurrentCheckpointOrder = 0;
                    await db.SaveChangesAsync(ct);

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📄 Baixar PDF da rota", $"pdf_{route.Id}"),
                            InlineKeyboardButton.WithCallbackData("▶️ Iniciar navegação", $"start_{route.Id}")
                        }
                    });

                    await _botClient.SendMessage(
                        chatId,
                        $"✅ Encontrei a rota **{route.OriginStation} → {route.DestinationStation}**!\n\n" +
                        $"Essa rota tem **{route.Checkpoints.Count} checkpoints**.\n\n" +
                        "Você pode baixar um PDF com o guia completo para consulta offline ou iniciar a navegação agora:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: ct
                    );
                    return;
                }

                await _botClient.SendMessage(
                    chatId,
                    $"😔 Desculpe, não encontrei uma rota de **{origin}** para **{destination}**.\n\n" +
                    "Use /rotas para ver as rotas disponíveis.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
                return;
            }
        }

        // Generic LLM response
        await _botClient.SendMessage(chatId, response, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private string NormalizeString(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pdfService = scope.ServiceProvider.GetRequiredService<PdfService>();

        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

        if (data.StartsWith("pdf_"))
        {
            var routeId = Guid.Parse(data[4..]);
            var route = await db.Routes.Include(r => r.Checkpoints).FirstOrDefaultAsync(r => r.Id == routeId, ct);

            if (route == null) return;

            var pdfBytes = pdfService.GenerateRoutePdf(route, route.Checkpoints);

            using var stream = new MemoryStream(pdfBytes);
            var inputFile = InputFile.FromStream(stream, $"MeGui_Rota_{route.OriginStation}_{route.DestinationStation}.pdf");

            await _botClient.SendDocument(
                chatId,
                inputFile,
                caption: $"📄 Aqui está o PDF da rota **{route.OriginStation} → {route.DestinationStation}**.\nGuarde para consulta offline!",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("▶️ Iniciar navegação", $"start_{route.Id}") }
            });

            await _botClient.SendMessage(
                chatId,
                "Quando estiver pronto, clique para iniciar a navegação:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );
        }
        else if (data.StartsWith("start_"))
        {
            var routeId = Guid.Parse(data[6..]);
            var session = await db.ChatSessions.FindAsync([chatId], ct);

            if (session == null) return;

            session.CurrentRouteId = routeId;
            session.CurrentCheckpointOrder = 0;
            await db.SaveChangesAsync(ct);

            await _botClient.SendMessage(
                chatId,
                "🚀 Navegação iniciada! Vou te enviar o primeiro checkpoint.",
                cancellationToken: ct
            );

            await SendNextCheckpointAsync(db, session, ct);
        }
        else if (data.StartsWith("arrived_"))
        {
            var session = await db.ChatSessions.FindAsync([chatId], ct);
            if (session == null) return;

            await SendNextCheckpointAsync(db, session, ct);
        }
    }

    private async Task SendNextCheckpointAsync(AppDbContext db, ChatSession session, CancellationToken ct)
    {
        if (!session.CurrentRouteId.HasValue) return;

        var nextOrder = session.CurrentCheckpointOrder + 1;

        var checkpoint = await db.Checkpoints
            .FirstOrDefaultAsync(c => c.RouteId == session.CurrentRouteId && c.Order == nextOrder, ct);

        if (checkpoint == null)
        {
            // Route finished
            session.CurrentRouteId = null;
            session.CurrentCheckpointOrder = 0;
            await db.SaveChangesAsync(ct);

            await _botClient.SendMessage(
                session.ChatId,
                "🎉 **Você chegou ao destino!**\n\nEspero ter ajudado. Posso ajudar com algo mais?\nUse /start para uma nova rota.",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            return;
        }

        session.CurrentCheckpointOrder = nextOrder;
        await db.SaveChangesAsync(ct);

        // Send checkpoint photo from local folder
        try
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "imagens", "estacoes-do-metro-em-SP.jpg");
            
            // Check if file exists, if not try within MeGui folder
            if (!System.IO.File.Exists(imagePath))
            {
                imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Imagens", checkpoint.ImageUrl);
            }

            if (System.IO.File.Exists(imagePath))
            {
                using var stream = System.IO.File.OpenRead(imagePath);
                await _botClient.SendPhoto(
                    session.ChatId,
                    InputFile.FromStream(stream),
                    caption: $"📍 **Checkpoint {checkpoint.Order}**\n\n{checkpoint.Instruction}",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            else
            {
                throw new FileNotFoundException("Imagem não encontrada", imagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar imagem local: {ImageUrl}", checkpoint.ImageUrl);
            // Fallback: send as text if image fails
            await _botClient.SendMessage(
                session.ChatId,
                $"📍 **Checkpoint {checkpoint.Order}**\n\n{checkpoint.Instruction}\n\n🖼️ (Imagem não disponível localmente: {checkpoint.ImageUrl})",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
        }

        var totalCheckpoints = await db.Checkpoints.CountAsync(c => c.RouteId == session.CurrentRouteId, ct);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✅ Cheguei!", $"arrived_{checkpoint.Order}") }
        });

        await _botClient.SendMessage(
            session.ChatId,
            $"_Checkpoint {checkpoint.Order} de {totalCheckpoints}_\n\nQuando chegar neste ponto, clique no botão ou me diga \"Cheguei!\"",
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct
        );
    }
}
