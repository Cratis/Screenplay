---
id: 0106
title: The application calendar and clock operands `today` and `now`
status: proposed
stage: none
class: contract
reversibility: costly
decider: Sindre Alstad Wilting
amends: 0087
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Decision 0087 item 7 records a clock rule and admits nothing: the server reads the clock once, when it receives the
operation, and `today` is the **UTC** calendar date of that instant. That fixes the date for every deployment to one
zone. The first application that needs a business day in another zone is SoloCRM. Its specification asks for overdue
tasks, tasks due today and dormant requests, and says that "today" and every day boundary follow the configured zone
(`App__TimeZone`, default `Europe/Berlin`; `requirements/solocrm/docs/SPEC.md:289`, `:577`). Its model cannot say this:
`Crm.play:446` and `:664-665` declare `DormantOpportunities`, `OverdueTasks` and `TasksDueToday` as plain
`observable X[]` queries. The agent filtered them in hand-edited generated code
(`Source/Crm/Tasks/Tasks/Tasks.cs:29-32`, `Pipeline.cs:34`) that the next render overwrites. The rules live in
`TodayRules.cs:26`, `:31`, `:36`, `:41-58`, `:69-73`.

What exists today:

- `today` is already a `Value` in the grammar (`grammar.md:997-1004`). The binder refuses it with `PLAY0268`
  (`SemanticModelBinder.DateComparisons.cs:45-68`). Samples and fixtures use it, no consumer model does.
- Application-root preambles exist (`numbers exact`, `command reads protected`; `grammar.md:45-56`).
- Specifications supply the instant with `given clock` (`grammar.md:1251-1275`).
- Arc captures a receipt instant for queries and commands (`QueryContext.ReceivedAt`, `QueryContext.cs:34`;
  `CommandContext.ReceivedAt`, `CommandContext.cs:63`) from an injectable `TimeProvider`
  (`OperationContextScope.cs:18-25`). Arc has no time-zone concept: `Tenant` is an id and a name (`Tenancy/Tenant.cs`),
  and culture is invariant (`CultureWebApplicationBuilderExtensions.cs:27-40`). Chronicle schedules are UTC (decision
  0009).

This record is allowed by the record that lifts the freeze (0105 on branch `screenplay-1035-typed-capture-key`).
Decision 0004 and 0066 still apply: changed canonical bytes need a new ESM version.

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **Two operands.**
   - `now` is an instant (`DateTime`).
   - `today` is a calendar date (`Date`): the date of the evaluation instant in the effective calendar (item 3).
2. **Arithmetic.** Only a literal whole number of units is added or subtracted.
   - `today + N days` and `today - N days` are calendar arithmetic on a `Date`. Daylight-saving changes do not matter.
     The result is a `Date`.
   - `now + D` and `now - D`, with `D` as `N minutes`, `N hours` or `N days`, are exact elapsed time on an instant.
     In `now`-arithmetic a day is 24 hours. The result is a `DateTime`.
   - `today - 7 days` and `now - 7 days` differ on purpose: the first is a calendar day, the second 168 elapsed hours.
   - `N` is a literal in this record; a typed argument as `N` belongs to decision 0108.
3. **Which calendar.** One rule, resolved on the server, never from the browser, the caller's locale or a request
   value.
   - **Application zone.** The application root may declare `time zone "<IANA id>"`, in the preamble position of
     `numbers exact`. The id is validated at compile time. An unknown id is a compile error. Without the declaration the
     application zone is UTC, which keeps 0087 item 7's meaning for every model that does not opt in.
   - **Configurable zone.** The declaration may add `configurable`, as in `time zone configurable default
     "Europe/Berlin"`. Only then may the deployment set the zone through host configuration (SoloCRM's `App__TimeZone`).
     Without `configurable`, host configuration never changes the zone. An invalid configured zone fails visibly; it is
     never replaced by UTC.
   - **Per query or command.** A query or a command may declare `calendar "<IANA id>"` (a literal) or
     `calendar from $context.<member>`. The member is a member of a typed context (decision 0012) whose type is a time
     zone, which the host supplies and trusts. A request value is never such a source.
   - **Precedence**, first that applies:
     1. the query's or command's own `calendar`;
     2. the deployment-configured application zone, only when the declaration is `configurable`;
     3. the declared application zone;
     4. UTC.
   - **Specifications** pin the zone. They use the model's declared zone, or UTC when none is declared. A specification
     may say `given time zone "<IANA id>"`, or supply a context member through the existing context givens. Deployment
     configuration never affects a specification.
   - `now` does not depend on any calendar.
4. **One instant per evaluation.** The evaluation instant is the receipt instant, as in 0087 item 7. Nothing reads the
   clock again during evaluation. The operand's `Date` is derived from that instant and the effective zone. Both the
   instant and the zone are inputs: a specification supplies them with `given clock` and the zone rules of item 3.
   - For a command or a reaction, the zone is the one in effect when it is evaluated. A reaction that is redelivered
     reuses the original occurrence instant (0087 item 7) and resolves the zone again. If the zone changed in between,
     the answer can differ. This is accepted because zones change rarely; open point 4.
5. **Types and comparison.** `today` is a `Date` and compares with a `Date`. `now` is a `DateTime` and compares with a
   `DateTime`. Mixing them stays refused, as 0087 item 1 says. To compare a `DateTime` property with a day, write the
   conversion explicitly: `date of <DateTime operand>` yields the `Date` of that instant in the effective calendar. There
   is no implicit conversion in either direction, because an implicit one reads differently for `<` and `<=` and outside
   UTC.
6. **Absence.** An optional operand is never converted or compared implicitly. `date of x` on an absent `x` is absent.
   How absence is tested, and what a comparison with an absent value does, is stated by the construct that holds the
   comparison (decisions 0107 and 0108). This record admits no new null semantics.
7. **ESM.** The version is the next free ESM version, allocated at admission per 0025. It is selected only by a model
   that declares `time zone` (with or without `configurable`), uses a clock operand, or declares a `calendar`. Every other model keeps its bytes and
   version. The application carries `timeZone` (default `"UTC"` when only operands are used) and a `configurable` flag.
   A `calendar` carries either a literal id or a context member reference. Operands carry
   `{ kind: today|now, offset }`. Records 2 and 3 admit the positions; they share this version when admitted together.
8. **Amendment of 0087.** Item 7 of 0087 changes in one word: `today` is the date of the receipt instant in the
   application calendar, which is UTC when none is declared. Everything else in item 7 stays: the reaction instant rule,
   clients never evaluate it, `given clock`, and the refusal in reaction `where` and concept rules.

## Options considered

- **Keep UTC only.** Rejected: it is wrong at local midnight for every non-UTC business. The default stays UTC.
- **The caller's zone from the request or an identity claim.** Rejected: client-controlled input into a server
  decision, and different answers per caller. Two users must not see different "overdue" sets.
- **Tenant zone implicitly everywhere.** Rejected: some applications have no tenant calendar, and it changes the
  meaning 0087 recorded.
- **Zone in tenant settings only, no model declaration.** Rejected: specifications and the reference evaluator would
  have no deterministic zone.
- **Implicit `DateTime` to `Date` comparison in the application zone.** Rejected: `lastActivityAt <= today - 7 days`
  is correct in one reading and wrong in another (0:00 UTC of the date, as the design drafts read it). An explicit
  `date of` makes the reading visible.
- **A fixed offset instead of an IANA id.** Rejected: offsets do not follow daylight-saving changes.

## Consequences

- **Scene:** none. Display of local timestamps stays a client concern separate from the business day.
- **Stage:** renders the zone as configuration and evaluates the day with `TimeZoneInfo.FindSystemTimeZoneById`;
  admits the new ESM version or refuses it with a typed refusal. Specifications map `given clock` to a fake
  `TimeProvider`.
- **Arc:** needs an application time-zone abstraction with a default from options and a tenant hook, plus the
  `TimeProvider` boundaries of decision 0109, one at every change of the local date in either direction, including the
  backward change in a daylight-saving overlap. That is an Arc decision.
- **Chronicle:** none; stored instants stay UTC.
- **CLI:** bundles the new Screenplay and Stage versions.
- **AI corpus:** teaches `time zone`, the operands and `date of`.
- **Samples:** Invoicing and Library pin `today` as refused (0087); they move to the new version deliberately.
- Models that use none of this keep their bytes. No admitted model uses `today` today, so changing "UTC date" to
  "application-calendar date, UTC by default" changes no admitted behaviour.
- Container images that lack tzdata cannot resolve an IANA id. This is unverified for Cratis images.

## Open points

1. The typed-context contract for a time-zone member (for example `businessTimeZone`) that
   `calendar from $context.<member>` names. Decision 0012 gives the descriptor; admitting a time-zone type there is a
   prerequisite for admitting the `calendar from $context.<member>` form, and both are admitted together. The form is
   decided here, and so is its meaning.
2. Whether a per-tenant zone is needed now. The first increment is a model-declared zone, which a deployment may
   override when it is `configurable`; the Arc abstraction leaves room for a tenant value.
3. Whether the zone may differ between tenants of one running application when a model declares `time zone`.
4. Whether a redelivered reaction should keep the zone of its first evaluation instead of resolving it again. Today the
   zone is resolved again.

## Related

Decisions 0004, 0006, 0009, 0012, 0025, 0066 and 0087; Screenplay #824; the record that lifts the freeze (0105 on branch
`screenplay-1035-typed-capture-key`).
