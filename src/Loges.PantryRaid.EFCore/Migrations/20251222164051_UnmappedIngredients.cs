using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loges.PantryRaid.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UnmappedIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnmappedIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    RecipeSourceId = table.Column<int>(type: "int", nullable: false),
                    OriginalText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SuggestedIngredientId = table.Column<int>(type: "int", nullable: true),
                    ResolvedIngredientId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmappedIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnmappedIngredients_Ingredients_ResolvedIngredientId",
                        column: x => x.ResolvedIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnmappedIngredients_Ingredients_SuggestedIngredientId",
                        column: x => x.SuggestedIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnmappedIngredients_RecipeSources_RecipeSourceId",
                        column: x => x.RecipeSourceId,
                        principalTable: "RecipeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnmappedIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedIngredients_RecipeId",
                table: "UnmappedIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedIngredients_RecipeSourceId",
                table: "UnmappedIngredients",
                column: "RecipeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedIngredients_ResolvedIngredientId",
                table: "UnmappedIngredients",
                column: "ResolvedIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedIngredients_SuggestedIngredientId",
                table: "UnmappedIngredients",
                column: "SuggestedIngredientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnmappedIngredients");
        }
    }
}
