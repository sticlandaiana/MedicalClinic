-- ============================================================
-- Fix: Adaugă tabelele AuditLogs și NurseDoctorAssignments
-- Rulează în SSMS conectat la baza de date MedicalClinic
-- ============================================================

-- AuditLogs (REQ-10)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId]  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]      NVARCHAR(MAX) NULL,
        [UserEmail]   NVARCHAR(MAX) NULL,
        [Action]      NVARCHAR(MAX) NULL,
        [EntityType]  NVARCHAR(MAX) NULL,
        [Details]     NVARCHAR(MAX) NULL,
        [Timestamp]   DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabel AuditLogs creat.';
END
ELSE
    PRINT 'AuditLogs exista deja.';

-- NurseDoctorAssignments (REQ-8)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NurseDoctorAssignments')
BEGIN
    CREATE TABLE [NurseDoctorAssignments] (
        [NurseDoctorAssignmentId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NurseId]  INT NOT NULL,
        [DoctorId] INT NOT NULL,
        CONSTRAINT FK_NDA_Nurses  FOREIGN KEY ([NurseId])  REFERENCES [Nurses]([NurseId]),
        CONSTRAINT FK_NDA_Doctors FOREIGN KEY ([DoctorId]) REFERENCES [Doctors]([DoctorId])
    );
    CREATE INDEX IX_NurseDoctorAssignments_NurseId  ON [NurseDoctorAssignments]([NurseId]);
    CREATE INDEX IX_NurseDoctorAssignments_DoctorId ON [NurseDoctorAssignments]([DoctorId]);
    PRINT 'Tabel NurseDoctorAssignments creat.';
END
ELSE
    PRINT 'NurseDoctorAssignments exista deja.';

-- Înregistrează migrația în EF Core
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260427000000_AddAuditLogAndNurseDoctorAssignment')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260427000000_AddAuditLogAndNurseDoctorAssignment', '10.0.5');
    PRINT 'Migratie inregistrata.';
END
