using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PokedexApi.Migrations
{
    public partial class v3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BattleHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Player1Id = table.Column<int>(type: "int", nullable: false),
                    Player2Id = table.Column<int>(type: "int", nullable: false),
                    WinnerId = table.Column<int>(type: "int", nullable: true),
                    Player1TeamId = table.Column<int>(type: "int", nullable: false),
                    Player2TeamId = table.Column<int>(type: "int", nullable: false),
                    TotalTurns = table.Column<int>(type: "int", nullable: false),
                    BattleLog = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    ReportReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleHistories_Teams_Player1TeamId",
                        column: x => x.Player1TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleHistories_Teams_Player2TeamId",
                        column: x => x.Player2TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleHistories_Users_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleHistories_Users_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleHistories_Users_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BattleSessions",
                columns: table => new
                {
                    BattleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Player1Id = table.Column<int>(type: "int", nullable: false),
                    Player1SocketId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Player2Id = table.Column<int>(type: "int", nullable: true),
                    Player2SocketId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Player1TeamId = table.Column<int>(type: "int", nullable: false),
                    Player2TeamId = table.Column<int>(type: "int", nullable: true),
                    BattleState = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleSessions", x => x.BattleId);
                });

            migrationBuilder.CreateTable(
                name: "MatchmakingQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    SocketId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchmakingQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchmakingQueues_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchmakingQueues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Peak = table.Column<int>(type: "int", nullable: false),
                    TotalBattles = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    LongestWinStreak = table.Column<int>(type: "int", nullable: false),
                    LastBattleAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BattleId = table.Column<int>(type: "int", nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleReports_BattleHistories_BattleId",
                        column: x => x.BattleId,
                        principalTable: "BattleHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BattleReports_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleHistories_Player1Id",
                table: "BattleHistories",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_BattleHistories_Player1TeamId",
                table: "BattleHistories",
                column: "Player1TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleHistories_Player2Id",
                table: "BattleHistories",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_BattleHistories_Player2TeamId",
                table: "BattleHistories",
                column: "Player2TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleHistories_WinnerId",
                table: "BattleHistories",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleReports_BattleId",
                table: "BattleReports",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleReports_ReportedById",
                table: "BattleReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingQueues_TeamId",
                table: "MatchmakingQueues",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingQueues_UserId",
                table: "MatchmakingQueues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRatings_UserId",
                table: "UserRatings",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleReports");

            migrationBuilder.DropTable(
                name: "BattleSessions");

            migrationBuilder.DropTable(
                name: "MatchmakingQueues");

            migrationBuilder.DropTable(
                name: "UserRatings");

            migrationBuilder.DropTable(
                name: "BattleHistories");
        }
    }
}
