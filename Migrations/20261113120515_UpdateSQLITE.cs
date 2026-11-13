using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvidenceX.Migrations;

/// <inheritdoc />
public partial class _20261113120515_UpdateSQLITE : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PayloadJson",
            table: "AuditEvents",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "jsonb");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PayloadJson",
            table: "AuditEvents",
            type: "jsonb",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT");
    }
}
