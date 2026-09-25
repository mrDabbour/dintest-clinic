using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dentist_clinic_api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToDentalServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DentalServices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "DentalServices");
        }
    }
}
