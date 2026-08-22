using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822090000_AddCompanyStructure")]
public partial class AddCompanyStructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CompanyStructureDepartments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyOwnerUserId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyStructureDepartments", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyStructureDepartments_Users_CompanyOwnerUserId",
                    column: item => item.CompanyOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CompanyStructureDivisions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DepartmentId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyStructureDivisions", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyStructureDivisions_CompanyStructureDepartments_DepartmentId",
                    column: item => item.DepartmentId,
                    principalTable: "CompanyStructureDepartments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CompanyStructurePositions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DivisionId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyStructurePositions", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyStructurePositions_CompanyStructureDivisions_DivisionId",
                    column: item => item.DivisionId,
                    principalTable: "CompanyStructureDivisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyStructureDepartments_Owner_SortOrder",
            table: "CompanyStructureDepartments",
            columns: new[] { "CompanyOwnerUserId", "SortOrder" });
        migrationBuilder.CreateIndex(
            name: "UX_CompanyStructureDepartments_Owner_Name",
            table: "CompanyStructureDepartments",
            columns: new[] { "CompanyOwnerUserId", "Name" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_CompanyStructureDivisions_Department_SortOrder",
            table: "CompanyStructureDivisions",
            columns: new[] { "DepartmentId", "SortOrder" });
        migrationBuilder.CreateIndex(
            name: "UX_CompanyStructureDivisions_Department_Name",
            table: "CompanyStructureDivisions",
            columns: new[] { "DepartmentId", "Name" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_CompanyStructurePositions_Division_SortOrder",
            table: "CompanyStructurePositions",
            columns: new[] { "DivisionId", "SortOrder" });
        migrationBuilder.CreateIndex(
            name: "UX_CompanyStructurePositions_Division_Name",
            table: "CompanyStructurePositions",
            columns: new[] { "DivisionId", "Name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanyStructurePositions");
        migrationBuilder.DropTable(name: "CompanyStructureDivisions");
        migrationBuilder.DropTable(name: "CompanyStructureDepartments");
    }
}
