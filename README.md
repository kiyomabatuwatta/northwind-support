# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. All filters, including `slaStatus`, are applied in the database. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `POST /api/tickets/{id}/escalate` | Escalate one priority level (reason 5-500 chars). 409 for Critical, Resolved or Closed |
| `GET /api/tickets/{id}/escalations` | Escalation history, newest first |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |

---

## Submission notes

### Status

| Task | Status |
| --- | --- |
| 1. DEFECT-117 + server-side filtering | **Done** (commits `f8337c1`, `773501b`) |
| 2. Escalation & assignment, **backend** | **Done**: triage on create, escalation, history, `slaStatus` filter, migration, tests |
| 2. Escalation & assignment, **frontend** | **Done**: one SLA indicator (list and detail), SLA filter, escalation panel with client validation, history, triage result after create, tests |
| 3. Login and authentication | **Not finished** |
| 4. Dockerise the API | **Not finished** |

I ran out of time and chose to finish Task 2 properly (backend and frontend) rather than rush four tasks. Tasks 3 and 4 are not started in code; the plans are in APPROACH.md.

### Business rules as I understood them

- **Priority (BR-1):** the requested priority, defaulting to Medium; a category flagged `ForcesCriticalPriority` is always Critical.
- **SLA window (BR-2/3):** Critical 4h, High 8h, Medium 24h, Low 72h from creation; halved for Premium customers, never below 1h.
- **Assignment (BR-4/5):** active agents strictly below their open-ticket limit, specialised where the category has `RequiresSpecialist`; fewest open tickets wins, lowest id breaks ties. Nobody eligible means the ticket is created unassigned with a reason (HTTP 201, not an error).
- **Escalation (BR-7):** one priority level up, due date recomputed from the moment of escalation, current agent kept if still eligible otherwise reassigned, reason 5-500 characters, immutable history row. Critical, Resolved and Closed tickets give 409.
- **SLA status (BR-8):** always derived from dates and an injectable clock, never stored. At risk means 25% or less of the window remains.
- **Where the numbers live (BR-6):** the `Sla` configuration section (`appsettings*.json` or `Sla__...` environment variables). `SlaOptions` carries the same values as defaults, so a missing section still works.

### Design decisions

- **Small rule classes in the domain, composed by the use cases.** `PriorityRule`, `SlaCalculator`, `AgentSelector` and `EscalationPolicy` are pure and separately testable. Create goes through `TicketTriageService`; escalate goes through `EscalationPolicy`, which reuses the same `SlaCalculator` and `AgentSelector`, so no rule is written twice. I rejected one big "TriageService" that owns all the rules, because it could not be tested in pieces.
- **`Tickets.SlaStartedAtUtc` column.** The at-risk threshold needs the start of the *current* window, which moves on escalation. I rejected deriving it from the escalation table because that needs a join per row and cannot be filtered cheaply in SQL. It is a timestamp, not a stored status. Older tickets leave it null and fall back to `CreatedAtUtc`.
- **Escalation history lives on the `Ticket` aggregate** (`Ticket.Escalations`, add-only) rather than behind a separate repository, so the history row and the ticket change are saved in one unit of work.
- **`slaStatus` filter as date predicates in SQL.** It is one more block in `TicketQueries.ApplyFilters`. The at-risk comparison uses `DATEDIFF(second, ...)`, so it is accurate to the second, while `SlaEvaluator` compares ticks. The two must be kept in step.
- **Sla options are read by hand** in `Infrastructure.DependencyInjection` so the Infrastructure project needs no extra configuration package.
- Not added: MediatR, AutoMapper, a generic repository, an interface per class.

### The defect (DEFECT-117)

See commit `f8337c1`. **Root cause:** the effect that fetched tickets only depended on `search`, `page` and `pageSize`, so changing any other filter (like Status) never re-ran it, and its fetch function read an old copy of `filters` from an earlier render, which is why the next search carried the stale status. **Fix:** the request is now built from every filter field, so any change sends a request; only the search text is debounced (300 ms), and responses to outdated requests are ignored. **Verify:** `npm test` runs `useTicketList.test.ts`, which changes the status and asserts the request that is sent; it fails on the original code.

### Filtering

Filters are applied in `TicketQueries.ApplyFilters` on the `IQueryable`, before the count and the paging, so `totalCount` and `totalPages` describe the filtered set. To add the next filter, add one `if` block there (this is how `slaStatus` was added).

### Database

- New table `TicketEscalations` (immutable rows; foreign keys to ticket and agents all restrict deletes; check constraints on the priority columns) and new column `Tickets.SlaStartedAtUtc`, both in migration `AddTicketEscalationsAndSlaStart`.
- **Index added:** `IX_TicketEscalations_TicketId_EscalatedAtUtc` on (`TicketId`, `EscalatedAtUtc` DESC). The history read is "one ticket's rows, newest first", so the leading `TicketId` serves the lookup and the foreign key, and the descending column avoids a sort.

### How to run and test

```bash
npm install
npm run db:up          # SQL Server in Docker
npm run api            # applies migrations, seeds, serves https://localhost:7043 (Swagger at /swagger)
npm test               # dotnet test, then the web tests
```

There is no login yet (Task 3 not done), so there are **no development credentials**. `POST /api/tickets/{id}/escalate` takes `escalatedBy` from the request body; once authentication exists it should come from the token.

### Testing

- Domain rules: SLA window per priority and tier including the 1-hour floor, forced-Critical categories, agent selection (fewest open, specialists only, full and inactive agents skipped, deterministic ties, nobody eligible), escalation (one level up, due date from now, owner kept or reassigned, history values, Critical/Resolved/Closed rejected).
- SLA status at the boundaries: exactly on the due date, one tick after, exactly at 25%, one tick either side, resolved on time vs late.
- Escalation use case (mocked repositories) and the `slaStatus` filter against SQLite.
- Frontend (Vitest + Testing Library): a too-short reason blocks submission and the API is not called; a 409 is shown and the ticket is unchanged; the SLA indicator renders every state with different text and symbol; the escalate control is disabled for a Critical ticket.
- **Not covered:** the `AtRisk` and `WithinSla` SQL filters (they use SQL Server's `DATEDIFF`, which SQLite cannot run); the history list and triage result components have no tests.

### AI usage

- **Tool:** Claude (chat), used for the domain rules, the escalation entity and EF configuration, the use cases and endpoints, the SQL filter, the unit tests, the React components (SLA indicator, escalation panel, history, triage result) and the setup scripts.
- **What went wrong:** Claude wrote the first rules without seeing my repository and guessed names; I then sent my real code and it was adapted (for example to my existing `SlaEvaluator` and the `Sla` configuration names). Its change to the `RaiseTicketCommandHandler` constructor broke the existing `TicketCommandTestContext` (compiler error CS7036), which I fixed.
- **How I verified:** `dotnet build` and the full `dotnet test` run before every commit; the migration was inspected for unexpected drops before applying it.
- **Not yet verified by me:** the SQL generated for the `slaStatus` filter on SQL Server (it should be checked in the API console).

### Known limitations

- Tasks 3 and 4 are not done; the optional SLA summary report is not done.
- Every API endpoint is currently open (no authentication).
- `escalatedBy` is taken from the request body.
- Assignment and escalation read open-ticket counts without locking, so two simultaneous creates could both pick the same agent at their limit.
- The at-risk SQL filter works in whole seconds.
