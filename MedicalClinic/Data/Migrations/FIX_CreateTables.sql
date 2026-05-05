-- ============================================================
-- RULEAZĂ ACESTA ÎN SSMS - creează tabelele lipsă
-- și marchează toate migrațiile ca aplicate
-- ============================================================

-- 1. AuditLogs
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId]  INT IDENTITY(1,1) NOT NULL,
        [UserId]      NVARCHAR(MAX) NOT NULL DEFAULT '',
        [UserEmail]   NVARCHAR(MAX) NOT NULL DEFAULT '',
        [Action]      NVARCHAR(MAX) NOT NULL DEFAULT '',
        [EntityType]  NVARCHAR(MAX) NOT NULL DEFAULT '',
        [Details]     NVARCHAR(MAX) NOT NULL DEFAULT '',
        [Timestamp]   DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
    );
    PRINT 'Tabel AuditLogs creat.';
END
ELSE
    PRINT 'AuditLogs exista deja.';

-- 2. NurseDoctorAssignments
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
    PRINT 'Tabel NurseDoctorAssignments creat.';
END
ELSE
    PRINT 'NurseDoctorAssignments exista deja.';

-- 3. Marchează TOATE migrațiile ca aplicate
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260426170709_AddReminderSent')
    INSERT INTO [__EFMigrationsHistory] VALUES ('20260426170709_AddReminderSent', '10.0.5');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260427000000_AddAuditLogAndNurseDoctorAssignment')
    INSERT INTO [__EFMigrationsHistory] VALUES ('20260427000000_AddAuditLogAndNurseDoctorAssignment', '10.0.5');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260503132730_AddAuditLogs')
    INSERT INTO [__EFMigrationsHistory] VALUES ('20260503132730_AddAuditLogs', '10.0.5');

-- 4. Verificare finală
PRINT '';
PRINT '=== Migrații aplicate ===';
SELECT [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId];

PRINT '';
PRINT '=== Tabele existente ===';
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;
