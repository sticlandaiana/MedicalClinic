using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogAndNurseDoctorAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REQ-10: AuditLogs table - creat doar dacă nu există
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
                BEGIN
                    CREATE TABLE [AuditLogs] (
                        [AuditLogId]  INT IDENTITY(1,1) NOT NULL,
                        [UserId]      NVARCHAR(MAX) NULL,
                        [UserEmail]   NVARCHAR(MAX) NULL,
                        [Action]      NVARCHAR(MAX) NULL,
                        [EntityType]  NVARCHAR(MAX) NULL,
                        [Details]     NVARCHAR(MAX) NULL,
                        [Timestamp]   DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
                    );
                END
            ");

            // REQ-8: NurseDoctorAssignments table - creat doar dacă nu există
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NurseDoctorAssignments')
                BEGIN
                    CREATE TABLE [NurseDoctorAssignments] (
                        [NurseDoctorAssignmentId] INT IDENTITY(1,1) NOT NULL,
                        [NurseId]  INT NOT NULL,
                        [DoctorId] INT NOT NULL,
                        CONSTRAINT [PK_NurseDoctorAssignments] PRIMARY KEY ([NurseDoctorAssignmentId]),
                        CONSTRAINT [FK_NDA_Nurses]  FOREIGN KEY ([NurseId])  REFERENCES [Nurses]([NurseId]),
                        CONSTRAINT [FK_NDA_Doctors] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors]([DoctorId])
                    );
                    CREATE INDEX [IX_NurseDoctorAssignments_NurseId]  ON [NurseDoctorAssignments]([NurseId]);
                    CREATE INDEX [IX_NurseDoctorAssignments_DoctorId] ON [NurseDoctorAssignments]([DoctorId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NurseDoctorAssignments') DROP TABLE [NurseDoctorAssignments];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs') DROP TABLE [AuditLogs];");
        }
    }
}
