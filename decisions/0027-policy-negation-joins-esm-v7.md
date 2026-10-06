---
id: 0027
title: Admit policy negation as a byte-preserving ESM v7 extension
status: accepted
stage: none
decided: 2026-10-06
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[#431](https://github.com/Cratis/Screenplay/issues/431) needs policies that distinguish a person from a service identity without inventing a positive claim. The existing conditions (`authenticated`, `role`, `claim … matches`, `and`, `or`) cannot express their complement. The ESM has no negation node and its strict reader rejects unknown condition variants.

[0004](0004-admission-and-governance-of-portable-executable-semantics.md) permits a construct that previously failed binding to join the highest ESM version when no model that bound before changes bytes. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) and [0026](0026-generated-values-and-command-responses-in-esm-v7.md) admitted generated values and responses as v7. This record adds policy negation to that version without changing their contract or assigning a new version.

## Decision

Policy negation joins ESM v7 as a byte-preserving extension under 0004. A model using negation selects language/semantic `7.0` and `schemaVersion: 7`; every model without it retains its existing version, canonical bytes, revision and outcomes. A policy condition accepts general unary `not <condition>`. `not` binds tighter than `and`, which binds tighter than `or`; parentheses override precedence, and repeated negation nests rightward.

The syntax and ESM carry one operand, preserving authored grouping. Its canonical JSON is `{"kind":"not","operand":<condition>}`. The strict reader and programmatic model validator admit this variant only at v7 or later, reject missing, duplicate, unknown or extraneous members, and recurse into the operand for claim-path and subject validation. Existing condition encodings do not acquire a flag or new member.

Evaluation negates the portable operand's Boolean result. It does not add authentication implicitly: `not role "Service"` can allow an unauthenticated caller without that role, so a person-only rule writes `authenticated and not role "Service"`. A missing claim has no match, so its negation is true. Existing role/claim comparison rules, short-circuit order, absent-caller refusal and request validation are unchanged.

Negation does not introduce references to named policies inside a `require` condition, nor extend `authorize` to accept `not`. Under [0005](0005-policy-predicates-as-an-implementation-attachment-role.md), an opaque predicate is a whole policy whose result the reference runner cannot know. It is not a portable condition operand. Nesting an opaque predicate under negation (or any logical condition) remains an invalid ESM contract; source attempts to put a policy name or implementation under `not` fail parsing with the existing policy-condition diagnostic. The compiler never treats an opaque/unsupported result as false and then turns it into permission. Composition of named policies retains 0005's authored-order three-outcome semantics.

## Options considered

- **Byte-preserving v7 extension — chosen.** Negation was previously unbindable and adds no bytes to existing models. This follows 0004's rule, as code validation joined v3.
- **Separate ESM version.** Gives consumers a separate version-admission switch, but consumes a version for an additive condition with deterministic semantics. Not chosen by Sindre Alstad Wilting.
- **Language-only syntax.** Parsing negation while refusing semantic binding cannot deliver reference evaluation or equivalent backend authorization. Not chosen.
- **Negate opaque predicates or named policy references.** Would expand the authorization grammar and require a three-outcome unary contract. Not needed for role/claim exclusion; opaque predicates remain whole-policy attachments under 0005.

## Default if unanswered

Without admission, negation stays unbindable and modelers must issue artificial positive claims or leave exclusions outside the model. The accepted decision settles that question; it does not authorize consumer rendering changes outside this repository.

## Timeline and scope

Applies from acceptance through delivery of #431 and until superseded. In scope: policy-condition syntax, printing, binding, canonical representation, strict reading, validation, reference execution, conformance, editors, samples and documentation. Out of scope: negated named-policy authorization, execution of implementation attachments, external consumer implementations, new ESM numbers and unrelated v7 behavior.

## Verification

**Done when:** Negated roles, claims, authenticated conditions and grouped conditions parse, round-trip, bind as v7 and execute with both allowed and denied outcomes. Precedence and repeated negation are pinned. An invoicing specification denies a service caller through `not`. A golden vector and source-backed corpus vector pin the new canonical bytes, revision and outcomes across file/folder forms. Existing v1–v7 golden/corpus vectors remain byte-for-byte unchanged and pass. Strict readers reject malformed negation, negation in pre-v7 models and nested opaque predicates.

**Verify by:** Run parser/printer/binder/reference-runner specs, canonical golden/corpus specs and TypeScript conformance specs, followed by the repository's Tier 1 gates. Inspect the golden/corpus diff for unchanged existing fixtures and the sample denial outcome. A commit implementing this contract carries `Decision: 0004, 0005, 0027`.

## Consequences

Authors can state exclusions directly without extra claims. Pre-extension v7 strict readers reject models containing the new `not` variant; consumers must explicitly implement condition reading, execution and rendering before admitting those models. Models without negation keep their bytes and revisions, including existing v7 models. A target that cannot realize negation must reject it rather than ignore it. The v7 version number alone no longer distinguishes pre-extension and extended condition support, which is the trade-off of 0004's permitted extension rule.

## Status notes

**2026-10-06 — accepted.** Sindre Alstad Wilting explicitly selected the byte-preserving v7 extension and unary precedence in the implementation request. Implementation and verification remain pending.
