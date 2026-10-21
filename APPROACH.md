# Approach

## Task 1: the defect and the filtering

I reproduced DEFECT-117 from the Network tab, found why changing a single filter did not send a request, fixed the cause rather than the symptom, and added a regression test that asserts the request that is sent (commit `f8337c1`). **Root cause: the fetch effect only depended on search, page and page size, so other filters never triggered a request, and it used stale filters (details in the README).**

For the filtering I put every filter in one place, `TicketQueries.ApplyFilters`, as `IQueryable` expressions that are ANDed together before the count and the paging, so the database does the filtering and `totalCount` matches the filtered set (commit `773501b`). Adding the next filter means adding one `if` block.

## Task 2: escalation and assignment (backend)

The brief says the challenge is where the rules live, so I started by writing each rule as a small pure class (`PriorityRule`, `SlaCalculator`, `AgentSelector`, `EscalationPolicy`) with unit tests, before touching persistence. Triage on create and escalation both compose those classes, so there is one SLA calculation and one agent-selection rule.

Rules that are numbers (windows, premium multiplier, floor, at-risk threshold) come from the `Sla` configuration section. Category behaviour comes from the existing `RequiresSpecialist` and `ForcesCriticalPriority` columns, never from category names.

SLA status stays derived. Because the at-risk rule needs the start of the current window, I added one timestamp column, `SlaStartedAtUtc`, instead of storing a status. The `slaStatus` filter is the SQL translation of the same rules, written as date predicates so filtering, counting and paging all happen in the database.

Escalation history is an add-only collection on the `Ticket` aggregate, saved in the same unit of work as the ticket change, with an index chosen for the one query it serves (one ticket, newest first).

## Task 2: escalation and assignment (frontend)

One `SlaIndicator` component draws every SLA state (text plus symbol, not colour alone) and is used by the list and the detail page. The `slaStatus` filter goes through the same request-building code as the DEFECT-117 fix, so it needed no special wiring. The escalation panel validates the reason (5-500 characters) before calling the API, shows server errors including a 409, and refreshes the ticket and history from the response. Validation rules live in `escalationRules.ts` so they sit in one place.

## Task 3: authentication

Credentials are a nullable `PasswordHash` on `Agents`: agents are the only users, so a separate table would only add a join. Hashing is PBKDF2-SHA256 from the runtime, behind an `IPasswordHasher` so the application never sees the algorithm. Login failures (unknown email, wrong password, inactive agent) are one 401 message and cost one hash verification each. The JWT is issued in the Presentation layer, and a fallback authorization policy closes every endpoint by default, so safety does not depend on remembering an attribute. `escalatedBy` now comes from the token. On the web side the session is held in `sessionStorage`, attached in `api/client.ts`, and any 401 clears it and returns the user to login, with the original page remembered.

## Task 4: Docker

A multi-stage Dockerfile restores from project files first so the restore layer is cached, then publishes and runs on the ASP.NET runtime image as the unprivileged user on port 8080. No configuration is baked in: Development settings are excluded from the build context and everything comes from environment variables in compose. On an empty volume the API migrates and seeds itself (opt-in flag); the README explains why a real deployment should migrate as a separate step instead.
