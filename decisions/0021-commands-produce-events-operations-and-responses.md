---
id: 0021
title: Commands produce events, operations and responses
status: accepted
stage: none
decided: 2026-09-28
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Syntax/CommandSyntax.cs
  - Source/DotNET/Screenplay/Syntax/ScreenplaySyntaxWalker.Commands.cs
  - Source/DotNET/Screenplay/Semantics/Versions.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceDiagnosticRepairs.cs
  - Documentation/screenplay/commands.md
  - Documentation/screenplay/events.md
---

## Context

A Screenplay command today declares its events elsewhere and maps them with `produces <Name>`. It cannot say which value is the runtime-generated event source id, return a value to the caller, run a side effect that must succeed with the command, or state the append metadata Chronicle already accepts. Chronicle and Arc support all of these; the model does not.

- **Identity.** Chronicle takes the event source id as an argument to every append, never as payload. Arc resolves it from `ICanProvideEventSourceId`, a typed or `[Key]` property, or `EventSourceId.New()`. Today's `commands.md` examples copy the identifier into the event payload, and a generated id is anonymous, so it cannot be returned.
- **Responses.** Arc returns one unhandled value and clears it on any failure. Screenplay has no response in its command contract, and Stage's live runtime never passes a response to forms.
- **Operations.** Arc's `ICommandOperation` runs after events are enrolled and before commit, with compensation governed by the commit disposition. Screenplay has nothing equivalent.
- **Append metadata.** Event source type, stream type and stream id are properties of the append. Arc applies them per command. The legacy `concurrency` block names these dimensions but does not bind ([0003](0003-decision-consistency-for-command-reads.md) item 3), and the documented "no concurrency check" default is wrong: Chronicle's optimistic strategy applies.

Existing decisions constrain the answer. [0001](0001-chronicle-runtime-semantic-authority.md) makes Chronicle's runtime meaning authoritative. [0004](0004-admission-and-governance-of-portable-executable-semantics.md) governs new ESM versions and Stage admission. [0008](0008-one-data-subject-per-event.md) fixes one subject per event. [0009](0009-external-event-origin-and-translation-slices.md) keeps a command's own events local. [0011](0011-event-generations.md) and [0015](0015-event-generations-in-the-executable-model.md) define generations and their bytes. [0012](0012-typed-context-descriptor-and-command-handler-role.md) defines the handler attachment pattern operations reuse. [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md) requires typed repairs. [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) owns decision consistency through `reads`.

Issue [#298](https://github.com/Cratis/Screenplay/issues/298) set the guarantees any new event syntax must keep: order-independent, per-file, printer-invertible and typo-safe.

## Decision

**1. Inline events.** `produces event <Name>` declares an event where it is produced. Its body lines are typed and mapped (`name Type = source`). Plain `produces <Name>` references a declared event. There is no `new` keyword. The binder lowers an inline event to a declared event plus a production, so canonical ESM bytes are identical to the explicit form and the #298 guarantees hold. Inline events are generation 1 only and `generation` is reserved in the header; a typed "extract inline event" repair produces standalone generations to evolve one. An optional `id "<event-type-id>"` on events, inline or standalone, pins the event-type identity. Inside `produces event`, `tag` is an event-type tag; plain `produces X` keeps its per-production tag. `description` and `documentation` are allowed on `produces event`, `produces operation`, response records, standalone events and standalone operations. They are authoring metadata and add no ESM bytes.

**2. The event source id is never payload.** Inside `produces event`, a missing `for` means the command's `identifier`. Plain `produces X` keeps today's meaning; the documentation is corrected and a typed repair adds an explicit `for`. Mapping the identifier into a payload raises `EventSourceIdInPayload` (a warning on inline events, information on declared ones) with two typed repairs per 0014: move identity to the event source, or declare generation 2 without the identity.

**3. `generated`.** A modifier on any command property whose type is a concept over `Uuid`. `generated identifier` (canonical printed order) is a generated event source id; `generated` alone is another generated value. A generated property is not a request input: it is absent from forms and proxies. Specifications supply it with `when … for` (the identifier) or `when … generated <name> = <value>`.

**4. Responses.** `returns <property>` comes first: one response value, cleared on failure, rendered as an Arc tuple. Named response records, `returns <Name>` with typed lines, follow later. Screens and forms bind returned names in `on submit` and `on success`; using one under `on failure` is an error. Specifications assert with `then returns`.

**5. Append metadata.** The modeler may express the event source (`identifier`, `for`, `generated`), a command-level `append` block with `sourceType`, `streamType` and `streamId` (a literal or `= property`) applied to every event, an optional per-production `occurred at <source>` (imports and replays only), a subject (0008) and tags. Per-event routing waits for Arc C# parity. System-assigned values are reserved and produce a diagnostic: namespace (tenant scope), sequence number, correlation, causation, caused-by and on-behalf-of, event store and hash. The legacy `concurrency` block is deprecated, with a typed repair moving its dimensions into `append`. Decision consistency stays with 0017 `reads`.

**6. Operations.** `produces operation <Name>` declares one inline, and a standalone `operation <Name>` is referenced by `produces <Name>`. Names are unique per slice across events and operations. Body lines are typed inputs. Optional `execute` and `compensate` lines each take a fenced code block or a `file` reference, following the handler pattern of 0012. Dependencies are declared in the model (`uses IWelcomeEmails as emails`), and Stage generates the Arc `ICommandOperation` record and method signatures. A missing body fails closed in Stage. Operations run after events enroll and before commit, in authored order, with compensation per Arc's commit-disposition rules. Specifications describe the success path by default; failure appears only through `given operation <Name> fails` with `then compensated <Name>`.

**7. Delivery.**

| Phase | Scope | ESM impact |
| --- | --- | --- |
| 1 | `produces event`, `id`, `EventSourceIdInPayload` and repairs, `description`/`documentation`, event-level `tag`, documentation fixes | None; bytes identical |
| 2 | `generated`, `returns <property>`, `then returns`, UI binding | Next unallocated version |
| 3 | `subject` keyword (0008) | Joins phase 2's version or takes the next |
| 4 | Operations, `given operation … fails`, `then compensated` | Next unallocated version |
| 5 | `append`, `occurred at`, multi-source `for` | Next unallocated version where bytes change |
| 6 | Response records | Next unallocated version |

This record allocates no version number. v5 and v6 are held by [#284](https://github.com/Cratis/Screenplay/issues/284) and [#285](https://github.com/Cratis/Screenplay/issues/285). Each phase after 1 fixes its number in its own dated amendment or follow-up decision under 0004, needs Stage admission that fails closed, and ships golden vectors.

```screenplay
slice StateChange RegisterProject
  command RegisterProject
    projectId  ProjectId generated identifier
    name       ProjectName
    ownerId    UserId
    ownerEmail EmailAddress
    append
      sourceType Project
    produces event ProjectRegistered
      tag onboarding
      name       ProjectName    = name
      ownerId    UserId subject = ownerId
      ownerEmail EmailAddress   = ownerEmail
    produces operation SendWelcomeEmail
      uses IWelcomeEmails as emails
      recipient   EmailAddress = ownerEmail
      projectName ProjectName  = name
      execute
        ```csharp
        await emails.Send(Recipient, ProjectName, cancellationToken);
        ```
    returns projectId
```

## Options considered

- **Declared inline with `produces event` (taken):** the header states the kind, keeps one place to read a command, and lowers to today's form.
- **Implicit declaration on first `produces`:** rejected. A typo in a name silently declares a new event, which breaks the typo-safe guarantee from #298, and the declaring occurrence depends on file order.
- **A `new` keyword (`produces new <Name>`):** rejected. It adds a word that carries no meaning `event` does not, and it reads as allocation.
- **Stream metadata per production:** deferred. Arc overwrites wrapper values from the command context, so a per-event value would not survive to Chronicle. Revisit when Arc C# honors it.
- **Inline operation code as full methods, or a context service locator:** rejected in favor of declared dependencies. Full methods put signatures in opaque text Screenplay cannot check, and a locator hides what an operation needs, so Stage cannot generate the record or a specification double.
- **Tenant or namespace routing:** rejected. The executing tenant's scope fixes the namespace, and a command cannot target another one. `$context.tenant` stays a value, never routing.

## Default if unanswered

Commands keep copying identifiers into payloads, generated ids stay anonymous, forms cannot navigate to what a command created, and side effects live in hand-written code outside the model. Stage keeps rendering none of Arc's operation or response constructs.

## Timeline and scope

In scope: command productions, responses, operations and append metadata in the language, ESM and Stage admission. Phase 1 can start immediately; later phases follow the order above. Out of scope: named and dynamic tags (Arc drops named tags), `$context` identity in metadata, handler return values (a 0012 follow-up), operations in reactions, reads and decision consistency (0017), and collection or read-model responses.

## Verification

**Done when:** Phase 1 parses, prints and round-trips `produces event` with bytes identical to the explicit form, `EventSourceIdInPayload` offers both typed repairs, and `commands.md` no longer copies the identifier into payloads or claims an unchecked default. Each later phase adds its syntax, a fail-closed Stage admission and a golden vector under its own version.

**Verify by:** Corpus vectors comparing inline and explicit canonical bytes; order-independence and typo-safety specs; printer round-trip (0013); repair specs proving each repair compiles or is not offered; for phase 2 onward, evaluator specs for `then returns`, generated properties and operation failure, and Stage specs that refuse unadmitted constructs.

## Consequences

Authors write a command and its events in one place, and the identity rule stops the id from being duplicated in payloads. Generated ids can be returned and used by forms. Operations get a typed, checkable home and a defined failure path. Cost: several phases and ESM versions, Stage and Scene work for responses and operations, and a deprecation of the `concurrency` block. Operation bodies remain opaque code, so their behavior is checked only on the success path in specifications.

## Related issues

Screenplay: [#284](https://github.com/Cratis/Screenplay/issues/284), [#285](https://github.com/Cratis/Screenplay/issues/285), [#298](https://github.com/Cratis/Screenplay/issues/298), [#299](https://github.com/Cratis/Screenplay/issues/299), [#300](https://github.com/Cratis/Screenplay/issues/300), [#301](https://github.com/Cratis/Screenplay/issues/301), [#302](https://github.com/Cratis/Screenplay/issues/302), [#303](https://github.com/Cratis/Screenplay/issues/303), [#304](https://github.com/Cratis/Screenplay/issues/304), [#305](https://github.com/Cratis/Screenplay/issues/305), [#141](https://github.com/Cratis/Screenplay/issues/141) (subject). Stage: [#175](https://github.com/Cratis/Stage/issues/175), [#176](https://github.com/Cratis/Stage/issues/176), [#177](https://github.com/Cratis/Stage/issues/177), [#178](https://github.com/Cratis/Stage/issues/178). Scene: [#53](https://github.com/Cratis/Scene/issues/53). Arc: [#2885](https://github.com/Cratis/Arc/issues/2885), [#2886](https://github.com/Cratis/Arc/issues/2886), [#2887](https://github.com/Cratis/Arc/issues/2887). Decisions: 0001, 0003, 0004, 0008, 0009, 0011, 0012, 0013, 0014, 0015, 0017.

## Status notes

**2026-09-28 — accepted, not implemented.** Sindre Alstad Wilting accepted the seven decisions above. No syntax, binding or rendering exists yet.
