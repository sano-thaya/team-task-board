Team Task Board Authentication

Architecture
------------
The browser uses Firebase Authentication for email/password registration and login. Firebase keeps the session in browser storage and `onAuthStateChanged` restores it after refresh. After registration or login, the frontend sends the Firebase ID token as a Bearer token to the ASP.NET Core API.

The API verifies tokens with Firebase Admin SDK. `POST /api/auth/sync` takes only the display name from the request body; UID and email always come from the verified token. It creates or updates the MySQL `Users` record and assigns `Member` on creation. `GET /api/auth/verify` remains available for direct token checks. Future protected controllers can use `[Authorize]`, `User` claims, and the `ManagerOnly` policy.

Configuration
-------------
Copy `frontend/.env.example` to `frontend/.env.local` and fill in the Firebase Web App values. Set `NEXT_PUBLIC_API_URL` when the API is not running at `http://localhost:5159`.

The backend requires a MySQL connection string named `ConnectionStrings:DefaultConnection` and Firebase Admin Application Default Credentials. Set `Firebase:ProjectId` (or `FIREBASE_PROJECT_ID`) to the Firebase project ID. Keep service-account credentials outside the repository; do not add JSON credentials to Git.

Running
-------
From `frontend`, run `npm install`, then `npm run dev`.
From `backend`, provide the connection string through .NET user secrets or environment configuration, then run `dotnet run --launch-profile http`.

Testing
-------
Run `npm run typecheck`, `npm run lint`, and `npm run build` in `frontend`; run `dotnet build` in `backend`. With MySQL, Firebase credentials, and the API running, use the registration/login pages and call `/api/auth/verify`, `/api/auth/sync`, and `/api/auth/me` with the Firebase Bearer token. Repeating sync for the same Firebase UID updates one row instead of creating a duplicate.

Project management
-------------------
Authenticated users can use `/projects` to create projects and view projects they own or belong to. Project owners can edit project name, description, and registered member emails, or delete the project. Members can view project details but cannot edit or delete them. The API derives ownership from the verified Firebase UID and validates every requested member email against the Users table.

Project API endpoints:

* `GET /api/projects` lists projects available to the authenticated user.
* `GET /api/projects/{id}` returns a project visible to the authenticated user.
* `POST /api/projects` creates a project owned by the authenticated user.
* `PUT /api/projects/{id}` updates an owned project.
* `DELETE /api/projects/{id}` deletes an owned project.

The project schema is in the `AddProjects` migration. Apply it with `dotnet ef database update --project backend --startup-project backend` after configuring the MySQL connection string and Firebase settings. To regenerate a migration after model changes, use `dotnet ef migrations add <MigrationName> --project backend --startup-project backend`.

Run the focused backend authorization tests with `dotnet test backend.Tests/TeamTaskBoard.Api.Tests.csproj`. These tests use EF Core in-memory data and cover project visibility and owner-only update/delete behavior.

Task management
---------------
Project owners and members can create, view, edit, search, filter, and delete tasks from the authenticated project task screen at `/projects/{id}/tasks`. Task assignment is validated against project participants on the API; task timestamps are stored in UTC, and deleting a project cascades to its tasks.

Task API endpoints:

* `GET /api/projects/{projectId}/tasks?search=login&priority=High` lists accessible project tasks.
* `GET /api/projects/{projectId}/tasks/{taskId}` returns task details.
* `POST /api/projects/{projectId}/tasks` creates a task with `title`, `description`, `assignedUserId`, `priority`, `status`, and optional `dueDateUtc`.
* `PUT /api/projects/{projectId}/tasks/{taskId}` updates those fields.
* `DELETE /api/projects/{projectId}/tasks/{taskId}` deletes a task.

Run `dotnet ef database update --project backend --startup-project backend` after configuring MySQL to apply the `AddTasks` migration. Run `dotnet test backend.Tests/TeamTaskBoard.Api.Tests.csproj` for task authorization and validation tests. The task UI uses the same Firebase ID token and `AuthGuard` as the existing project pages.

Task comments and live communication
------------------------------------
Comments are stored in Cloud Firestore under `projects/{projectId}/tasks/{taskId}/comments/{commentId}`. The ASP.NET Core API validates the Firebase identity and MySQL project/task membership before reading or creating a comment, and derives the author from the authenticated Firebase UID. The frontend subscribes to a non-sensitive `commentEvents/{projectId}_{taskId}` Firestore marker with `onSnapshot`; when the marker changes, it refreshes the authorized comment list through the API.

The Firestore rules are in `firestore.rules`. Comment documents are not directly readable or writable by browser clients; only authenticated clients may read the change marker, while the trusted backend Admin SDK bypasses rules for comment storage and event updates. Deploy the rules with the Firebase CLI after selecting the existing Firebase project:

`firebase deploy --only firestore:rules`

Comment API endpoints:

* `GET /api/projects/{projectId}/tasks/{taskId}/comments` lists comments chronologically.
* `POST /api/projects/{projectId}/tasks/{taskId}/comments` creates a comment with `{ "text": "..." }`.

The backend uses Application Default Credentials for the Firestore Admin client, the same credential approach already used by Firebase Admin token verification. `Firebase:ProjectId` or `FIREBASE_PROJECT_ID` must be configured; no service-account JSON is exposed to the frontend.

Overdue task reporting
----------------------
Project owners can view the overdue task report on the project details page. It includes tasks whose UTC due date is earlier than the current UTC time and whose status is not `Completed`, grouped by assigned team member. The API returns `404` for users who are not the project owner so report availability is not disclosed.

Report endpoint:

* `GET /api/projects/{projectId}/reports/overdue-tasks` returns grouped overdue tasks and requires a Firebase Bearer token.

The report uses the existing MySQL `Tasks` and `Users` tables and does not add a migration. The frontend renders it only for the owner, but the backend authorization check is authoritative. To test it manually, sign in as a project owner and open a project containing an open task after its due date. A member should not receive the report. Completed and future-dated tasks are excluded.
