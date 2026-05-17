-- ============================================================
-- RULEAZĂ ACESTA ÎNAINTE de Update-Database
-- Marchează migrațiile deja aplicate manual ca să nu le
-- mai execute EF Core a doua oară
-- ============================================================

-- Pasul 1: Marchează migrația duplicată ca aplicată
-- (coloana ReminderSent există deja în DB din aplicarea manuală)
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = '20260426170709_AddReminderSent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260426170709_AddReminderSent', '10.0.5');
    PRINT 'Marcat: 20260426170709_AddReminderSent';
END
ELSE
    PRINT 'Deja marcat: 20260426170709_AddReminderSent';

-- Pasul 2: Dacă ai aplicat manual AuditLogs + NurseDoctorAssignments,
-- marchează și acestea (decomentează dacă tabelele există deja în DB)
/*
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = '20260427000000_AddAuditLogAndNurseDoctorAssignment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260427000000_AddAuditLogAndNurseDoctorAssignment', '10.0.5');
    PRINT 'Marcat: 20260427000000';
END
*/

-- Pasul 3: Verificare - ce migrații sunt aplicate?
SELECT [MigrationId], [ProductVersion] FROM [__EFMigrationsHistory] ORDER BY [MigrationId];
