using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalClinic.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter AuditLogs columns to NOT NULL - safe idempotent via SQL
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
                BEGIN
                    ALTER TABLE [AuditLogs] ALTER COLUMN [UserId]     NVARCHAR(MAX) NOT NULL;
                    ALTER TABLE [AuditLogs] ALTER COLUMN [UserEmail]  NVARCHAR(MAX) NOT NULL;
                    ALTER TABLE [AuditLogs] ALTER COLUMN [EntityType] NVARCHAR(MAX) NOT NULL;
                    ALTER TABLE [AuditLogs] ALTER COLUMN [Details]    NVARCHAR(MAX) NOT NULL;
                    ALTER TABLE [AuditLogs] ALTER COLUMN [Action]     NVARCHAR(MAX) NOT NULL;
                END
            ");

            // Add FK on NurseDoctorAssignments - only if not already present
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NurseDoctorAssignments'
                ) AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NurseDoctorAssignments_Doctors_DoctorId'
                )
                BEGIN
                    ALTER TABLE [NurseDoctorAssignments]
                    ADD CONSTRAINT [FK_NurseDoctorAssignments_Doctors_DoctorId]
                    FOREIGN KEY ([DoctorId]) REFERENCES [Doctors]([DoctorId]) ON DELETE NO ACTION;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NurseDoctorAssignments'
                ) AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NurseDoctorAssignments_Nurses_NurseId'
                )
                BEGIN
                    ALTER TABLE [NurseDoctorAssignments]
                    ADD CONSTRAINT [FK_NurseDoctorAssignments_Nurses_NurseId]
                    FOREIGN KEY ([NurseId]) REFERENCES [Nurses]([NurseId]) ON DELETE NO ACTION;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NurseDoctorAssignments_Doctors_DoctorId')
                    ALTER TABLE [NurseDoctorAssignments] DROP CONSTRAINT [FK_NurseDoctorAssignments_Doctors_DoctorId];
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NurseDoctorAssignments_Nurses_NurseId')
                    ALTER TABLE [NurseDoctorAssignments] DROP CONSTRAINT [FK_NurseDoctorAssignments_Nurses_NurseId];
            ");
        }
    }
}
