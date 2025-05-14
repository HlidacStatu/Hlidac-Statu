CREATE TABLE PoliticiEditorUsers (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     NameId NVARCHAR(255) NOT NULL,
                                     Email NVARCHAR(255) NULL,
                                     EmailUpper NVARCHAR(255) NULL,
                                     PhoneNumber NVARCHAR(50) NULL,
                                     Name NVARCHAR(255) NULL,
                                     LastLogin DATETIME NULL,
                                     IsLockedOut BIT NOT NULL DEFAULT(0)
);

CREATE UNIQUE INDEX IX_PoliticiEditorUsers_NameId
    ON PoliticiEditorUsers (NameId);

CREATE INDEX IX_PoliticiEditorUsers_EmailUpper
    ON PoliticiEditorUsers (EmailUpper);

---------------------------------------------------

CREATE TABLE LoginTokens (
                             Id INTEGER PRIMARY KEY AUTOINCREMENT,
                             Token TEXT NOT NULL,
                             UserId INTEGER NOT NULL,
                             CreatedAt DATETIME NOT NULL,
                             ExpiresAt DATETIME NOT NULL,
                             Used INTEGER NOT NULL DEFAULT 0,
                             FOREIGN KEY (UserId) REFERENCES PoliticiEditorUsers(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_LoginTokens_Token
    ON LoginTokens (Token);

INSERT INTO PoliticiEditorUsers (NameId, Email, EmailUpper, Name)
VALUES ('ivan-bartos', 'petr@hlidacstatu.cz', 'PETR@HLIDACSTATU.CZ', 'admin'); 