---
id: 0157
title: A policy state condition compares with the caller's identifier in ESM v49
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-10
decider: Einar Ingebrigtsen (direct request to deliver Cratis/StudioIssues#1387)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/policies.md
---
## Context
A policy that reads state could compare a recorded property only with a constant ([0151](0151-policies-that-decide-from-recorded-state-esm-v44.md)). "Only the author of an article may edit it" needs the recorded author compared with who is asking ([Screenplay#1233](https://github.com/Cratis/Screenplay/issues/1233)).
## Decision
- A state condition may compare a read property with `$identity.id` using `==` or `!=`: `require authenticated and article.author == $identity.id`. The property must be a required text value. Other caller details (`$identity.name`, claims) and artifact operands are still refused at binding.
- A caller fixture names its identifier with `id "alice"` beside `authenticated`, `role` and `claim`. It is the value `$identity.id` reads when a specification runs.
- A caller with no identifier gives nothing to compare, so the comparison is false: absence cannot grant access. `!=` against a missing identifier is also false for the same reason, because a policy that cannot tell who is asking must not allow.
- The identifier is a distinct operand (`callerId`) on the condition, not a string the evaluator recognizes, so a literal that happens to read `$identity.id` is still a literal.
- Only a model that uses it selects ESM v49; the caller `id` and the operand are omitted from the bytes otherwise, so every earlier model keeps its bytes and revision.
## Consequences
- Stage renders the operand from the authorized principal's identifier ([Stage](https://github.com/Cratis/Stage/issues/1050) follow-up) and refuses it until then.
