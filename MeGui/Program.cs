using MeGui.Data;
using MeGui.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Telegram.Bot;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=megui.db"));

// OpenRouter
var openRouterSettings = new OpenRouterSettings();
builder.Configuration.GetSection("OpenRouter").Bind(openRouterSettings);
builder.Services.AddSingleton(openRouterSettings);
builder.Services.AddHttpClient<OpenRouterService>();

// PDF
builder.Services.AddSingleton<PdfService>();

// Telegram Bot
var botToken = builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException("Telegram:BotToken não configurado. Defina via appsettings.json ou variável de ambiente.");

builder.Services.AddSingleton(new TelegramBotClient(botToken));
builder.Services.AddHostedService<BotService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "running", app = "MêGui Bot", version = "1.0.0-mvp" }));

app.MapGet("/routes", async (AppDbContext db) =>
{
    var routes = await db.Routes
        .Include(r => r.Checkpoints.OrderBy(c => c.Order))
        .ToListAsync();

    return Results.Ok(routes.Select(r => new
    {
        r.Id,
        r.OriginStation,
        r.DestinationStation,
        Checkpoints = r.Checkpoints.Select(c => new
        {
            c.Order,
            c.Instruction,
            c.ImageUrl
        })
    }));
});

app.Run();
