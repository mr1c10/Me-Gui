using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MeGui.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginStation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DestinationStation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentRouteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentCheckpointOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.ChatId);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Routes_CurrentRouteId",
                        column: x => x.CurrentRouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Instruction = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "Id", "DestinationStation", "OriginStation" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "Paulista", "Sé" });

            migrationBuilder.InsertData(
                table: "Checkpoints",
                columns: new[] { "Id", "ImageUrl", "Instruction", "Order", "RouteId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "https://placehold.co/600x400?text=Checkpoint+1+-+Plataforma+Se", "Você está na plataforma da estação Sé. Siga em direção à saída indicada pela placa 'Linha 1 - Azul, sentido Tucuruvi'. Suba a escada rolante à sua esquerda.", 1, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "https://placehold.co/600x400?text=Checkpoint+2+-+Corredor", "Após subir a escada rolante, siga pelo corredor principal. Você verá uma banca de jornais à direita. Continue reto por aproximadamente 50 metros.", 2, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "https://placehold.co/600x400?text=Checkpoint+3+-+Embarque", "Você chegou à plataforma da Linha 1 - Azul. Aguarde o trem no sentido Tucuruvi. Embarque e desça na próxima estação: Paraíso. De lá, faça a transferência para a Linha 2 - Verde sentido Vila Madalena.", 3, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "https://placehold.co/600x400?text=Checkpoint+4+-+Paraiso", "Você está na estação Paraíso. Siga as placas indicando 'Linha 2 - Verde'. Desça a escada à direita e siga pelo corredor de transferência.", 4, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "https://placehold.co/600x400?text=Checkpoint+5+-+Paulista", "Embarque no trem da Linha 2 - Verde sentido Vila Madalena. A próxima estação é a Paulista (Consolação). Você chegou ao seu destino! 🎉", 5, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CurrentRouteId",
                table: "ChatSessions",
                column: "CurrentRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_RouteId",
                table: "Checkpoints",
                column: "RouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Checkpoints");

            migrationBuilder.DropTable(
                name: "Routes");
        }
    }
}
