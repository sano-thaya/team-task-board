# Team Task Board

Team Task Board is a full-stack task collaboration application built for the Full Stack Developer Intern technical assignment.

## Stack

- Frontend: Next.js, React, TypeScript, and Tailwind CSS
- Backend: ASP.NET Core Web API targeting .NET 10
- Database: MySQL 8 with Entity Framework Core and Pomelo
- Authentication: Firebase Authentication with email/password
- Token verification: Firebase Admin SDK integrated with ASP.NET Core authentication middleware
- Comments and live updates: Cloud Firestore and `onSnapshot`

## Current functionality

The repository currently includes:

- Firebase registration, login, logout, session restoration, and protected routes.
- Google Sign-In through Firebase Authentication, with the same backend user synchronization flow.
- Firebase-to-MySQL user synchronization with a default `Member` role.
- Project creation, listing, details, owner-only editing, member management, and deletion.
- Task creation, editing, deletion, assignment, search, priority filtering, and due-date validation.
- A responsive To Do / In Progress / Done board, status filtering, pagination metadata, and compatibility task routes.
- Task comments stored in Firestore with live refresh through a Firestore event marker.
- Top-level comment replies stored in the same Firestore task collection.
- Owner-only overdue task reporting grouped by assigned user.
- Responsive task, comment, project, report, and dashboard interfaces.

## Architecture and design decisions

The backend follows a controller/service/DTO structure. Controllers receive HTTP requests and map results to status codes; database access is in services using Entity Framework Core. Firebase authentication is registered as an ASP.NET Core authentication scheme, so protected controllers use `[Authorize]` and retrieve the Firebase UID from claims.

Entity Framework Core was chosen instead of Dapper because the application has related `User`, `Project`, `ProjectMember`, and `TaskItem` entities, ownership relationships, cascade rules, migrations, and test coverage using the EF Core in-memory provider. The existing migrations are the source of truth for the MySQL schema.

MySQL stores users, projects, project membership, and tasks. Firestore stores task comments because comments need a simple per-task collection and live subscriptions. The browser never writes comment documents directly; it calls the authenticated API. The API derives comment authors from the verified token and checks project/task membership before reading or creating comments.

## Project structure

```text
frontend/       Next.js application and Firebase web client
backend/        ASP.NET Core API, services, models, and EF migrations
backend.Tests/  xUnit service tests using EF Core in-memory data
database/       Reserved database documentation directory
docs/           Assignment/project documentation directory
```

## Configuration

### Frontend

Copy `frontend/.env.example` to `frontend/.env.local` and set the Firebase Web App values from Firebase Console:

```text
NEXT_PUBLIC_FIREBASE_API_KEY=
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=
NEXT_PUBLIC_FIREBASE_PROJECT_ID=
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=
NEXT_PUBLIC_FIREBASE_APP_ID=
NEXT_PUBLIC_API_URL=http://localhost:5159
```

### Backend

The API requires:

- `ConnectionStrings:DefaultConnection` pointing to the MySQL database.
- `Firebase:ProjectId` or `FIREBASE_PROJECT_ID`.
- Google Application Default Credentials for Firebase Admin and Firestore server access.

### Google Sign-In setup

In Firebase Console, open **Authentication → Sign-in method**, enable the **Google** provider, choose a project support email, and save. Add the local frontend origin (`http://localhost:3000`) under **Authentication → Settings → Authorized domains** if it is not already present. No Google client secret is placed in the frontend or repository; Firebase handles the provider flow. The frontend uses `signInWithPopup`, obtains the Firebase ID token, and calls `/api/auth/sync`. The backend uses the token’s verified UID and email, so repeated Google sign-ins update the existing `Users` row rather than creating duplicates.

Keep service-account credentials outside the repository. The `.gitignore` excludes environment files, secret directories, certificate files, and private keys. No Firebase service-account JSON is used by the frontend.

## Running locally

From `backend`:

```powershell
dotnet restore
dotnet run --launch-profile http
```

From `frontend`:

```powershell
npm install
npm run dev
```

The local frontend runs at `http://localhost:3000` and the API at `http://localhost:5159`.

Apply the existing EF migrations after configuring MySQL and Firebase settings:

```powershell
dotnet ef database update --project backend --startup-project backend
```

The current task migration is `20261121082315_AddTasks` and includes the `Tasks` table, foreign keys, and indexes for project, assignee, status, and priority.

The standalone SQL equivalents are [database/schema.sql](database/schema.sql) and [database/seed.sql](database/seed.sql). Use them for a fresh development database only; use EF migrations for an existing application database:

```powershell
mysql -u <user> -p < database/schema.sql
mysql -u <user> -p < database/seed.sql
```

## API endpoints currently implemented

All endpoints below that are marked protected require a Firebase ID token in the header:

```text
GET    /api/auth/verify                         token verification
POST   /api/auth/sync                           synchronize the authenticated profile
GET    /api/auth/me                             current authenticated profile

GET    /api/projects                            list available projects
GET    /api/projects/{id}                       project details
POST   /api/projects                            create a project
PUT    /api/projects/{id}                       owner-only project update
DELETE /api/projects/{id}                       owner-only project deletion

GET    /api/projects/{id}/tasks                 list tasks; supports search, priority, status, page, and pageSize
GET    /api/projects/{id}/tasks/{taskId}        task details
POST   /api/projects/{id}/tasks                 create a task
PUT    /api/projects/{id}/tasks/{taskId}        update a task
DELETE /api/projects/{id}/tasks/{taskId}        delete a task

GET    /api/tasks/{taskId}                      assignment-compatible task lookup
PUT    /api/tasks/{taskId}                      assignment-compatible task update
DELETE /api/tasks/{taskId}                      assignment-compatible task deletion

GET    /api/projects/{id}/tasks/{taskId}/comments       list comments
POST   /api/projects/{id}/tasks/{taskId}/comments       create comment or reply
GET    /api/projects/{id}/reports/overdue-tasks         owner-only overdue report
```

Comment replies use the same POST endpoint with:

```json
{
  "text": "This is a reply",
  "parentCommentId": "firestore-comment-document-id"
}
```

## Authorization and validation

- Firebase tokens are verified by the backend authentication handler.
- Missing, invalid, and expired tokens are rejected.
- Project and task services check ownership or project membership before returning data.
- Project updates and deletes are owner-only.
- Task assignees must be the project owner or a project member.
- Task titles and descriptions are length-validated.
- Priority and status values are validated.
- Due dates cannot be earlier than the current UTC date.
- Comment and reply authors are derived from the authenticated Firebase identity.
- A reply must reference an existing top-level comment on the same task.

## Testing commands

Frontend:

```powershell
cd frontend
npm run typecheck
npm run lint
npm run build
```

Backend:

```powershell
cd backend
dotnet build
dotnet test ..\backend.Tests\TeamTaskBoard.Api.Tests.csproj
```

The current automated backend suite contains 8 passing service tests covering project authorization, task CRUD/validation/filtering, and overdue report filtering/grouping/authorization/empty results. No Playwright or frontend component test suite is currently present in the repository.

For live comments and replies, use two authenticated browser sessions, open the same task, post a comment or reply in one session, and confirm the other session updates without a page refresh. Firebase credentials, a running API, MySQL, and Firestore access are required for this manual test.



## Known gaps and recommended next steps

1. Add drag-and-drop only if it can be introduced without compromising keyboard status changes.
2. Add frontend component/browser tests and Firebase integration tests.
3. Extend the SQL seed with a controlled overdue development record when manual report testing needs it.
4. Remove the interviewer-only section from any externally submitted copy of the assignment document if it is still present outside this repository.

## Security note

Never commit Firebase service-account credentials, private keys, `.env.local`, or other secrets. The Firebase web configuration is intended for the frontend, but backend credentials must remain in local/server configuration.
