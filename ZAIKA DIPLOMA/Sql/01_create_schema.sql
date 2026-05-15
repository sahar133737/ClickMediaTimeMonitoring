/* ИС мониторинга рабочего времени — ООО «Клик Медиа»
   БД в 3НФ: справочники вынесены, M:N прав через RolePermissions.
   Имя БД: ClickMediaTimeDB — должно совпадать с App.config и скриптами резервного копирования.
*/
IF DB_ID(N'ClickMediaTimeDB') IS NULL
BEGIN
    CREATE DATABASE ClickMediaTimeDB;
END
GO

USE ClickMediaTimeDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* --- 1. Роли (уровни доступа) --- */
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name            NVARCHAR(80)  NOT NULL,
        Code            NVARCHAR(32)  NOT NULL,
        AccessLevel     INT            NOT NULL,
        IsDeleted       BIT            NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT(0),
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_Roles_Code UNIQUE (Code)
    );
END
GO

/* --- 2. Права по ролям (ключи модулей/операций) --- */
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId          INT            NOT NULL,
        PermissionKey   NVARCHAR(120)  NOT NULL,
        IsAllowed       BIT            NOT NULL CONSTRAINT DF_RolePermissions_IsAllowed DEFAULT(1),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
        CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, PermissionKey)
    );
    CREATE INDEX IX_RolePermissions_RoleId ON dbo.RolePermissions(RoleId);
END
GO

/* --- 3. Подразделения --- */
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name            NVARCHAR(200)  NOT NULL,
        Description     NVARCHAR(500)  NULL,
        IsDeleted       BIT            NOT NULL CONSTRAINT DF_Departments_IsDeleted DEFAULT(0),
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_Departments_Name UNIQUE (Name)
    );
END
GO

/* --- 4. Должности --- */
IF OBJECT_ID(N'dbo.Positions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Positions
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name            NVARCHAR(200)  NOT NULL,
        IsDeleted       BIT            NOT NULL CONSTRAINT DF_Positions_IsDeleted DEFAULT(0),
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Positions_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_Positions_Name UNIQUE (Name)
    );
END
GO

/* --- 5. Сотрудники --- */
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DepartmentId    INT            NOT NULL,
        PositionId      INT            NOT NULL,
        FullName        NVARCHAR(200)  NOT NULL,
        PersonnelNumber NVARCHAR(40)   NOT NULL,
        Phone           NVARCHAR(32)   NULL,
        Email           NVARCHAR(200)  NULL,
        HireDate        DATE           NOT NULL,
        IsActive        BIT            NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT(1),
        IsDeleted       BIT            NOT NULL CONSTRAINT DF_Employees_IsDeleted DEFAULT(0),
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
        CONSTRAINT FK_Employees_Positions   FOREIGN KEY (PositionId)   REFERENCES dbo.Positions(Id),
        CONSTRAINT UQ_Employees_PersonnelNumber UNIQUE (PersonnelNumber)
    );
    CREATE INDEX IX_Employees_DepartmentId ON dbo.Employees(DepartmentId);
END
GO

/* --- 6. Пользователи ИС --- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LoginName           NVARCHAR(64)   NOT NULL,
        PasswordHash        NVARCHAR(400)  NOT NULL,
        FullName            NVARCHAR(200)  NOT NULL,
        RoleId              INT            NOT NULL,
        EmployeeId          INT            NULL,
        RegistrationDate    DATETIME2      NOT NULL CONSTRAINT DF_Users_Reg DEFAULT(SYSUTCDATETIME()),
        LastPasswordChange  DATETIME2      NULL,
        MustChangePassword  BIT            NOT NULL CONSTRAINT DF_Users_MustChg DEFAULT(0),
        IsActive            BIT            NOT NULL CONSTRAINT DF_Users_Active DEFAULT(1),
        IsDeleted           BIT            NOT NULL CONSTRAINT DF_Users_Deleted DEFAULT(0),
        CONSTRAINT FK_Users_Roles     FOREIGN KEY (RoleId)     REFERENCES dbo.Roles(Id),
        CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT UQ_Users_LoginName UNIQUE (LoginName)
    );
    CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
END
GO

/* --- 7. Статусы проектов (справочник) --- */
IF OBJECT_ID(N'dbo.ProjectStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectStatuses
    (
        Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name        NVARCHAR(120)  NOT NULL,
        CONSTRAINT UQ_ProjectStatuses_Name UNIQUE (Name)
    );
END
GO

/* --- 8. Проекты (учёт времени по проектам медиа-агентства) --- */
IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Projects
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name            NVARCHAR(250)  NOT NULL,
        ClientName      NVARCHAR(250)  NULL,
        StatusId        INT            NOT NULL,
        StartDate       DATE           NOT NULL,
        EndDate         DATE           NULL,
        PlannedHours    DECIMAL(9,2)   NOT NULL CONSTRAINT DF_Projects_Planned DEFAULT(0),
        IsDeleted       BIT            NOT NULL CONSTRAINT DF_Projects_IsDeleted DEFAULT(0),
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Projects_Statuses FOREIGN KEY (StatusId) REFERENCES dbo.ProjectStatuses(Id)
    );
    CREATE INDEX IX_Projects_StatusId ON dbo.Projects(StatusId);
END
GO

/* --- 9. Типы учёта времени --- */
IF OBJECT_ID(N'dbo.TimeEntryTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TimeEntryTypes
    (
        Id      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name    NVARCHAR(120) NOT NULL,
        CONSTRAINT UQ_TimeEntryTypes_Name UNIQUE (Name)
    );
END
GO

/* --- 10. Учёт рабочего времени (факт по сотруднику и проекту) --- */
IF OBJECT_ID(N'dbo.WorkTimeEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkTimeEntries
    (
        Id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId          INT            NOT NULL,
        ProjectId           INT            NOT NULL,
        WorkDate            DATE           NOT NULL,
        StartTime           TIME(0)        NOT NULL,
        EndTime             TIME(0)        NOT NULL,
        BreakMinutes        INT            NOT NULL CONSTRAINT DF_WTE_Break DEFAULT(0),
        TypeId              INT            NOT NULL,
        Comment             NVARCHAR(500)  NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_WTE_Created DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId     INT            NULL,
        CONSTRAINT FK_WTE_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT FK_WTE_Projects  FOREIGN KEY (ProjectId)  REFERENCES dbo.Projects(Id),
        CONSTRAINT FK_WTE_Types     FOREIGN KEY (TypeId)     REFERENCES dbo.TimeEntryTypes(Id),
        CONSTRAINT FK_WTE_Users     FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_WTE_Time CHECK (EndTime > StartTime),
        CONSTRAINT CK_WTE_Break CHECK (BreakMinutes >= 0 AND BreakMinutes < 24 * 60)
    );
    CREATE INDEX IX_WTE_Employee_Date ON dbo.WorkTimeEntries(EmployeeId, WorkDate);
    CREATE INDEX IX_WTE_Project_Date ON dbo.WorkTimeEntries(ProjectId, WorkDate);
END
GO

/* --- 11. Журнал аудита --- */
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId          INT            NULL,
        TableName       NVARCHAR(128)  NOT NULL,
        OperationType   NVARCHAR(32)   NOT NULL,
        RecordId        NVARCHAR(64)   NULL,
        OldValue        NVARCHAR(MAX)  NULL,
        NewValue        NVARCHAR(MAX)  NULL,
        [Timestamp]     DATETIME2      NOT NULL CONSTRAINT DF_Audit_Ts DEFAULT(SYSUTCDATETIME()),
        IPAddress       NVARCHAR(50)   NULL,
        CONSTRAINT FK_Audit_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_Audit_Timestamp ON dbo.AuditLog([Timestamp]);
END
GO

/* --- 12. Резервные копии --- */
IF OBJECT_ID(N'dbo.Backups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Backups
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FileName        NVARCHAR(255)  NOT NULL,
        FilePath        NVARCHAR(500)  NOT NULL,
        SizeBytes       BIGINT         NULL,
        CreationDate    DATETIME2      NOT NULL CONSTRAINT DF_Backups_Created DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId INT            NULL,
        Comment         NVARCHAR(500)  NULL,
        IsAuto          BIT            NOT NULL CONSTRAINT DF_Backups_Auto DEFAULT(0),
        CONSTRAINT FK_Backups_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
    );
END
GO

/* --- 13. Попытки входа --- */
IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginAttempts
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LoginName   NVARCHAR(64)   NOT NULL,
        AttemptTime DATETIME2      NOT NULL CONSTRAINT DF_LoginAtt_Ts DEFAULT(SYSUTCDATETIME()),
        IsSuccess   BIT            NOT NULL,
        IPAddress   NVARCHAR(50)   NULL,
        [Message]   NVARCHAR(400)  NULL
    );
END
GO

/* --- 14. Сессии --- */
IF OBJECT_ID(N'dbo.Sessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId          INT            NOT NULL,
        Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Sessions_Token DEFAULT(NEWID()),
        LoginTime       DATETIME2      NOT NULL CONSTRAINT DF_Sessions_Login DEFAULT(SYSUTCDATETIME()),
        LastActivity    DATETIME2      NOT NULL CONSTRAINT DF_Sessions_Activity DEFAULT(SYSUTCDATETIME()),
        IPAddress       NVARCHAR(50)   NULL,
        IsRevoked       BIT            NOT NULL CONSTRAINT DF_Sessions_Revoked DEFAULT(0),
        CONSTRAINT FK_Sessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

/* --- 15. Настройки приложения --- */
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ParamName       NVARCHAR(120)  NOT NULL,
        ParamValue      NVARCHAR(1000) NOT NULL,
        [Description]   NVARCHAR(500)  NULL,
        LastModified    DATETIME2      NOT NULL CONSTRAINT DF_SysSet_Mod DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_SystemSettings_Name UNIQUE (ParamName)
    );
END
GO

/* --- 16. Журнал ошибок клиента/сервера (опционально для диплома) --- */
IF OBJECT_ID(N'dbo.ErrorLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErrorLog
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId      INT            NULL,
        Source      NVARCHAR(200)  NOT NULL,
        Message     NVARCHAR(2000) NOT NULL,
        StackTrace  NVARCHAR(MAX)  NULL,
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_ErrLog_Created DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ErrorLog_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

/* --- Сиды справочников --- */
MERGE dbo.Roles AS t
USING (VALUES
    (N'Администратор', N'ADMIN', 100),
    (N'Руководитель', N'MANAGER', 60),
    (N'Сотрудник', N'EMPLOYEE', 20)
) AS s(Name, Code, AccessLevel)
ON t.Code = s.Code
WHEN NOT MATCHED THEN
    INSERT (Name, Code, AccessLevel) VALUES (s.Name, s.Code, s.AccessLevel);
GO

MERGE dbo.ProjectStatuses AS t
USING (VALUES (N'Планирование'), (N'В работе'), (N'На паузе'), (N'Завершён'))
AS s(Name)
ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (s.Name);
GO

MERGE dbo.TimeEntryTypes AS t
USING (VALUES (N'Офис'), (N'Удалённо'), (N'Переработка'), (N'Командировка'))
AS s(Name)
ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (s.Name);
GO

MERGE dbo.Departments AS t
USING (VALUES
    (N'Дирекция', N'Управление компанией'),
    (N'Продакшн', N'Производство контента'),
    (N'Media Buying', N'Закупка рекламы')
) AS s(Name, Description)
ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name, Description) VALUES (s.Name, s.Description);
GO

MERGE dbo.Positions AS t
USING (VALUES
    (N'Генеральный директор'),
    (N'Руководитель отдела'),
    (N'Медиапланер'),
    (N'Аккаунт-менеджер'),
    (N'Специалист')
) AS s(Name)
ON t.Name = s.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (s.Name);
GO

DECLARE @AdminRole INT = (SELECT Id FROM dbo.Roles WHERE Code = N'ADMIN');
DECLARE @MgrRole   INT = (SELECT Id FROM dbo.Roles WHERE Code = N'MANAGER');
DECLARE @EmpRole   INT = (SELECT Id FROM dbo.Roles WHERE Code = N'EMPLOYEE');

;WITH Keys AS (
    SELECT N'module.dashboard' AS k UNION ALL SELECT N'module.departments' UNION ALL SELECT N'module.positions'
    UNION ALL SELECT N'module.employees' UNION ALL SELECT N'module.projects' UNION ALL SELECT N'module.worktime'
    UNION ALL SELECT N'module.users' UNION ALL SELECT N'module.reports' UNION ALL SELECT N'module.backups'
    UNION ALL SELECT N'module.audit' UNION ALL SELECT N'module.admin'
)
INSERT INTO dbo.RolePermissions (RoleId, PermissionKey, IsAllowed)
SELECT @AdminRole, k, 1 FROM Keys k
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @AdminRole AND rp.PermissionKey = k);

INSERT INTO dbo.RolePermissions (RoleId, PermissionKey, IsAllowed)
SELECT @MgrRole, k, 1
FROM (VALUES
    (N'module.dashboard'), (N'module.departments'), (N'module.positions'), (N'module.employees'),
    (N'module.projects'), (N'module.worktime'), (N'module.reports')
) AS x(k)
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @MgrRole AND rp.PermissionKey = x.k);

INSERT INTO dbo.RolePermissions (RoleId, PermissionKey, IsAllowed)
SELECT @EmpRole, k, 1
FROM (VALUES (N'module.dashboard'), (N'module.worktime')) AS x(k)
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @EmpRole AND rp.PermissionKey = x.k);
GO

MERGE dbo.SystemSettings AS t
USING (VALUES (N'CompanyName', N'ООО «Клик Медиа»', N'Наименование организации для отчётов'))
AS s(ParamName, ParamValue, [Description])
ON t.ParamName = s.ParamName
WHEN NOT MATCHED THEN INSERT (ParamName, ParamValue, [Description]) VALUES (s.ParamName, s.ParamValue, s.[Description]);
GO

/* Демо-сотрудники и проекты (пользователи с паролями создаются из приложения при первом запуске) */
IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
    DECLARE @dProd INT = (SELECT Id FROM dbo.Departments WHERE Name = N'Продакшн');
    DECLARE @dBuy  INT = (SELECT Id FROM dbo.Departments WHERE Name = N'Media Buying');
    DECLARE @pHead INT = (SELECT Id FROM dbo.Positions WHERE Name = N'Руководитель отдела');
    DECLARE @pSpec INT = (SELECT Id FROM dbo.Positions WHERE Name = N'Специалист');
    DECLARE @pAcc  INT = (SELECT Id FROM dbo.Positions WHERE Name = N'Аккаунт-менеджер');

    INSERT INTO dbo.Employees (DepartmentId, PositionId, FullName, PersonnelNumber, Phone, Email, HireDate, IsActive)
    VALUES
        (@dProd, @pHead, N'Иванов Сергей Петрович', N'КМ-0001', N'+7 (999) 100-00-01', N'ivanov@clickmedia.demo', DATEFROMPARTS(2019, 3, 1), 1),
        (@dBuy,  @pAcc,  N'Петрова Анна Игоревна',   N'КМ-0002', N'+7 (999) 100-00-02', N'petrova@clickmedia.demo', DATEFROMPARTS(2020, 6, 15), 1),
        (@dProd, @pSpec, N'Сидоров Олег Викторович', N'КМ-0003', N'+7 (999) 100-00-03', N'sidorov@clickmedia.demo', DATEFROMPARTS(2021, 1, 10), 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Projects)
BEGIN
    DECLARE @stWork INT = (SELECT Id FROM dbo.ProjectStatuses WHERE Name = N'В работе');
    DECLARE @stPlan INT = (SELECT Id FROM dbo.ProjectStatuses WHERE Name = N'Планирование');

    INSERT INTO dbo.Projects (Name, ClientName, StatusId, StartDate, EndDate, PlannedHours)
    VALUES
        (N'Рекламная кампания «Весна 2026»', N'Ритейл Холдинг', @stWork, DATEFROMPARTS(2026, 2, 1), DATEFROMPARTS(2026, 5, 30), 480),
        (N'Брендирование соцсетей', N'ООО «Старт»', @stPlan, DATEFROMPARTS(2026, 4, 1), NULL, 120),
        (N'Performance-закупка Q2', N'Агрегатор услуг', @stWork, DATEFROMPARTS(2026, 4, 1), DATEFROMPARTS(2026, 6, 30), 600);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.WorkTimeEntries)
BEGIN
    DECLARE @e1 INT = (SELECT TOP 1 Id FROM dbo.Employees WHERE PersonnelNumber = N'КМ-0001');
    DECLARE @e2 INT = (SELECT TOP 1 Id FROM dbo.Employees WHERE PersonnelNumber = N'КМ-0002');
    DECLARE @e3 INT = (SELECT TOP 1 Id FROM dbo.Employees WHERE PersonnelNumber = N'КМ-0003');
    DECLARE @pr1 INT = (SELECT TOP 1 Id FROM dbo.Projects ORDER BY Id);
    DECLARE @pr2 INT = (SELECT Id FROM dbo.Projects ORDER BY Id OFFSET 1 ROW FETCH NEXT 1 ROW ONLY);
    DECLARE @tOffice INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Офис');
    DECLARE @tRemote INT = (SELECT Id FROM dbo.TimeEntryTypes WHERE Name = N'Удалённо');

    INSERT INTO dbo.WorkTimeEntries (EmployeeId, ProjectId, WorkDate, StartTime, EndTime, BreakMinutes, TypeId, Comment)
    VALUES
        (@e1, @pr1, CAST(GETDATE() AS date), '09:00', '18:00', 60, @tOffice, N'Планирование медиамикса'),
        (@e2, @pr1, CAST(GETDATE() AS date), '10:00', '19:00', 60, @tRemote, N'Согласование ставок'),
        (@e3, @pr2, DATEADD(DAY, -1, CAST(GETDATE() AS date)), '09:30', '18:30', 45, @tOffice, N'Креативные макеты');
END
GO
