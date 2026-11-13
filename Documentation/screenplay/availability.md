---
title: "Audiences and availability"
description: "Decide whether a part of a Screenplay model is there at all for a tenant, user or system caller with audiences, reusable availability blocks and available gates."
---

# Audiences and availability

`policy` and `authorize` say who may do something. Availability says whether a part of the application is *there at all* for the caller's tenant, user or system. A commercial package that includes Data Quality, a capability rolled out to a few tenants first and a part switched off for an incident all use the same three pieces: an `audience`, an `availability` and an `available` gate.

A Screenplay audience is who a part of the application is available to. It is unrelated to a token's `aud` claim (`$context.identity.claims.aud`).

## Declare audiences and availabilities

```screenplay
audience Plus
  description "Customers on the Plus commercial package"

audience Preview
  description "Tenants taking part in the preview"

availability DataQuality
  description "Sold with the Plus package, once the vessel has reported"
  for Plus
  when $context.vessel.hasReported == true
```

An `audience` is a named group with a description. The model declares audiences and never who is in them, so nothing can ship as an accidental entitlement. Membership comes from the runtime: tenants, users and the system caller are granted audiences, and the context carries the merged result.

An `availability` is a reusable rule. Its `for`, `when` and `with` lines all have to hold, and `with` builds on another availability. Availabilities that build on each other in a cycle are an error (`PLAY1137`).

## Gate a part of the model

```screenplay
module Fleet
  feature DataQuality
    available with DataQuality

    slice StateView QualityScores
      available for Plus or Preview
```

| Form | Holds when |
| --- | --- |
| `available for <audiences>` | the caller's tenant, user or system holds the audience or audiences |
| `available when <condition>` | the condition over `$context` is true |
| `available with <Availability>` | the named availability holds |

`for` and `with` combine names with `and`, `or` and parentheses, the way `authorize` combines policies. A `when` line is one condition and keeps its own `and` and `or`. A line holds one form; to mix forms, name them in an `availability` block and use `with`. Several `available` lines on one construct all have to hold.

A gate is allowed on a module, feature, slice, command, query and screen. Gates AND down the path from module to construct, and availability is decided **before** `authorize`: a part that is not available is refused as not available, which clients can tell apart from not allowed.

## Rules the language holds

- **Unknown never opens a gate.** A missing audience, a missing value or an unresolved condition leaves the part unavailable, as with policies.
- **Events and observers are unaffected.** A reaction that produces into an unavailable slice is fine, and projections keep running, so switching a part on again needs no replay.
- **Specifications always run with everything available.** They verify behavior, not entitlement, so there is no `given availability` form, and the reference evaluator treats every gate as available.
- **Personas carry audiences for authoring only.** `audience Plus` under a `persona` tells the board, Studio and documentation which audiences the persona is in. It changes nothing at runtime and is not part of the executable model.
- **The runtime context carries `$context.audiences`**, the union of the tenant's, the user's and the system caller's audiences. Withdrawing a grant removes it from its own level only.

## Values a condition may read

An `available when` condition reads `$context`. Values come from the built-in members and from `provide` lines in scope; see [contexts](context.md). A condition that reads a value nothing provides there is reported (`PLAY1138`) and stays closed.

## Calling a command that may be unavailable

A screen action, a form or a reaction that `invokes` a command whose audiences can be off while the invoker's are on can be refused as not available. The compiler warns (`PLAY1139`), comparing audience sets. `when` conditions are opaque and are skipped, never guessed. A reaction is never gated, and it runs with the tenant's and the system's audiences, never a user's.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PLAY1130`, `PLAY1131`, `PLAY1132` | A malformed `audience`, `availability` or `available` line |
| `PLAY1133`, `PLAY1134` | A duplicate audience or availability |
| `PLAY1135`, `PLAY1136` | An audience or availability that is not declared |
| `PLAY1137` | Availabilities that build on each other in a cycle |
| `PLAY1138` | A `when` that reads a value nothing provides |
| `PLAY1139` | An invocation of a command that can be unavailable |
| `PLAY1140`, `PLAY1141` | An unexpected token or an unclosed parenthesis in a gate |

## How a refusal reads

A part that is not available is refused with the reason `NotAvailable`, distinct from an authorization denial, so a client can hide an entry or explain that the capability is not part of the package. A failure to decide (a lookup that errors) is an error, never a denial. An undecided result counts as unavailable.

## Relation to Chronicle audiences and tokens

Chronicle routes events with its own audience label, which stays in event options and is unrelated. A token's `aud` claim says who the token is for and is read as `$context.identity.claims.aud`. A Screenplay audience is neither.

## Who does what at runtime

| Piece | Owns |
| --- | --- |
| Choreography | The engine that evaluates an availability for a request: available, unavailable with a reason, or unknown, which denies |
| Showrunner | The audience contracts: public events that grant and withdraw an audience for a tenant, a user or the system; the newest wins |
| Arc | Enforcement at the command and query boundary, before authorization |

Applications never call these directly; the model says what is gated and the runtime enforces it.

## Relation to Box Office

Audiences are a separate mechanism beside Box Office module entitlements, which keep owning tenant-level commercial modules. A gate that needs both can read a host-provided value in `available when`, for example `provide entitlements from host`.

## See also

- [Policies](policies.md) for `authorize`
- [Personas](personas.md) for persona `audience` lines
- [Contexts](context.md) for `provide` and `$context`
- [Grammar](grammar.md) for the productions
