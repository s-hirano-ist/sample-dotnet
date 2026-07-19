using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Todos_CreatedAt_Id",
                table: "Todos",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Todos_IsDone_CreatedAt_Id",
                table: "Todos",
                columns: new[] { "IsDone", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Todos_Title_Id",
                table: "Todos",
                columns: new[] { "Title", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Todos_CreatedAt_Id",
                table: "Todos");

            migrationBuilder.DropIndex(
                name: "IX_Todos_IsDone_CreatedAt_Id",
                table: "Todos");

            migrationBuilder.DropIndex(
                name: "IX_Todos_Title_Id",
                table: "Todos");
        }
    }
}
