using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlGhaniMedicalStore.Migrations
{
    /// <inheritdoc />
    public partial class PieceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesStrips",
                table: "Medicines",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsesStrips",
                table: "Medicines");
        }
    }
}
