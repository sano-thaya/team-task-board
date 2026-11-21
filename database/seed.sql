-- Development-only seed data. These are placeholder identities, not Firebase credentials.
-- Run schema.sql first. INSERT IGNORE makes repeated runs safe for these fixed IDs.

USE team_task_board;

INSERT IGNORE INTO Users (Id, FirebaseUid, Email, DisplayName, Role, CreatedAtUtc) VALUES
    (1, 'seed-owner-uid', 'owner@example.test', 'Seed Owner', 'Manager', UTC_TIMESTAMP(6)),
    (2, 'seed-member-uid', 'member@example.test', 'Seed Member', 'Member', UTC_TIMESTAMP(6)),
    (3, 'seed-member-two-uid', 'member.two@example.test', 'Seed Member Two', 'Member', UTC_TIMESTAMP(6));

INSERT IGNORE INTO Projects (Id, Name, Description, OwnerId, CreatedAtUtc, UpdatedAtUtc) VALUES
    (1, 'Website Redesign', 'Development-only sample project.', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO ProjectMembers (ProjectId, UserId) VALUES
    (1, 2),
    (1, 3);

-- Priority: Low=0, Medium=1, High=2. Status: ToDo=0, InProgress=1, Completed=2.
INSERT IGNORE INTO Tasks (Id, ProjectId, Title, Description, AssignedUserId, Priority, Status, DueDateUtc, CreatedAtUtc, UpdatedAtUtc) VALUES
    (1, 1, 'Prepare project brief', 'Review the scope with the team.', 2, 1, 0, DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 7 DAY), UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)),
    (2, 1, 'Review landing page', 'Check the responsive layout.', 3, 2, 1, DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 3 DAY), UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
