using Microsoft.EntityFrameworkCore;
using Route = MeGui.Models.Route;
using Checkpoint = MeGui.Models.Checkpoint;
using ChatSession = MeGui.Models.ChatSession;

namespace MeGui.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.OriginStation).IsRequired().HasMaxLength(200);
            entity.Property(r => r.DestinationStation).IsRequired().HasMaxLength(200);

            entity.HasMany(r => r.Checkpoints)
                  .WithOne(c => c.Route)
                  .HasForeignKey(c => c.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Checkpoint>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(c => c.Instruction).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(s => s.ChatId);
            entity.Property(s => s.UserName).HasMaxLength(200);

            entity.HasOne(s => s.CurrentRoute)
                  .WithMany()
                  .HasForeignKey(s => s.CurrentRouteId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var routeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        modelBuilder.Entity<Route>().HasData(new Route
        {
            Id = routeId,
            OriginStation = "Sé",
            DestinationStation = "Paulista"
        });

        modelBuilder.Entity<Checkpoint>().HasData(
            new Checkpoint
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                RouteId = routeId,
                Order = 1,
                ImageUrl = "https://placehold.co/600x400?text=Checkpoint+1+-+Plataforma+Se",
                Instruction = "Você está na plataforma da estação Sé. Siga em direção à saída indicada pela placa 'Linha 1 - Azul, sentido Tucuruvi'. Suba a escada rolante à sua esquerda."
            },
            new Checkpoint
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RouteId = routeId,
                Order = 2,
                ImageUrl = "https://placehold.co/600x400?text=Checkpoint+2+-+Corredor",
                Instruction = "Após subir a escada rolante, siga pelo corredor principal. Você verá uma banca de jornais à direita. Continue reto por aproximadamente 50 metros."
            },
            new Checkpoint
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                RouteId = routeId,
                Order = 3,
                ImageUrl = "https://placehold.co/600x400?text=Checkpoint+3+-+Embarque",
                Instruction = "Você chegou à plataforma da Linha 1 - Azul. Aguarde o trem no sentido Tucuruvi. Embarque e desça na próxima estação: Paraíso. De lá, faça a transferência para a Linha 2 - Verde sentido Vila Madalena."
            },
            new Checkpoint
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                RouteId = routeId,
                Order = 4,
                ImageUrl = "https://placehold.co/600x400?text=Checkpoint+4+-+Paraiso",
                Instruction = "Você está na estação Paraíso. Siga as placas indicando 'Linha 2 - Verde'. Desça a escada à direita e siga pelo corredor de transferência."
            },
            new Checkpoint
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                RouteId = routeId,
                Order = 5,
                ImageUrl = "https://placehold.co/600x400?text=Checkpoint+5+-+Paulista",
                Instruction = "Embarque no trem da Linha 2 - Verde sentido Vila Madalena. A próxima estação é a Paulista (Consolação). Você chegou ao seu destino! 🎉"
            }
        );
    }
}
