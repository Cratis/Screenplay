---
title: "Audiences and availability"
description: "Decide whether part of a Screenplay model exists for a tenant, user or system caller using audiences, reusable availability blocks and available gates."
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
| `available for <audiences>` | the caller's tenant, user or system holds the audience or audiences; `not <Audience>` holds when the audience is known and not held |
| `available when <condition>` | the condition over `$context` is true |
| `available with <Availability>` | the named availability holds |

`for` and `with` combine names with `and`, `or`, `not` and parentheses, the way `authorize` combines policies. `not` binds tighter than `and`, which binds tighter than `or`, and applies to one audience name in a `for` line: `available for Plus and not Incident` and `available for not Incident` are valid; `not (Plus)`, `not not Plus` and `with not Open` are errors (`PLAY1171`). A `when` line is one condition and keeps its own `and` and `or`. A line holds one form; to mix forms, name them in an `availability` block and use `with`. Several `available` lines on one construct all have to hold.

A gate is allowed on a module, feature, slice, command, query, screen and on a screen `action` or `data` element (see [Gate a screen element](#gate-a-screen-element)). Gates AND down the path from module to construct, and availability is decided **before** `authorize`: a part that is not available is refused as not available, which clients can tell apart from not allowed.

## Gate a screen element

An element of a screen that is bound to a command or a query is offered only when the caller may use it **and** the part it binds is available. A button bound to a command and a table bound to a query follow the same path the command or query follows: suspension first, then the gates down the module, the feature, the slice and the construct, and then `authorize`. You write nothing for that; it is derived. A renderer that asks for the availability of an element is told whether it is available, unavailable because the part is not in the caller's offer (`NotAvailable`) or because an operator suspended it (`Suspended`), unavailable because the caller may not use it, or undecided.

You can also gate an element yourself. A gate goes on a screen `action` or `data` element, one form per line as everywhere else:

```screenplay
screen VesselScreen
  data Vessel via query VesselByKey by vesselId
    available for Plus
  action ReportVessel for screen
    label "Report"
    available for Plus and not Incident
```

- **An element gate can only narrow.** The derived path always applies, so a gate on an element is added to it. A gate that can be on while the bound command or query is not available is broader than the part and is reported (`PLAY1172`); narrow it or remove it.
- **With no bound command or query a gate would be presentation only.** Nothing can refuse a request there, so a gate on such an element would hide nothing the system enforces. Only the `action` and `data` elements bind a command or a query, and they are the only elements that take a gate today; an element with nothing bound (a section, a table, a summary, a component) takes none, and a renderer decides what it shows from the elements it holds.
- **Availability is asked with the data the screen shows.** An element is asked with the values of the data on the screen, for example one vessel through `provide vessel ... by arguments.vesselId`, not only with the tenant and the user.
- **An element gate is part of the executable model from ESM v59.** A model with no gate on a screen element keeps its earlier version, and the member is written only when an element has a gate.
- **The reference evaluator offers everything.** Unless it is given the audiences of the caller and the parts an operator suspended, it reports every element available, as specifications run. Given them, it reports the path of the bound part and the explicit gate, and leaves a `when` condition undecided because it reads context the model does not evaluate.

## Rules the language holds

- **Unknown never opens a gate.** A missing audience, a missing value or an unresolved condition leaves the part unavailable, as with policies. `not` is three-valued: `not` of an unknown audience is unknown, so the part stays closed. A part is closed for a *granted* audience by granting it, for example an `Incident` audience; withdrawing the grant reopens the part. The union rule is unchanged.
- **A negated audience needs ESM v58.** The executable model carries it as `"negated": true` on the audience requirement, so a model that uses no `not` keeps its earlier version.
- **Events and observers are unaffected.** A reaction that produces into an unavailable slice is fine, and projections keep running, so switching a part on again needs no replay.
- **Specifications always run with everything available.** They verify behavior, not entitlement, so there is no `given availability` form, and the reference evaluator treats every gate as available.
- **Personas carry audiences for authoring only.** `audience Plus` under a `persona` tells the board, Studio and documentation which audiences the persona is in. It changes nothing at runtime and is not part of the executable model.
- **The runtime context carries `$context.audiences`**, the union of the tenant's, the user's and the system caller's audiences. Withdrawing a grant removes it from its own level only.

## Values a condition may read

An `available when` condition reads `$context`. Values come from the built-in members and from `provide` lines in scope; see [contexts](context.md). A condition that reads a value nothing provides there is reported (`PLAY1138`) and stays closed.

## Calling a command that may be unavailable

A screen action, a form or a reaction that `invokes` a command whose audiences can be off while the invoker's are on can be refused as not available. The compiler warns (`PLAY1139`), comparing audience sets. `when` conditions are opaque and are skipped, never guessed; so is any gate that contains `not`, because whether it differs depends on grants the model cannot see. A reaction is never gated, and it runs with the tenant's and the system's audiences, never a user's.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PLAY1130`, `PLAY1131`, `PLAY1132` | A malformed `audience`, `availability` or `available` line |
| `PLAY1133`, `PLAY1134` | A duplicate audience or availability |
| `PLAY1135`, `PLAY1136` | An audience or availability that is not declared |
| `PLAY1137` | Availabilities that build on each other in a cycle |
| `PLAY1138` | A `when` that reads a value nothing provides |
| `PLAY1139` | An invocation of a command that can be unavailable |
| `PLAY1172` | The gate on a screen `action` or `data` element is broader than the command or query it binds |
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
