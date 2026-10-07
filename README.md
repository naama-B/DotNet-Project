# Gym API — .NET Web API final project

A gym class-booking server built as a four-project ASP.NET Core solution. Members register,
browse scheduled classes, and book a spot; administrators manage the catalogue and the
schedule. The interesting part is what happens when two members reach for the **same last
spot at the same instant** — the system guarantees that only one of them gets it.

The server is the graded deliverable. A React client will be added later.

---

## The limited resource and how contention is handled

**The limited resource is a `ClassSession`** — one scheduled run of a class (e.g. "Spin,
Tuesday 18:00") with a fixed `Capacity`. Every confirmed booking consumes one of a small,
countable number of spots.

**Where the contention is:** `POST /api/bookings`. The naive flow — read the session, check
`BookedCount < Capacity`, insert a booking, write `BookedCount + 1` — is a race. Two requests
can both read "1 of 2 booked", both pass the check, and both save, leaving 3 bookings on a
2-seat class with nothing in the database showing that anything went wrong.

**How the system responds:** optimistic concurrency.

* `ClassSession` carries a `Guid Version` marked `[ConcurrencyCheck]`. `GymDbContext`
  refreshes it on every save (`SaveChangesAsync` override), and EF Core puts the *original*
  value into the `UPDATE … WHERE Id = @id AND Version = @original` clause.
* `BookingService.BookAsync` does the capacity check and the write (`BookedCount + 1` plus the
  new `Booking` row) against the same tracked entity, committed in **one** `SaveChangesAsync`.
* If another booking slipped in between our read and our save, the row's `Version` no longer
  matches, the `UPDATE` touches zero rows, and EF Core throws `DbUpdateConcurrencyException`.
  The service catches exactly that exception, logs it at **Warning**, and returns
  **HTTP 409 Conflict** with a "please try again" message. It never catches `Exception`
  broadly, and the check is never done outside the transaction.

When a session is genuinely full, the booking is placed on a **waitlist** (position returned
to the caller) rather than rejected.

The proof that this works is an automated test:
`Gym.Tests/Concurrency/OptimisticConcurrencyTests.cs` opens **two independent `DbContext`s**
against a real database, has both take the last spot, and asserts that the first save succeeds
and the second throws `DbUpdateConcurrencyException`.

---

## Architecture

```
Gym.sln
  Gym.Core      Entities, enums, DTOs, interfaces, Result type   (no external dependencies)
  Gym.Data      GymDbContext, EF configurations, repositories, migrations, seeder
  Gym.Service   Business logic, AutoMapper profiles, JWT + password hashing
  Gym.API       Controllers, middleware, Program.cs, nlog.config
  Gym.Tests     xUnit + Moq
```

Dependency direction: `Core` depends on nothing; `Data → Core`; `Service → Core`;
`API → Service, Core`, and `API → Data` only to register services in `Program.cs`.

Cross-cutting pieces:

* **Middleware** — `ExceptionHandlingMiddleware` (global, uniform JSON error body, logs at
  Error) then `CorrelationIdMiddleware` (per-request `X-Correlation-ID`, pushed into the log
  scope). Registered before authentication.
* **Logging** — the code uses `ILogger`; **NLog** is wired underneath (`nlog.config`). One log
  line per incoming request with its correlation id, Error for unhandled exceptions, Warning
  for every booking conflict. Passwords, tokens and registration bodies are never logged.
* **Auth** — JWT bearer, two roles (`Member`, `Admin`) with different permissions actually
  enforced on the server via `[Authorize(Roles = "Admin")]`.
* **Async** — every call is async from controller to `DbContext`, and `CancellationToken`
  flows the whole way.

---

## Running locally

### Prerequisites

* .NET SDK 9.0
* **PostgreSQL** — the connection string points at a Postgres database on `localhost:5432`.
  Pick one:

  **Option A — Docker (quickest):**

  ```bash
  docker compose up -d      # starts postgres:16 on localhost:5432 (db=gymdb user=gym pass=gym_local_dev)
  ```

  **Option B — a locally installed PostgreSQL (no Docker):**

  Install PostgreSQL (16 or newer) — on Windows: `winget install PostgreSQL.PostgreSQL.17`,
  which registers an auto-starting service on port 5432 with a `postgres` superuser. Then
  create the database and login this project expects (run from
  `C:\Program Files\PostgreSQL\17\bin`, or with `psql` on PATH):

  ```bash
  psql -U postgres -c "CREATE ROLE gym LOGIN PASSWORD 'gym_local_dev';"
  psql -U postgres -c "CREATE DATABASE gymdb OWNER gym;"
  ```

  Any other Postgres works too; just adjust the connection string below. The app creates its
  own tables (migrations run on startup) — you only need the empty `gymdb` database to exist.

### 1. Configure secrets (not committed)

The connection string and the JWT signing key are read from configuration and are **not** in
the repo. Set them with user-secrets on the API project:

```bash
cd Gym.API
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=gymdb;Username=gym;Password=gym_local_dev"
dotnet user-secrets set "Jwt:Key" "dev-only-signing-key-change-me-please-32+chars"
```

(`Jwt:Issuer`, `Jwt:Audience`, token lifetime and CORS origins are non-secret and live in
`appsettings.json`.)

### 2. Run

```bash
dotnet run --project Gym.API
```

On startup the app **applies migrations and seeds demo data automatically** — no manual SQL.
Swagger UI opens at `https://localhost:7056/swagger`.

### 3. Migrations (reference)

```bash
dotnet ef migrations add <Name> -p Gym.Data -s Gym.API
dotnet ef database update      -p Gym.Data -s Gym.API
```

`-p` is where the `DbContext` lives (`Gym.Data`), `-s` is the startup project (`Gym.API`).

### 4. Tests

```bash
dotnet test
```

The test project uses in-memory SQLite for the database-backed tests, so no running Postgres
is needed for `dotnet test`.

---

## Demo users

Seeded on first run. Password for every account is shown below.

| Role   | Email             | Password     | Can do                                                        |
|--------|-------------------|--------------|--------------------------------------------------------------|
| Admin  | `admin@gym.local` | `Admin#123`  | Everything, plus create/update class types, instructors, sessions, cancel sessions |
| Member | `dana@gym.local`  | `Member#123` | Browse classes, book, waitlist, cancel own bookings          |
| Member | `noa@gym.local`   | `Member#123` | same                                                         |
| Member | `yael@gym.local`  | `Member#123` | same                                                         |
| Member | `tamar@gym.local` | `Member#123` | same                                                         |

### Trying the booking race by hand

The seed creates a **Spin** session tomorrow at 18:00 with **capacity 3 and 2 already
booked** — exactly one spot left. Log in as two different members, and fire
`POST /api/bookings` with that session's id from both at the same time: one gets `201`, the
other gets `409 Conflict`.

---

## Key endpoints

| Method & path                     | Auth        | Purpose                              |
|-----------------------------------|-------------|--------------------------------------|
| `POST /api/auth/register`         | anonymous   | Register, returns JWT                |
| `POST /api/auth/login`            | anonymous   | Login, returns JWT                   |
| `GET  /api/classsessions`         | any member  | Paged/filterable session list (`fromUtc` limits it to upcoming) |
| `GET  /api/classsessions/reviews` | any member  | Every rated session with its title, instructor and reviews |
| `GET  /api/classsessions/{id}`    | any member  | One session                          |
| `POST /api/classsessions`         | Admin       | Create a session                     |
| `POST /api/classsessions/{id}/cancel` | Admin   | Cancel a session                     |
| `GET  /api/classsessions/{id}/waitlist` | Admin | The session's waiting list, in queue order |
| `GET  /api/classsessions/{id}/ratings` | any member | All satisfaction ratings + the average |
| `POST /api/classsessions/{id}/ratings` | attendee | Leave/update a 1–5 star rating for a past class |
| `POST /api/bookings`              | any member  | Book a spot (→ 409 on the race)      |
| `GET  /api/bookings/mine`         | any member  | My bookings                          |
| `POST /api/bookings/{id}/cancel`  | any member  | Cancel my booking                    |
| `GET/POST/PUT/DELETE /api/classtypes` | GET any / writes Admin | Class-type catalogue     |
| `GET/POST /api/instructors`       | GET any / POST Admin | Instructors                 |

Pagination is real: `?page=2&pageSize=10&search=spin&onlyAvailable=true` translates to
`Skip`/`Take` in the SQL query, not in memory.
