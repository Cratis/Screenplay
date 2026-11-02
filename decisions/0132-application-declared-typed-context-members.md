---
id: 0132
title: Applications declare typed-context members, starting with time-zone members, published as sidecar revision 3
status: proposed
stage: none
class: contract
reversibility: costly
decider: proposed for woksin's review (Cratis/Screenplay#1120)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Decision [0106](0106-the-application-calendar-and-clock-operands.md) lets a command write `calendar from $context.<member>`. The member is "a member of a typed context (decision 0012) whose type is a time zone, which the host supplies and trusts". Its first open point asks for the typed-context contract of such a member, for example `businessTimeZone`, and says the form and the contract are admitted together. Cratis/Screenplay#1120 asks for exactly that: an application declares its own time-zone member, `calendar from $context.<member>` binds against it, and the declaration is published in a versioned contract that consumers can refuse or accept explicitly.

State on `origin/main` at the time of writing:

- The grammar parses the application `time zone` declaration and `calendar from $context.<QualifiedName>` (`Documentation/screenplay/grammar.md`). Nothing binds. The binder refuses both with `PLAY0268` and names #824 (`SemanticModelBinder.ClockOperands.cs`, called from `SemanticModelBinder.cs`).
- No construct declares an application-owned member of the typed context. The closest precedent is the `identity` block, whose details are authoring metadata and not executable behavior (`grammar.md`, decision [0052](0052-identity-details-as-authoring-metadata.md)). The `$context` roots are a fixed list (`Syntax/Expressions.cs`, `KnownRoots`), and descriptor source kinds are a closed vocabulary (`SemanticContextSourceKinds`).
- Cratis/Screenplay#1044 (clock-operand binding, not merged when this record was written) proposes one fixed member: `calendar from $context.timeZone`, a `TimeZone` runtime token in the descriptor, and sidecar `contractRevision: 2`, written only when the token is used. This record builds on that work and does not change it.

If nobody chooses, the application zone and a command's `calendar "<id>"` stay the only ways to name a calendar, and `calendar from` stays limited to the one fixed member. An application that needs a zone from its host, such as a per-tenant zone, has no way to say so.

## Decision (proposed)

The five choices below are proposals, each with the options considered. None is accepted until woksin accepts this record.

### D1. Declaration: a root `context` block with `<name> time zone` lines

The preamble gets at most one `context` block, after the `time zone` declaration:

```screenplay
time zone configurable default "Europe/Oslo"
context
  businessTimeZone time zone
```

- Each line is `<memberName> time zone`. The member type vocabulary is closed, and `time zone` is its only entry.
- The block merges across the files of a folder, as `identity` does. A member is declared once.
- The names `timeZone` and the built-in `$context` roots (`command`, `arguments`, `tenant`, `causedBy`, `causation`, `occurred`, `identity`) are reserved.
- Grammar: `ContextDecl = "context", NL, INDENT, { DescriptionDecl | ContextMemberDecl }, DEDENT ;` and `ContextMemberDecl = PropertyName, "time", "zone", NL ;`.

Options considered:

- **A `context` block that reuses the `identity` block's shape.** Proposed. It follows an existing pattern and keeps a closed type vocabulary.
- **`<name> TimeZone` with a new primitive type.** Rejected: it collides with user concepts called `TimeZone` and invites `TimeZone` command properties. Decision 0106 forbids request values as a zone source.
- **An implicit declaration by use in `calendar from`.** Rejected: a typo would declare a new member and could not fail closed.
- **Identity claim details as zone sources.** Rejected by 0106: claims are client controlled.

### D2. Specification supply: `given time zone` supplies every declared member

`given time zone "<id>"` supplies `timeZone` and every declared time-zone member. No new grammar. This matches the supply of `$context.timeZone` in #1044.

Options considered:

- **`given time zone` for all members.** Proposed.
- **A new `given context <member> "<id>"` per member.** Not taken now. It is needed only when two declared members must differ inside one specification, and it can be added later without changing this decision.

### D3. Versioning: sidecar revision 3, a new source kind and a member registry

- A command whose calendar reads declared member `m` gets the descriptor member `Pascal(m)` with the runtime token `TimeZone` and the source `{ kind: "application-context", path: "<m>" }`. `application-context` is a new value of the closed source-kind vocabulary.
- The typed-context sidecar writes `contractRevision: 3`, includes the `application-context` source kind, and adds a top-level `contextMembers` registry, `[{ name, runtimeToken: "TimeZone" }]`. The registry lets a consumer render one host provider per declared member.
- All of this is written only when at least one member is declared and read. Models that declare none keep their revision 1 or revision 2 bytes, byte for byte. `typed-contexts-v1.json` does not change.
- **The ESM canonical bytes do not change.** The declaration is candidate-only, like the application zone in #1044. It ships in the ESM version that clock admission numbers at admission (0106 item 7, decision [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)). This record allocates no ESM version.
- Semver impact: minor everywhere. The sidecar revision is additive.

Options considered:

- **Revision 3 with `application-context` and `contextMembers`.** Proposed.
- **Fold the new members into revision 2 if #1044 has not been released when this lands.** Not taken. It saves a consumer step but ties the meaning of revision 2 to release timing.

### D4. Record: a new decision, with status notes on 0106 and 0012

This record resolves 0106 open point 1. It adds grammar and a contract revision, which is more than an amendment. 0106 and 0012 receive dated status notes and are not rewritten.

Option not taken: a status note on 0106 only. Rejected, because the descriptor contract of 0012 changes as well.

### D5. Semantics: the host supplies every member, with no fallback

- There is no default value for a declared member. The host supplies each one by name.
- A member that is missing, empty or not an IANA id fails the operation. There is no fallback to the application zone and none to UTC (0106: a request value is never a zone source, and an invalid zone fails visibly).
- A declared member is readable only by `calendar from` in this record. Any other `$context.<declared>` path keeps its current unknown-root behavior.
- `calendar from $context.<m>` binds when `m` is `timeZone` (#1044) or a declared time-zone member. Any other member is `PLAY0853` (`InvalidCalendarContextMember`, proposed in #1044), and its message lists the declared members.

Option not taken: fall back to the application zone when a member is missing. Rejected, because it hides missing host plumbing and gives a request an answer the host never chose.

## Default if unanswered

0106 open point 1 stays open. Applications keep `calendar "<id>"` and the application zone. `calendar from` stays limited to the fixed `timeZone` member of #1044, and a model that needs a host-supplied zone under another name is refused. The cost is workarounds in host code, and a zone that cannot differ by command or tenant without a model change.

## Timeline and scope

In scope: the `context` declaration on every Screenplay surface (both compilers, printers, syntax serialization, MCP, editors, documentation), binding of `calendar from` against declared members, sidecar revision 3 and its vector, and the execution request that carries declared zones.

Out of scope: declaring other member types, reading a declared member outside `calendar from`, a per-member `given context` form, a per-tenant zone (0106 open point 2), and any ESM version number, which belongs to clock admission.

Work follows in this order:

1. **Syntax on every surface, refused at binding.** Grammar, both parsers, folder merge, printer, syntax serialization, diagnostics, editor surfaces and documentation. The binder refuses a declaration with `PLAY0268` naming #824, as it does today for the clock.
2. **Candidate binding, sidecar revision 3 and execution.** Starts after #1044 has merged. Adds binding, the `application-context` source kind, `contextMembers`, a revision 3 vector and the execution request field.
3. **Consumer admission**, tracked in each consumer's own repository (see Consequences).

## Verification

Done when:

- An application can declare a time-zone member, and `calendar from $context.<member>` binds against it in the clock candidate.
- The sidecar for a model that reads a declared member says `contractRevision: 3`, lists the `application-context` source kind and the `contextMembers` entry. A model that declares nothing produces unchanged bytes.
- An undeclared member, a declared member of the wrong type and a missing, empty or non-IANA zone each fail, and none falls back to UTC.

Verify by: binder specifications for declared, undeclared and reserved members; serializer and packaged-vector specifications for revisions 1, 2 and 3, with the existing vectors unchanged; execution specifications for a supplied zone, a missing zone and an invalid zone; parse, merge and printer round-trip specifications.

## Consequences

- **Stage:** admits typed-context revisions {1, 2, 3} (it refuses everything above revision 1 today). It renders the `TimeZone` member, renders one host provider seam per `contextMembers` entry, resolves ids with `TimeZoneInfo.FindSystemTimeZoneById`, and fails closed.
- **Studio:** unknown sidecar revisions, runtime tokens and source kinds must be refused, not displayed. Studio checks only the per-descriptor version today, so it must land the refusal before or together with the Screenplay release that can write revision 3, then admit revisions 2 and 3.
- **cli:** forwards descriptors and has no admission of its own. It needs a pin bump after Screenplay and Stage release.
- **AI corpus:** teaches the `context` block, the binding rule and the fail-closed supply.
- **Arc and Scene:** none, as 0106 records.
- **Chronicle:** none.
- Revision 3 is candidate-only until clock admission, so the exposure of a consumer that has not yet admitted it is small.
- A model that uses the fixed `timeZone` member (revision 2) is already refused by Stage. That is explicit, and it means no clock model renders until Stage admits revision 2.
- Container images that lack tzdata cannot resolve an IANA id (0106 Consequences). This is unverified and is a Stage runtime concern.

## Related

Decisions 0012, 0025, 0052 and 0106. Cratis/Screenplay#1120, #1044, #824 and #1126. #1126 changes the zone member spelled in the two clock fixtures and overlaps with the fixtures this work edits.
