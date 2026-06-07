using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddJobHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Seniorities_SenioritiesId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniorities_JobFamilies_JobFamiliesId",
                table: "Seniorities");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Positions_PositionsId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_PositionsId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "PositionsId",
                table: "Skills");

            migrationBuilder.RenameColumn(
                name: "JobFamiliesId",
                table: "Seniorities",
                newName: "JobFamilyId");

            migrationBuilder.RenameIndex(
                name: "IX_Seniorities_JobFamiliesId",
                table: "Seniorities",
                newName: "IX_Seniorities_JobFamilyId");

            migrationBuilder.RenameColumn(
                name: "SenioritiesId",
                table: "Positions",
                newName: "SeniorityId");

            migrationBuilder.RenameIndex(
                name: "IX_Positions_SenioritiesId",
                table: "Positions",
                newName: "IX_Positions_SeniorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_PositionId",
                table: "Skills",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Seniorities_SeniorityId",
                table: "Positions",
                column: "SeniorityId",
                principalTable: "Seniorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seniorities_JobFamilies_JobFamilyId",
                table: "Seniorities",
                column: "JobFamilyId",
                principalTable: "JobFamilies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Positions_PositionId",
                table: "Skills",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Seniorities_SeniorityId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniorities_JobFamilies_JobFamilyId",
                table: "Seniorities");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Positions_PositionId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_PositionId",
                table: "Skills");

            migrationBuilder.RenameColumn(
                name: "JobFamilyId",
                table: "Seniorities",
                newName: "JobFamiliesId");

            migrationBuilder.RenameIndex(
                name: "IX_Seniorities_JobFamilyId",
                table: "Seniorities",
                newName: "IX_Seniorities_JobFamiliesId");

            migrationBuilder.RenameColumn(
                name: "SeniorityId",
                table: "Positions",
                newName: "SenioritiesId");

            migrationBuilder.RenameIndex(
                name: "IX_Positions_SeniorityId",
                table: "Positions",
                newName: "IX_Positions_SenioritiesId");

            migrationBuilder.AddColumn<int>(
                name: "PositionsId",
                table: "Skills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_PositionsId",
                table: "Skills",
                column: "PositionsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Seniorities_SenioritiesId",
                table: "Positions",
                column: "SenioritiesId",
                principalTable: "Seniorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seniorities_JobFamilies_JobFamiliesId",
                table: "Seniorities",
                column: "JobFamiliesId",
                principalTable: "JobFamilies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Positions_PositionsId",
                table: "Skills",
                column: "PositionsId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
