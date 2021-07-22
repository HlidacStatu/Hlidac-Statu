BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId];
GO

ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId];
GO

ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId];
GO

ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId];
GO

ALTER TABLE [AspNetRoles] DROP CONSTRAINT [PK_dbo.AspNetRoles];
GO

ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [PK_dbo.AspNetUserRoles];
GO

ALTER TABLE [AspNetUsers] DROP CONSTRAINT [PK_dbo.AspNetUsers];
GO

ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [PK_dbo.AspNetUserClaims];
GO

ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [PK_dbo.AspNetUserLogins];
GO

ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [PK_AspNetUserTokens];
GO

EXEC sp_rename N'[AspNetUserRoles]', N'AspNetUserRoles_old';
GO

EXEC sp_rename N'[AspNetUserClaims]', N'AspNetUserClaims_old';
GO

EXEC sp_rename N'[AspNetUserLogins]', N'AspNetUserLogins_old';
GO

EXEC sp_rename N'[AspNetRoles]', N'AspNetRoles_old';
GO

EXEC sp_rename N'[AspNetUsers]', N'AspNetUsers_old';
GO

EXEC sp_rename N'[AspNetUserTokens]', N'AspNetUserTokens_old';
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUserApiTokens] (
    [Id] nvarchar(450) NOT NULL,
    [Token] uniqueidentifier NOT NULL,
    [Created] datetime2 NOT NULL,
    [LastAccess] datetime2 NULL,
    [Count] int NOT NULL,
    CONSTRAINT [PK_AspNetUserApiTokens] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

insert into AspNetUsers 
                select Id, UserName, UPPER(UserName), 
                       Email, UPPER(email), EmailConfirmed, 
	                   PasswordHash, SecurityStamp, NEWID(), 
	                   PhoneNumber, PhoneNumberConfirmed, 
	                   TwoFactorEnabled, LockoutEndDateUtc, 
	                   LockoutEnabled, AccessFailedCount
                  FROM AspNetUsers_old;
GO

insert into AspNetRoles
                select Id, Name, UPPER(Name), NEWID()
                from AspNetRoles_old;
GO

insert into AspNetUserClaims(UserId, ClaimType, ClaimValue)
                select UserId, ClaimType, ClaimValue
                from AspNetUserClaims_old;
GO

insert into AspNetUserLogins
                select LoginProvider, ProviderKey, LoginProvider, UserId
                from AspNetUserLogins_old
GO

insert into AspNetUserRoles
                select * from AspNetUserRoles_old;
GO

insert into AspNetUserApiTokens
                select * from AspNetUserTokens_old;
GO

COMMIT;
GO

