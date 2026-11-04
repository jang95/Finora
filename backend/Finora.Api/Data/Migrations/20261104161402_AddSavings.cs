using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Finora.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Savings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BankName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaturityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InterestRate = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    InterestMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PlannedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Memo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Savings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Savings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavingEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SavingId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Memo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavingEntries_Savings_SavingId",
                        column: x => x.SavingId,
                        principalTable: "Savings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavingEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavingEntries_SavingId_Opening",
                table: "SavingEntries",
                column: "SavingId",
                unique: true,
                filter: "\"Type\" = 'Opening'");

            migrationBuilder.CreateIndex(
                name: "IX_SavingEntries_TransactionId",
                table: "SavingEntries",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingEntries_UserId_Date",
                table: "SavingEntries",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Savings_UserId",
                table: "Savings",
                column: "UserId");

            // [직접 쓴 SQL] 이미 가입한 사용자에게 "이자" 수입 대분류를 넣는다 (결정 D24).
            // 새 사용자는 가입할 때 DefaultCategories가 넣지만, 기존 사용자는 테이블 변경만으로는 생기지 않는다.
            // 그래서 자동 생성된 코드 뒤에 migrationBuilder.Sql로 데이터를 넣는 SQL을 직접 붙였다.
            // - NOT EXISTS: 사용자가 이미 "이자" 대분류를 만들어 뒀으면 건너뛴다 (고유 인덱스 충돌 방지)
            // - SortOrder: 그 사용자의 수입 대분류 중 가장 큰 값 + 1 → 맨 뒤에 붙는다
            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("UserId", "ParentId", "Name", "Type", "SortOrder", "CreatedAt", "UpdatedAt")
                SELECT u."Id", NULL, '이자', 'Income',
                       COALESCE((SELECT MAX(c."SortOrder") + 1 FROM "Categories" c
                                 WHERE c."UserId" = u."Id" AND c."Type" = 'Income' AND c."ParentId" IS NULL), 0),
                       now(), now()
                FROM "AspNetUsers" u
                WHERE NOT EXISTS (SELECT 1 FROM "Categories" c
                                  WHERE c."UserId" = u."Id" AND c."Type" = 'Income'
                                    AND c."ParentId" IS NULL AND c."Name" = '이자');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 넣은 "이자" 카테고리는 되돌릴 때 지우지 않는다. 그사이 거래에 쓰였을 수 있어서다.

            migrationBuilder.DropTable(
                name: "SavingEntries");

            migrationBuilder.DropTable(
                name: "Savings");
        }
    }
}
