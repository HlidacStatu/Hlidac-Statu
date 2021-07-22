# Veřejné repository Hlídače Státu

## Jak rozběhnout projekt

1. vzít starou db Firmy
2. scaffold (https://docs.microsoft.com/en-us/ef/core/managing-schemas/scaffolding?tabs=dotnet-core-cli)
3. ručně fixnout scaffold podle produkce (db.edmx) - někde se tam volají uložené procedury (db.context.cs)
4. přidat migraci `dotnet ef migrations add initial --project LibCore`
5. do vygenerovaného migračního skriptu přidat na začátek metody Up
```
protected override void Up(MigrationBuilder migrationBuilder)
{
    //migrace: nejprve přejmenovat stávající tabulky 
    // AspNetUsers
    // AspNetRoles
    // AspNetUserClaims
    // AspNetUserLogins
    // AspNetUserRoles
    // na "_old"
    // a tabulku AspNetUserTokens přejmenovat na AspNetUserApiTokens   
    
    //migrace: změnit skript dole tak, aby from bylo ze stejné tabulky, ale každá tabulka bude mít _old koncovku
    
    migrationBuilder.Sql(
        @"insert into AspNetUsers 
        select Id, UserName, UPPER(UserName), 
               Email, UPPER(email), EmailConfirmed, 
               PasswordHash, SecurityStamp, NEWID(), 
               PhoneNumber, PhoneNumberConfirmed, 
               TwoFactorEnabled, LockoutEndDateUtc, 
               LockoutEnabled, AccessFailedCount
          FROM [Firmy].[dbo].[AspNetUsers];"
    );

    migrationBuilder.Sql(
        @"insert into AspNetRoles
        select Id, Name, UPPER(Name), NEWID()
        from [Firmy].[dbo].[AspNetRoles];"
    );

    migrationBuilder.Sql(
        @"insert into AspNetUserClaims(UserId, ClaimType, ClaimValue)
        select UserId, ClaimType, ClaimValue
        from [Firmy].[dbo].[AspNetUserClaims];"
    );
    
    migrationBuilder.Sql(
        @"insert into AspNetUserLogins
        select LoginProvider, ProviderKey, LoginProvider, UserId
        from [Firmy].[dbo].[AspNetUserLogins]"
    );

    migrationBuilder.Sql(
        @"insert into AspNetUserRoles
        select * from [Firmy].[dbo].[AspNetUserRoles];"
    );
    
    //migrationBuilder.Sql(
    //    @"insert into AspNetUserApiTokens
    //    select * from [Firmy].[dbo].[AspNetUserTokens];"
    //);
    
}

```