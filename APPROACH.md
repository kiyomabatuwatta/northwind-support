# Approach

## Task 1: the defect and the filtering

I reproduced DEFECT-117 from the Network tab, found why changing a single filter did not send a request, fixed the cause rather than the symptom, and added a regression test that asserts the request that is sent (commit `f8337c1`). **Root cause: to be written in my own words before submitting.**

For the filtering I put every filter in one place, `TicketQueries.ApplyFilters`, as `IQueryable` expressions that are ANDed together before the count and the paging, so the database does the filtering and `totalCount` matches the filtered set (commit `773501b`). Adding the next filter means adding one `if` block.

## Task 2: escalation and assignment (backend)

The brief says the challenge is where the rules live, so I started by writing each rule as a small pure class (`PriorityRule`, `SlaCalculator`, `AgentSelector`, `EscalationPolicy`) with unit tests, before touching persistence. Triage on create and escalation both compose those classes, so there is one SLA calculation and one agent-selection rule.

Rules that are numbers (windows, premium multiplier, floor, at-risk threshold) come from the `Sla` configuration section. Category behaviour comes from the existing `RequiresSpecialist` and `ForcesCriticalPriority` columns, never from category names.

SLA status stays derived. Because the at-risk rule needs the start of the current window, I added one timestamp column, `SlaStartedAtUtc`, instead of storing a status. The `slaStatus` filter is the SQL translation of the same rules, written as date predicates so filtering, counting and paging all happen in the database.

Escalation history is an add-only collection on the `Ticket` aggregate, saved in the same unit of work as the ticket change, with an index chosen for the one query it serves (one ticket, newest first).

I did not finish the frontend part of Task 2.

## Task 3: authentication

Not finished. My plan was a `PasswordHash` on the existing agents (PasswordHasher), a JWT login endpoint, a fallback authorization policy so every endpoint is closed unless opened explicitly, the signing key from configuration, and `escalatedBy` taken from the token.

## Task 4: Docker

Not finished.
