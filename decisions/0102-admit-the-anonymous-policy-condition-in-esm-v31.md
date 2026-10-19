---
id: 0102
title: Admit the anonymous policy condition in ESM v31 (exception to the freeze)
status: accepted
stage: none
decided: 2026-10-19
decider: Sindre Wilting (delegated to the L0 coordinator)
class: product
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/Invoicing/**
---

## Context

Stage is moving generated applications to `[Authorize]` by default (https://github.com/Cratis/Stage/issues/494). A
command, query or screen that has no `authorize` then refuses callers without an identity. A Screenplay model has no
way to say, explicitly and visibly, that an artifact may be used without a caller, so Stage has nothing to map to
`[AllowAnonymous]`.

The decision on Stage#494 (https://github.com/Cratis/Stage/issues/494#issuecomment-6263507049) settles three things
this record carries out:

- Item 3: the reference semantics of an artifact without `authorize` stay unchanged. It allows every caller, with or
  without an identity. Generated applications diverge visibly through a Stage render diagnostic.
- Item 4: the opt-in is an `anonymous` policy condition, a new ESM kind, admitted as the one scoped exception to the
  ESM v28 freeze (decision 0092).

The blocked model is any Screenplay application Stage renders with a public surface: catalog browsing, sign-up, a
public verification page. The alternative tried is to leave such artifacts without `authorize`. Under Stage#494 that
renders `[Authorize]`, so the alternative no longer produces a public endpoint and is not reasonable.

## Decision

1. A policy may say `require anonymous`. The condition always holds: a caller without an identity passes, and so does
   any authenticated caller, a system identity and a reaction that runs as an identity. It does not mean "only callers
   without an identity".
2. `anonymous` must be the whole effective authorization of a command or query:
   - `PLAY0804`: `anonymous` combined with another condition inside `require` (`and`, `or`).
   - `PLAY0805`: `not anonymous`; `authenticated` says that.
   - `PLAY0806`: an `authorize` expression that combines an anonymous policy with another policy.
   - `PLAY0807`: an anonymous gate under, or above, another gate (module, feature, command or query). The reference
     semantics AND the gates, while ASP.NET lets `[AllowAnonymous]` override `[Authorize]`; refusing the combination
     keeps the reference evaluator and generated applications in agreement.
3. A specification of a command or query whose effective authorization is anonymous may omit `given caller`. Every
   other authorized specification still needs one (`PLAY0389`): a missing identity is never assumed.
4. The condition is admitted in **ESM v31**, serialized as `{"kind":"anonymous"}`. Only a model that declares an
   anonymous policy selects v31. Every other model selects the version it selects today, and its bytes do not change.
5. This is an item 4 exception to decision 0092. Consumers admit v31 under item 5: Stage maps the condition to
   `[AllowAnonymous]` (Stage PR B for #494), and until then refuses v31 with a typed refusal.

## Options considered

- Admit in a new ESM v31. Chosen: v30 is claimed by decision 0098 and may already be under admission in Stage, so
  reopening it would move a target a consumer is working towards.
- Fold the condition into the unreleased v30, the way decision 0027 added `not` to v7. Avoids one version for
  consumers, but changes the meaning of a version already claimed.
- A new `authorize anonymous` form instead of a policy condition. Rejected on Stage#494: it fits the policy model worse
  and needs a second ESM shape for authorization.
- Let an anonymous gate override enclosing gates, like ASP.NET. Rejected: it changes the meaning of the gates around it
  in the reference semantics; refusing the combination keeps one meaning everywhere.

## Consequences

- Stage, the CLI, Studio and the AI corpus admit v31 under 0092 item 5.
- A declared but unused anonymous policy still selects v31, because every policy is serialized.
- A `then denied` on an anonymous command can never hold; no diagnostic for it yet.

## Related issues

Cratis/Screenplay#951; Cratis/Stage#494; decisions 0092 and 0098.
