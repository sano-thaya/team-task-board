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
- Firebase-to-MySQL user synchronization with a default `Member` role.
- Project creation, listing, details, owner-only editing, member management, and deletion.
- Task creation, editing, deletion, assignment, search, priority filtering, and due-date validation.
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

GET    /api/projects/{id}/tasks                 list tasks; supports search and priority
GET    /api/projects/{id}/tasks/{taskId}        task details
POST   /api/projects/{id}/tasks                 create a task
PUT    /api/projects/{id}/tasks/{taskId}        update a task
DELETE /api/projects/{id}/tasks/{taskId}        delete a task

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

## Assignment compliance review

This review compares the current repository with the supplied assignment brief. It describes the actual implementation as inspected; it is not a claim that every optional deliverable is complete.

| Area | Score | Review |
| --- | ---: | --- |
| Frontend | 19/25 | Authentication, protected routes, forms, validation, search/filtering, comments, loading/error states, and responsive UI are implemented. The brief requests a three-column board; the current UI is a responsive task list with selected-task details instead. |
| Backend | 20/25 | Controllers delegate to services, DTOs are used, Firebase authentication and ownership checks are present, and correct common status codes are returned. The exact brief routes `/api/tasks/{id}` are not used; task update/delete remain nested under projects. Status and pagination query parameters are not implemented. |
| Database | 9/15 | EF migrations define primary keys, foreign keys, required fields, relationships, and task indexes. The overdue report currently uses EF/LINQ plus application-side grouping rather than one explicit SQL `JOIN`/`GROUP BY` query. Required `schema.sql` and `seed.sql` files are not present. |
| Firebase | 9/10 | Firebase Auth, backend token verification, Firestore comments, live updates, protected rules, and secret handling are implemented. Automated Firestore integration testing is not present. |
| Code quality and Git | 12/15 | Layered backend design, dependency injection, service tests, and feature branches are present in the inspected checkout. A complete submission-history/PR review was not performed here, and some frontend files are compact rather than conventionally formatted. |
| README and docs | 5/5 | This README documents setup, environment variables, architecture, endpoints, testing, design decisions, and known gaps. The existing `README.txt` remains as legacy documentation. |
| Tests and bonus | 3/5 | Backend tests are meaningful and passing. There are no frontend component/Playwright tests, and the optional SQL deliverables are missing. |
| **Total** | **77/100** | **Strong core implementation, with the documented assignment-specific gaps above.** |

## Known gaps and recommended next steps

1. Add `database/schema.sql` and `database/seed.sql` with safe sample values.
2. Add a dedicated SQL overdue report query using `JOIN` and `GROUP BY`, or document why the EF query is the chosen equivalent.
3. Add `status` and `page`/total-count support to the task list endpoint.
4. Decide whether to add compatibility routes matching the brief’s `/api/tasks/{id}` update/delete endpoints.
5. Render tasks in explicit To Do, In Progress, and Done columns if strict board compliance is required.
6. Extend `backend/TeamTaskBoard.Api.http` with comments, replies, and overdue report requests.
7. Add frontend component or browser tests and Firestore integration tests.
8. Remove the interviewer-only section from any externally submitted copy of the assignment document if it is still present outside this repository.

## Security note

Never commit Firebase service-account credentials, private keys, `.env.local`, or other secrets. The Firebase web configuration is intended for the frontend, but backend credentials must remain in local/server configuration.
