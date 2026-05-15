/* =============================================================================
   ИС «Клик Медиа» — демонстрационное наполнение БД ClickMediaTimeDB
   Файл: Sql\02_seed_demo_data.sql

   Запуск:
     1) Выполните 01_create_schema.sql (или запустите приложение один раз).
     2) Выполните этот скрипт в SSMS / Azure Data Studio / sqlcmd.

   @ReplaceDemoData = 1 — удалить данные с меткой [seed:v2] и загрузить снова.
   @ReplaceDemoData = 0 — дозаполнить недостающее (можно запускать повторно).

   Объёмы:
     ~10 подразделений, ~12 должностей
     ~30 сотрудников, ~25 проектов
     100 записей учёта времени, 100 строк аудита
     ~50 попыток входа, ~8 резервных копий (метаданные), ~15 ErrorLog, ~10 сессий

   Вход: admin / manager / employee, пароль Admin123! (создаёт приложение).
   ============================================================================= */
USE ClickMediaTimeDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ReplaceDemoData BIT = 0;   /* <-- 1 = перезалить демо-данные */
DECLARE @SeedTag NVARCHAR(32) = N'[seed:v2]';
DECLARE @AdminUserId INT = (SELECT TOP 1 Id FROM dbo.Users WHERE LoginName = N'admin' AND IsDeleted = 0);

/* --- Удаление демо-данных при перезаливке --- */
IF @ReplaceDemoData = 1
BEGIN
    DELETE FROM dbo.WorkTimeEntries WHERE Comment LIKE N'%' + @SeedTag + N'%';
    DELETE FROM dbo.AuditLog      WHERE NewValue LIKE N'%' + @SeedTag + N'%' OR OldValue LIKE N'%' + @SeedTag + N'%';
    DELETE FROM dbo.LoginAttempts WHERE [Message] LIKE N'%' + @SeedTag + N'%';
    DELETE FROM dbo.Backups       WHERE Comment LIKE N'%' + @SeedTag + N'%';
    DELETE FROM dbo.ErrorLog      WHERE Source LIKE N'%' + @SeedTag + N'%';
    DELETE FROM dbo.Sessions      WHERE IPAddress = N'127.0.0.1-demo';

    DELETE FROM dbo.Users WHERE LoginName LIKE N'demo[_]%' AND IsDeleted = 0;
    DELETE FROM dbo.Employees WHERE PersonnelNumber >= N'КМ-0100' AND PersonnelNumber <= N'КМ-9999';

    DELETE FROM dbo.SystemSettings WHERE ParamName = N'DemoSeed_v2_Applied';
    PRINT N'Демо-данные [seed:v2] удалены.';
END

BEGIN TRANSACTION;

/* Справочник названий проектов для вставки / удаления при перезаливке */
IF OBJECT_ID(N'tempdb..#SeedProjects') IS NOT NULL DROP TABLE #SeedProjects;
CREATE TABLE #SeedProjects
(
    ProjectName   NVARCHAR(250) NOT NULL PRIMARY KEY,
    ClientName    NVARCHAR(250) NOT NULL,
    StatusName    NVARCHAR(120) NOT NULL,
    StartOffset   INT NOT NULL,   /* дней назад от сегодня */
    EndOffset     INT NULL,       /* NULL = без даты окончания */
    PlannedHours  DECIMAL(9,2) NOT NULL
);

INSERT INTO #SeedProjects (ProjectName, ClientName, StatusName, StartOffset, EndOffset, PlannedHours) VALUES
(N'Рекламная кампания «Весна 2026»',           N'Ритейл Холдинг',      N'В работе',      75,  45,  480),
(N'Брендирование соцсетей',                    N'ООО «Старт»',         N'Планирование',  20,  NULL, 120),
(N'Performance-закупка Q2',                    N'Агрегатор услуг',     N'В работе',      40,  50,  600),
(N'TV-ролик 15 сек — запуск модели',           N'АвтоДрайв',           N'В работе',      55,  30,  320),
(N'Таргет VK Ads — лидогенерация',             N'ФармГрупп',           N'В работе',      30,  60,  240),
(N'SEO-аудит и контент-план',                  N'TechNova',            N'Планирование',  15,  90,  160),
(N'SMM «FoodBox» — контент-календарь',         N'FoodBox',             N'В работе',      45,  40,  200),
(N'Медиаплан OLV — премиальная карта',         N'Банк «Север»',        N'В работе',      60,  35,  520),
(N'Имиджевая кампания «Открытие сезона»',      N'TravelLine',          N'Завершён',      120, -10, 380),
(N'Ребрендинг каталога B2B',                   N'СтройКомплект',       N'На паузе',      90,  20,  280),
(N'Контекст Яндекс — федеральная акция',       N'Ритейл Холдинг',      N'В работе',      25,  55,  410),
(N'Influence — блогеры lifestyle',             N'ООО «Старт»',         N'В работе',      35,  45,  190),
(N'Продакшн фото для e-com',                   N'FoodBox',             N'Завершён',      100, -5,  150),
(N'Programmatic-закупка display',            N'Агрегатор услуг',     N'В работе',      50,  40,  350),
(N'Лендинг промо-акции «Май»',                 N'TechNova',            N'Планирование',  10,  70,  140),
(N'Аналитика post-campaign Q1',                N'Банк «Север»',        N'Завершён',      110, -15,  90),
(N'Креатив: key visual и гайдлайн',            N'АвтоДрайв',           N'В работе',      28,  50,  220),
(N'Telegram Ads — тест гипотез',               N'ФармГрупп',           N'На паузе',      40,  30,  80),
(N'Подкаст «Бизнес-лайт» — спонсорство',       N'TravelLine',          N'Планирование',  5,   NULL, 100),
(N'CRM-рассылка и дизайн писем',               N'СтройКомплект',       N'В работе',      22,  48,  130),
(N'Наружная реклама — макеты 6×3',             N'Ритейл Холдинг',      N'Планирование',  18,  80,  170),
(N'Видеопродакшн Reels для бренда',            N'ООО «Старт»',         N'В работе',      12,  65,  210);

/* Убрать устаревшие проекты с префиксом [Демо] */
IF EXISTS (SELECT 1 FROM dbo.Projects WHERE Name LIKE N'[Демо]%')
BEGIN
    DELETE w FROM dbo.WorkTimeEntries w
    INNER JOIN dbo.Projects p ON p.Id = w.ProjectId
    WHERE p.Name LIKE N'[Демо]%';
    DELETE FROM dbo.Projects WHERE Name LIKE N'[Демо]%';
END

IF @ReplaceDemoData = 1
BEGIN
    DELETE w FROM dbo.WorkTimeEntries w
    INNER JOIN dbo.Projects p ON p.Id = w.ProjectId
    WHERE p.Name IN (SELECT ProjectName FROM #SeedProjects);
    DELETE FROM dbo.Projects WHERE Name IN (SELECT ProjectName FROM #SeedProjects);
END

/* ---------- 1. Подразделения (~10) ---------- */
;WITH src AS (
    SELECT * FROM (VALUES
        (N'Дирекция', N'Управление компанией'),
        (N'Продакшн', N'Производство контента'),
        (N'Media Buying', N'Закупка рекламы'),
        (N'Креатив', N'Идеи, дизайн, копирайт'),
        (N'SMM и контент', N'Соцсети и комьюнити'),
        (N'Аналитика', N'Отчётность и BI'),
        (N'Клиентский сервис', N'Аккаунты и сопровождение'),
        (N'HR и администрирование', N'Кадры и офис'),
        (N'Финансы', N'Бюджеты и закрытие периодов'),
        (N'IT и автоматизация', N'Инфраструктура и ИС')
    ) AS v(Name, Description)
)
MERGE dbo.Departments AS t
USING src AS s ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name, Description) VALUES (s.Name, s.Description);

/* ---------- 2. Должности (~12) ---------- */
;WITH src AS (
    SELECT Name FROM (VALUES
        (N'Генеральный директор'), (N'Руководитель отдела'), (N'Медиапланер'),
        (N'Аккаунт-менеджер'), (N'Специалист'), (N'Дизайнер'), (N'Копирайтер'),
        (N'Аналитик данных'), (N'Project-менеджер'), (N'Таргетолог'),
        (N'Видеомонтажёр'), (N'Стажёр')
    ) AS v(Name)
)
MERGE dbo.Positions AS t
USING src AS s ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (s.Name);

/* ---------- 3. Сотрудники (~30) ---------- */
IF (SELECT COUNT(*) FROM dbo.Employees WHERE IsDeleted = 0) < 30
BEGIN
    ;WITH tally AS (
        SELECT TOP (27) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_columns
    ),
    dept AS (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn, COUNT(*) OVER () AS cnt
        FROM dbo.Departments WHERE IsDeleted = 0
    ),
    pos AS (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn, COUNT(*) OVER () AS cnt
        FROM dbo.Positions WHERE IsDeleted = 0
    ),
    names AS (
        SELECT n,
            CASE (n % 15) WHEN 0 THEN N'Алексеев' WHEN 1 THEN N'Борисова' WHEN 2 THEN N'Волков'
                WHEN 3 THEN N'Громова' WHEN 4 THEN N'Дмитриев' WHEN 5 THEN N'Егорова'
                WHEN 6 THEN N'Жуков' WHEN 7 THEN N'Зайцева' WHEN 8 THEN N'Ильин'
                WHEN 9 THEN N'Козлова' WHEN 10 THEN N'Лебедев' WHEN 11 THEN N'Морозов'
                WHEN 12 THEN N'Никитина' WHEN 13 THEN N'Орлов' ELSE N'Павлова' END AS Fam,
            CASE (n % 10) WHEN 0 THEN N'Александр' WHEN 1 THEN N'Мария' WHEN 2 THEN N'Дмитрий'
                WHEN 3 THEN N'Елена' WHEN 4 THEN N'Игорь' WHEN 5 THEN N'Ольга'
                WHEN 6 THEN N'Павел' WHEN 7 THEN N'Татьяна' WHEN 8 THEN N'Николай' ELSE N'Анна' END AS Im,
            CASE (n % 8) WHEN 0 THEN N'Сергеевич' WHEN 1 THEN N'Игоревна' WHEN 2 THEN N'Андреевич'
                WHEN 3 THEN N'Петровна' WHEN 4 THEN N'Викторович' WHEN 5 THEN N'Олеговна'
                WHEN 6 THEN N'Романович' ELSE N'Денисовна' END AS Ot
        FROM tally
    )
    INSERT INTO dbo.Employees (DepartmentId, PositionId, FullName, PersonnelNumber, Phone, Email, HireDate, IsActive)
    SELECT
        d.Id, p.Id,
        nm.Fam + N' ' + nm.Im + N' ' + nm.Ot,
        N'КМ-' + RIGHT(N'0000' + CAST(99 + nm.n AS NVARCHAR(4)), 4),
        N'+7 (999) 2' + RIGHT(N'00' + CAST(nm.n AS NVARCHAR(2)), 2) + N'-10-' + RIGHT(N'00' + CAST(nm.n AS NVARCHAR(2)), 2),
        LOWER(REPLACE(LEFT(nm.Im, 1) + nm.Fam, N'ё', N'e')) + N'@clickmedia.ru',
        DATEADD(MONTH, -((nm.n % 48) + 6), CAST(GETDATE() AS DATE)),
        CASE WHEN nm.n % 17 = 0 THEN 0 ELSE 1 END
    FROM names nm
    INNER JOIN dept d ON d.rn = ((nm.n - 1) % d.cnt) + 1
    INNER JOIN pos p ON p.rn = ((nm.n + 2) % p.cnt) + 1
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Employees e
        WHERE e.PersonnelNumber = N'КМ-' + RIGHT(N'0000' + CAST(99 + nm.n AS NVARCHAR(4)), 4));
END

/* ---------- 4. Проекты (реальные названия кампаний) ---------- */
INSERT INTO dbo.Projects (Name, ClientName, StatusId, StartDate, EndDate, PlannedHours)
SELECT
    sp.ProjectName,
    sp.ClientName,
    ps.Id,
    DATEADD(DAY, -sp.StartOffset, CAST(GETDATE() AS DATE)),
    CASE WHEN sp.EndOffset IS NULL THEN NULL
         WHEN sp.EndOffset < 0 THEN DATEADD(DAY, sp.EndOffset, CAST(GETDATE() AS DATE))
         ELSE DATEADD(DAY, sp.EndOffset, CAST(GETDATE() AS DATE)) END,
    sp.PlannedHours
FROM #SeedProjects sp
INNER JOIN dbo.ProjectStatuses ps ON ps.Name = sp.StatusName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Projects p WHERE p.Name = sp.ProjectName AND p.IsDeleted = 0);

/* ---------- 5. Учёт времени — 100 записей ---------- */
DECLARE @WteTarget INT = 100;
DECLARE @WteSeedCount INT = (SELECT COUNT(*) FROM dbo.WorkTimeEntries WHERE Comment LIKE N'%' + @SeedTag + N'%');

IF @WteSeedCount < @WteTarget
BEGIN
    DECLARE @tOffice INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Офис');
    DECLARE @tRemote INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Удалённо');
    DECLARE @tOver INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Переработка');
    DECLARE @tTrip INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Командировка');
    DECLARE @NeedWte INT = @WteTarget - @WteSeedCount;

    ;WITH tally AS (
        SELECT TOP (@NeedWte) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_columns a CROSS JOIN sys.all_columns b
    ),
    emp AS (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn, COUNT(*) OVER () AS cnt
        FROM dbo.Employees WHERE IsDeleted = 0 AND IsActive = 1
    ),
    prj AS (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn, COUNT(*) OVER () AS cnt
        FROM dbo.Projects WHERE IsDeleted = 0
    )
    INSERT INTO dbo.WorkTimeEntries (EmployeeId, ProjectId, WorkDate, StartTime, EndTime, BreakMinutes, TypeId, Comment, CreatedByUserId)
    SELECT
        e.Id, p.Id,
        DATEADD(DAY, -(t.n % 90), CAST(GETDATE() AS DATE)),
        CAST(DATEADD(MINUTE, 540, 0) AS TIME(0)),
        CAST(DATEADD(MINUTE, 1080 + (t.n % 3) * 15, 0) AS TIME(0)),
        30 + (t.n % 4) * 15,
        CASE (t.n % 4) WHEN 0 THEN @tOffice WHEN 1 THEN @tRemote WHEN 2 THEN @tOver ELSE @tTrip END,
        CASE (t.n % 12)
            WHEN 0 THEN N'Согласование медиаплана с клиентом'
            WHEN 1 THEN N'Загрузка креативов в рекламный кабинет'
            WHEN 2 THEN N'Еженедельный отчёт по KPI'
            WHEN 3 THEN N'Правки по баннерам после фидбэка'
            WHEN 4 THEN N'Брифинг с аккаунт-командой'
            WHEN 5 THEN N'Настройка UTM и целей в Метрике'
            WHEN 6 THEN N'Монтаж ролика, версия для ТВ'
            WHEN 7 THEN N'Сверка часов с подрядчиком'
            WHEN 8 THEN N'Подготовка презентации для клиента'
            WHEN 9 THEN N'Тестирование гипотез в таргете'
            WHEN 10 THEN N'Вёрстка лендинга, согласование ТЗ'
            ELSE N'Актуализация контент-плана SMM'
        END + N' ' + @SeedTag,
        @AdminUserId
    FROM tally t
    INNER JOIN emp e ON e.rn = ((t.n - 1) % e.cnt) + 1
    INNER JOIN prj p ON p.rn = ((t.n * 7) % p.cnt) + 1;
END

/* ---------- 6. Журнал аудита — 100 записей ---------- */
DECLARE @AuditSeed INT = (SELECT COUNT(*) FROM dbo.AuditLog WHERE NewValue LIKE N'%' + @SeedTag + N'%');
IF @AuditSeed < 100
BEGIN
    DECLARE @NeedAudit INT = 100 - @AuditSeed;

    ;WITH tally AS (
        SELECT TOP (@NeedAudit) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_columns a CROSS JOIN sys.all_columns b
    )
    INSERT INTO dbo.AuditLog (UserId, TableName, OperationType, RecordId, OldValue, NewValue, [Timestamp], IPAddress)
    SELECT
        @AdminUserId,
        CASE (t.n % 5) WHEN 0 THEN N'WorkTimeEntries' WHEN 1 THEN N'Projects' WHEN 2 THEN N'Employees'
            WHEN 3 THEN N'Users' ELSE N'Departments' END,
        CASE (t.n % 3) WHEN 0 THEN N'INSERT' WHEN 1 THEN N'UPDATE' ELSE N'DELETE' END,
        CAST(t.n AS NVARCHAR(16)),
        CASE WHEN t.n % 3 = 0 THEN NULL
             ELSE N'Плановые часы: ' + CAST(40 + (t.n % 20) AS NVARCHAR(8)) END,
        CASE (t.n % 6)
            WHEN 0 THEN N'Добавлена запись учёта времени'
            WHEN 1 THEN N'Изменён статус проекта'
            WHEN 2 THEN N'Обновлены контакты сотрудника'
            WHEN 3 THEN N'Скорректирован период проекта'
            WHEN 4 THEN N'Изменена роль пользователя'
            ELSE N'Обновлено описание подразделения'
        END + N' ' + @SeedTag,
        DATEADD(HOUR, -t.n * 3, SYSUTCDATETIME()),
        N'127.0.0.1'
    FROM tally t;
END

/* ---------- 7. Попытки входа — 50 записей ---------- */
DECLARE @LoginSeed INT = (SELECT COUNT(*) FROM dbo.LoginAttempts WHERE [Message] LIKE N'%' + @SeedTag + N'%');
IF @LoginSeed < 50
BEGIN
    DECLARE @NeedLogin INT = 50 - @LoginSeed;

    ;WITH tally AS (
        SELECT TOP (@NeedLogin) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_columns a CROSS JOIN sys.all_columns b
    )
    INSERT INTO dbo.LoginAttempts (LoginName, AttemptTime, IsSuccess, IPAddress, [Message])
    SELECT
        CASE (n % 4) WHEN 0 THEN N'admin' WHEN 1 THEN N'manager' WHEN 2 THEN N'employee' ELSE N'guest' END,
        DATEADD(MINUTE, -n * 47, SYSUTCDATETIME()),
        CASE WHEN n % 7 = 0 THEN 0 ELSE 1 END,
        N'127.0.0.1',
        CASE WHEN n % 7 = 0 THEN N'Неверный пароль' ELSE N'Успешная аутентификация' END + N' ' + @SeedTag
    FROM tally;
END

/* ---------- 8. Резервные копии — 8 (метаданные) ---------- */
IF (SELECT COUNT(*) FROM dbo.Backups WHERE Comment LIKE N'%' + @SeedTag + N'%') < 8
BEGIN
    ;WITH tally AS (SELECT TOP (8) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_columns)
    INSERT INTO dbo.Backups (FileName, FilePath, SizeBytes, CreatedByUserId, Comment, IsAuto)
    SELECT
        N'ClickMediaTimeDB_' + CONVERT(NVARCHAR(8), DATEADD(DAY, -n, GETDATE()), 112)
            + CASE WHEN n % 3 = 0 THEN N'_auto' ELSE N'_manual' END + N'.bak',
        N'D:\SQLBackup\ClickMedia\ClickMediaTimeDB_' + CONVERT(NVARCHAR(8), DATEADD(DAY, -n, GETDATE()), 112)
            + CASE WHEN n % 3 = 0 THEN N'_auto' ELSE N'_manual' END + N'.bak',
        45000000 + n * 2345678,
        @AdminUserId,
        CASE WHEN n % 3 = 0 THEN N'Автоматическая копия при закрытии' ELSE N'Ручная копия администратора' END
            + N', неделя ' + CAST(n AS NVARCHAR(2)) + N' ' + @SeedTag,
        CASE WHEN n % 3 = 0 THEN 1 ELSE 0 END
    FROM tally t
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Backups b
        WHERE b.FileName LIKE N'ClickMediaTimeDB_' + CONVERT(NVARCHAR(8), DATEADD(DAY, -t.n, GETDATE()), 112) + N'%';
END

/* ---------- 9. Журнал ошибок — 15 ---------- */
IF (SELECT COUNT(*) FROM dbo.ErrorLog WHERE Source LIKE N'%' + @SeedTag + N'%') < 15
BEGIN
    ;WITH tally AS (SELECT TOP (15) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_columns)
    INSERT INTO dbo.ErrorLog (UserId, Source, Message, StackTrace, CreatedAt)
    SELECT @AdminUserId, N'ClickMediaWorkTime.UI' + @SeedTag,
        CASE (n % 5)
            WHEN 0 THEN N'Таймаут подключения к SQL Server'
            WHEN 1 THEN N'Не удалось сохранить файл отчёта'
            WHEN 2 THEN N'Операция отменена пользователем'
            WHEN 3 THEN N'Ошибка валидации: время окончания раньше начала'
            ELSE N'Сбой при формировании PDF-отчёта'
        END,
        N'   at ClickMediaWorkTime.Forms.ReportsForm.LoadAll()',
        DATEADD(DAY, -n, SYSUTCDATETIME())
    FROM tally;
END

/* ---------- 10. Сессии — 10 ---------- */
IF @AdminUserId IS NOT NULL AND (SELECT COUNT(*) FROM dbo.Sessions WHERE IPAddress = N'127.0.0.1-demo') < 10
BEGIN
    ;WITH tally AS (SELECT TOP (10) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_columns)
    INSERT INTO dbo.Sessions (UserId, LoginTime, LastActivity, IPAddress, IsRevoked)
    SELECT @AdminUserId,
        DATEADD(HOUR, -n * 5, SYSUTCDATETIME()),
        DATEADD(HOUR, -n * 5 + 1, SYSUTCDATETIME()),
        N'127.0.0.1-demo',
        CASE WHEN n % 9 = 0 THEN 1 ELSE 0 END
    FROM tally;
END

MERGE dbo.SystemSettings AS t
USING (SELECT N'DemoSeed_v2_Applied' AS ParamName, N'1' AS ParamValue,
       N'Демо-наполнение 02_seed_demo_data.sql' AS [Description]) AS s
ON t.ParamName = s.ParamName
WHEN MATCHED THEN UPDATE SET ParamValue = s.ParamValue, LastModified = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (ParamName, ParamValue, [Description])
    VALUES (s.ParamName, s.ParamValue, s.[Description]);

COMMIT TRANSACTION;

DECLARE @CntDept INT, @CntPos INT, @CntEmp INT, @CntPrj INT, @CntWte INT, @CntWteSeed INT, @CntAudit INT, @CntLogin INT;

SELECT @CntDept = COUNT(*) FROM dbo.Departments WHERE IsDeleted = 0;
SELECT @CntPos  = COUNT(*) FROM dbo.Positions WHERE IsDeleted = 0;
SELECT @CntEmp  = COUNT(*) FROM dbo.Employees WHERE IsDeleted = 0;
SELECT @CntPrj  = COUNT(*) FROM dbo.Projects WHERE IsDeleted = 0;
SELECT @CntWte  = COUNT(*) FROM dbo.WorkTimeEntries;
SELECT @CntWteSeed = COUNT(*) FROM dbo.WorkTimeEntries WHERE Comment LIKE N'%[seed:v2]%';
SELECT @CntAudit = COUNT(*) FROM dbo.AuditLog;
SELECT @CntLogin = COUNT(*) FROM dbo.LoginAttempts;

PRINT N'';
PRINT N'=== Демо-данные [seed:v2] применены ===';
PRINT N'Подразделения : ' + CAST(@CntDept AS NVARCHAR(10));
PRINT N'Должности     : ' + CAST(@CntPos AS NVARCHAR(10));
PRINT N'Сотрудники    : ' + CAST(@CntEmp AS NVARCHAR(10));
PRINT N'Проекты       : ' + CAST(@CntPrj AS NVARCHAR(10));
PRINT N'Учёт времени  : ' + CAST(@CntWte AS NVARCHAR(10)) + N' (seed: ' + CAST(@CntWteSeed AS NVARCHAR(10)) + N')';
PRINT N'Аудит         : ' + CAST(@CntAudit AS NVARCHAR(10));
PRINT N'Попытки входа : ' + CAST(@CntLogin AS NVARCHAR(10));
GO
