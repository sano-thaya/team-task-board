-- Team Task Board MySQL schema.
-- This script matches the EF Core InitialCreate, AddProjects, and AddTasks migrations.
-- Run against a development database only; production changes should use EF migrations.

CREATE DATABASE IF NOT EXISTS team_task_board
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE team_task_board;

CREATE TABLE IF NOT EXISTS Users (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    FirebaseUid VARCHAR(128) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'Member',
    CreatedAtUtc DATETIME(6) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_FirebaseUid UNIQUE (FirebaseUid),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS Projects (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(150) NOT NULL,
    Description VARCHAR(2000) NOT NULL,
    OwnerId BIGINT NOT NULL,
    CreatedAtUtc DATETIME(6) NOT NULL,
    UpdatedAtUtc DATETIME(6) NOT NULL,
    CONSTRAINT PK_Projects PRIMARY KEY (Id),
    CONSTRAINT FK_Projects_Users_OwnerId FOREIGN KEY (OwnerId) REFERENCES Users (Id) ON DELETE RESTRICT,
    INDEX IX_Projects_OwnerId (OwnerId)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS ProjectMembers (
    ProjectId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    CONSTRAINT PK_ProjectMembers PRIMARY KEY (ProjectId, UserId),
    CONSTRAINT FK_ProjectMembers_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES Projects (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProjectMembers_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT,
    INDEX IX_ProjectMembers_UserId (UserId)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS Tasks (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    ProjectId BIGINT NOT NULL,
    Title VARCHAR(150) NOT NULL,
    Description VARCHAR(2000) NOT NULL,
    AssignedUserId BIGINT NOT NULL,
    Priority INT NOT NULL,
    Status INT NOT NULL,
    DueDateUtc DATETIME(6) NULL,
    CreatedAtUtc DATETIME(6) NOT NULL,
    UpdatedAtUtc DATETIME(6) NOT NULL,
    CONSTRAINT PK_Tasks PRIMARY KEY (Id),
    CONSTRAINT FK_Tasks_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES Projects (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Tasks_Users_AssignedUserId FOREIGN KEY (AssignedUserId) REFERENCES Users (Id) ON DELETE RESTRICT,
    INDEX IX_Tasks_ProjectId (ProjectId),
    INDEX IX_Tasks_AssignedUserId (AssignedUserId),
    INDEX IX_Tasks_Status (Status),
    INDEX IX_Tasks_Priority (Priority)
) CHARACTER SET utf8mb4;
