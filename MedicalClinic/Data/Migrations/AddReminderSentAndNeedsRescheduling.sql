-- Migrație manuală: adaugă coloana ReminderSent pe tabelul Appointments
-- Rulați acest script în SQL Server Management Studio dacă folosiți o bază de date existentă

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Appointments' AND COLUMN_NAME = 'ReminderSent'
)
BEGIN
    ALTER TABLE Appointments ADD ReminderSent BIT NOT NULL DEFAULT 0;
    PRINT 'Coloana ReminderSent adăugată cu succes.';
END
ELSE
BEGIN
    PRINT 'Coloana ReminderSent există deja.';
END
