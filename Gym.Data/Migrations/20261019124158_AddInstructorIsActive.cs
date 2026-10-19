using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gym.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Instructors",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Instructors");
        }
    }
}
