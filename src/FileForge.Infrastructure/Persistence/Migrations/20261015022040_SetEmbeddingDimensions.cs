using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace FileForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetEmbeddingDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "document_chunks",
                type: "vector(1536)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "document_chunks",
                type: "vector",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(1536)");
        }
    }
}
