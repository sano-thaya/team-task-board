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
