using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalClinic.Migrations
{
    /// <inheritdoc />
    public partial class RenameNurseDoctorAssignmentPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = 'NurseDoctorAssignments'
        )
        BEGIN
            CREATE TABLE [NurseDoctorAssignments] (
                [NurseDoctorAssignmentId] INT IDENTITY(1,1) NOT NULL,
                [NurseId] INT NOT NULL,
                [DoctorId] INT NOT NULL,
                CONSTRAINT [PK_NurseDoctorAssignments] PRIMARY KEY ([NurseDoctorAssignmentId]),
                CONSTRAINT [FK_NurseDoctorAssignments_Nurses_NurseId] 
                    FOREIGN KEY ([NurseId]) REFERENCES [Nurses]([NurseId]) ON DELETE NO ACTION,
                CONSTRAINT [FK_NurseDoctorAssignments_Doctors_DoctorId] 
                    FOREIGN KEY ([DoctorId]) REFERENCES [Doctors]([DoctorId]) ON DELETE NO ACTION
            );
        END
        ELSE IF EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'NurseDoctorAssignments' 
            AND COLUMN_NAME = 'Id'
        )
        BEGIN
            EXEC sp_rename 'NurseDoctorAssignments.Id', 'NurseDoctorAssignmentId', 'COLUMN';
        END
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NurseDoctorAssignmentId",
                table: "NurseDoctorAssignments",
                newName: "Id");
        }
    }
}
