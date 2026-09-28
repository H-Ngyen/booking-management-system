# Service Booking Management System

A booking system for services. Customers book a time slot with a staff member.
Admins manage services, staff, work schedules, and all bookings.

Stack: ASP.NET Core (.NET 10) + EF Core + PostgreSQL for the API,
Next.js App Router + TypeScript for the web app.

## 1. Run the demo (one command)

```bash
docker compose up -d --build
```

That is all. Open your browser:

| What | Address |
|---|---|
| Web app (customer + admin) | http://localhost:3000 |
| API | http://localhost:5000/api/v1 |
| Swagger UI | http://localhost:5000/swagger |

On first start, the API runs migrations and seeds sample data (see section 3).
Stop the demo: `docker compose down`. To wipe demo data and seed fresh:
`docker compose down -v && docker compose up -d --build`.

## 2. Demo accounts

| Role | Username | Password | Use it for |
|---|---|---|---|
| Admin | `admin` | `admin123` | Manage services / staff / schedules / bookings |
| Customer | `customer1` | `customer123` | Book slots, view and cancel own bookings |
| Customer | `customer2` | `customer123` | Test access rules (TC4) |

> After login: admins land on `/admin/bookings`, customers land on `/services`.

## 3. Sample data (auto seed)

The seed meets the required minimum: 1 admin, 2 customers, 2 staff,
5 services (one is locked, for testing), work schedules for the next 7 days
(2 shifts per day per staff), and 10 bookings in many states
(Pending / Confirmed / Completed / Cancelled). Seed dates are relative to
today, so the demo never goes stale. Seeding is idempotent — running it again
creates nothing twice (it stops if users already exist).

## 4. API (base path `/api/v1`)

| Group | Endpoint |
|---|---|
| Auth | `POST /api/v1/auth/login`, `GET /api/v1/auth/me` |
| Services | `GET /api/v1/services` (search + paging), `POST /api/v1/services`, `PUT /api/v1/services/{id}` (lock / reopen with `isActive`) |
| Staff / Schedule | `GET /api/v1/staffs`, `POST /api/v1/staffs`, `PUT /api/v1/staffs/{id}`, `GET /api/v1/staffs/{id}/schedules`, `POST /api/v1/staffs/{id}/schedules`, `DELETE /api/v1/staffs/{staffId}/schedules/{scheduleId}` |
| Bookings | `GET /api/v1/bookings/my-bookings`, `GET /api/v1/bookings` (admin, filter by date + status + paging), `GET /api/v1/bookings/available-slots`, `POST /api/v1/bookings`, `PATCH /api/v1/bookings/{id}/status`, `POST /api/v1/bookings/{id}/cancel` |

Auth uses JWT Bearer. Errors use one shape: `{ message, code }`
(`VALIDATION_ERROR` 400, `BOOKING_CONFLICT` 409, `FORBIDDEN` 403, …).
Full details are in Swagger UI while the demo runs.

Main business rules (all checked in the **backend**, independent of the frontend):
- `EndTime = StartTime + DurationMinutes` is computed by the backend.
  The client only picks the start time.
- No bookings in the past, outside work hours, or for locked services / staff.
- No double booking: `NewStart < ExistingEnd && NewEnd > ExistingStart`
  (cancelled bookings are ignored) → `409 Conflict`. Check + insert run in one
  transaction with a `FOR UPDATE` row lock, so two requests for the same slot
  cannot both win.
- Customers see and cancel only their own bookings, cannot confirm or complete
  them. Completed or started bookings cannot be cancelled. Cancelling needs a
  reason.

## 5. Screens

| Path | Content |
|---|---|
| `/login` | Login + error display |
| `/services` | Service list + book button |
| `/booking` | Pick service, staff, date, time slot, note (shows expected end time) |
| `/my-bookings` | View, filter (date + status), cancel own bookings (reason required) |
| `/admin/services` | Add / edit / lock / reopen services, search + paging |
| `/admin/staffs` | Add / edit / lock / reopen staff (extra screen, needed for “admin adds staff”) |
| `/admin/schedules` | Create schedules per staff (blocks overlap, `start < end`), delete shifts |
| `/admin/bookings` | Confirm / complete / cancel all bookings, filter by date + status + paging |

## 6. Tests

Backend integration tests (xUnit + Testcontainers Postgres, each run uses its
own clean database). Test code lives in `apps/api/tests`, grouped by area
(`Auth`, `Bookings`, `Services`, `Staffs`, `Jobs`, `Data`, `Realtime`,
`Smoke`, `Validation`, plus shared `Support` helpers):

```bash
dotnet test apps/api/tests/api.Tests.csproj
```

Current result: **109/109 passed** (overlap guard, access rules, validation,
error shape, overdue job, realtime hub).

Manual walkthrough of the 6 required cases on the docker stack
(API `http://localhost:5000/api/v1`):

| TC | How to try | Result |
|---|---|---|
| TC1: no past booking | `POST /bookings` with `startTime` in 2020 | `400 BOOKING_IN_PAST` ✅ |
| TC2: no out-of-hours booking | `POST /bookings` at 05:00 (shifts start 08:00) | `400 OUTSIDE_WORKING_HOURS` ✅ |
| TC3: no double booking | Create 08:00 (30 min) → create 08:15 again | `201` then `409 BOOKING_CONFLICT` ✅ |
| TC4: cannot see other bookings | customer2 calls `my-bookings` + cancels customer1's booking | Not listed; `403 FORBIDDEN` ✅ |
| TC5: customer cannot complete | customer calls `PATCH /bookings/{id}/status` → Completed | `403 FORBIDDEN` ✅ |
| TC6: cannot cancel completed | Cancel a Completed booking | `400 CANNOT_CANCEL_STARTED` ✅ |

## 7. Run locally without docker (optional)

You need PostgreSQL on `localhost:5432` (user / password / db =
`postgres` / `postgres` / `bookingdb`, or edit
`apps/api/src/appsettings.Development.json`):

```bash
# Terminal 1 — API (auto migrate + seed in Development)
cd apps/api && pnpm dev              # http://localhost:5000
# (or: dotnet watch run --project ./src/api.csproj --launch-profile http)

# Terminal 2 — Web
cd apps/web && pnpm install && pnpm dev   # http://localhost:3000
```

The web app calls the API through `NEXT_PUBLIC_API_URL`
(default `http://localhost:5000/api`).

## 8. Done / not done

**Done (all required parts):** login + 2 roles; service management
(add / edit / lock / reopen, search, paging); staff management; work schedules
(blocks overlap, blocks deleting shifts that have bookings); booking with
backend-computed end time; double-booking guard (409 + row lock against
races); customer view / cancel own bookings (date + status filter); admin
confirm / complete / cancel all bookings; sample seed; Swagger; Hangfire job
that auto-cancels overdue bookings (runs daily at 00:00); SignalR realtime
updates; EF Core migrations; basic responsive design; integration tests.

**Bonus already included:** integration tests (109 tests), full-stack Docker
Compose, SignalR realtime, Hangfire overdue job, safe concurrent booking
(`FOR UPDATE` + transaction).

**Not done:** Postman collection (Swagger covers it), sign-up / forgot
password (not required), visual calendar view (the current list is clear and
usable, as required).

## 9. Repo layout

```
booking_management_system/
├── docker-compose.yml          # db + api + web — one command demo
├── apps/api/                   # ASP.NET Core (.NET 10), EF Core, Postgres
│   ├── package.json            # turbo scripts (dev / build)
│   ├── Dockerfile
│   ├── src/                    # app code
│   │   ├── Controllers/        # thin: forward to Services
│   │   ├── Services/           # business logic + access rules
│   │   ├── Repositories/       # queries (paging in the database)
│   │   ├── Data/Migrations/    # EF Core migrations
│   │   ├── Data/DbSeeder.cs    # sample data (dev only)
│   │   ├── Jobs/               # Hangfire: cancel overdue bookings
│   │   └── Hubs/               # SignalR realtime
│   └── tests/                  # all tests, grouped by area
│       ├── Support/            # shared base + DB fixture
│       ├── Auth/ Bookings/ Services/ Staffs/
│       ├── Jobs/ Data/ Realtime/ Smoke/ Validation/
│       └── api.Tests.csproj
├── apps/web/                   # Next.js App Router + TypeScript
│   └── src/app/                # (customer)/, (admin)/, login/
└── README.md
```
