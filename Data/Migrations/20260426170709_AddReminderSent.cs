using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalClinic.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderSent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'Appointments' 
            AND COLUMN_NAME = 'ReminderSent'
        )
        BEGIN
            ALTER TABLE [Appointments] ADD [ReminderSent] BIT NOT NULL DEFAULT 0;
        END
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op corespunzător
        }
    }
}
