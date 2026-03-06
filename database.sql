CREATE TABLE IF NOT EXISTS ParkingRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TagNumber TEXT NOT NULL,
    CheckIn TEXT NOT NULL,
    CheckOut TEXT NULL,
    AmountCharged REAL NULL
);

CREATE INDEX IF NOT EXISTS IX_Tag ON ParkingRecords (TagNumber);
CREATE INDEX IF NOT EXISTS IX_CheckOut ON ParkingRecords (CheckOut);
