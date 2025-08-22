# Инициализиране на роли в продукционната база

Тъй като `SeedData.cs` е премахнат, трябва ръчно да създадеш ролите в базата данни.

## SQL скрипт за създаване на роли:

```sql
-- Създаване на ролите (изпълни само веднъж)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
END

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Teacher')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Teacher', 'TEACHER', NEWID());
END

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Student')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Student', 'STUDENT', NEWID());
END

-- Проверка на създадените роли
SELECT Id, Name, NormalizedName FROM AspNetRoles;
```

## Стъпки за изпълнение:

1. **Стартирай приложението** с продукционната конфигурация
2. **Изпълни миграциите** (ако не са изпълнени):
   ```bash
   dotnet ef database update --environment Production
   ```
3. **Изпълни SQL скрипта** за създаване на ролите
4. **Създай админ потребител** ръчно чрез регистрация

## Създаване на админ потребител:

1. Отиди на `/Account/Register` и създай акаунт
2. Потвърди имейла
3. Ръчно добави ролята "Admin" в базата данни:

```sql
-- Намери User ID-то на създадения потребител
SELECT Id, UserName, Email FROM AspNetUsers WHERE Email = 'admin@codegrade.com';

-- Добави ролята Admin (замени USER_ID с реалното ID)
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 'USER_ID', Id FROM AspNetRoles WHERE Name = 'Admin';

-- Активирай потребителя
UPDATE AspNetUsers 
SET IsActive = 1, EmailConfirmed = 1 
WHERE Email = 'admin@codegrade.com';
```

## Важно:
- Ролите трябва да съществуват преди да можеш да добавиш потребители към тях
- Изпълни скрипта само веднъж
- Замени `YOUR_DB_PASSWORD` с реалната парола
- Базата данни ще започне чиста без тестови данни
