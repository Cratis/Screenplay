---
id: 0034
title: Treat sensitive values as operational secrets, not personal data
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/IdentifierComplianceValidator.cs
  - Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/concepts.md
  - Documentation/screenplay/diagnostics.md
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

The meaning of `@sensitive` was unresolved in [Screenplay #384](https://github.com/Cratis/Screenplay/issues/384). Earlier discussion compared `[NotAudited]` with classification only, when Chronicle had no other protection for values that are sensitive without being personal data.

Chronicle 19.32.0 adds `[Encrypted]` in `Cratis.Chronicle.ProtectedValues`. Its security documentation describes this category as sensitive without being personal data and warns that using `[PII]` for a secret wrongly enrolls it in right-to-erasure. `[NotAudited]` alone leaves event values in plaintext. `[Encrypted]` alone still lets command inputs reach the causation chain: Arc withholds only `[NotAudited]` and compliance-managed values.

Sindre Alstad Wilting settled the meaning and mapping in [his ruling](https://github.com/Cratis/Screenplay/issues/384#issuecomment-6052500379). This record preserves that ruling, not a new compliance policy.

## Decision

`@sensitive` marks an operational secret, not personal data: encrypted at rest without erasure and withheld from the causation chain. Examples are an API key, a bank account number or a token. `@pii` is the personal-data marker and enrolls a value in erasure. C# providers render the attributes on the concept as follows:

| Screenplay | C# provider mapping |
| --- | --- |
| `@sensitive` | `[Encrypted]` + `[NotAudited]` |
| `@pii` | `[PII]` (unchanged) |
| `@pii @sensitive` | `[PII]` only |

`[PII]` already keeps the value off the causation chain. Chronicle rejects `[PII]` combined with `[Encrypted]` (`CHR0053`), so the combined Screenplay attributes never render both.

A concept marked `@pii` or `@sensitive` cannot be an identity concept. Chronicle rejects `[PII]` (`CHR0034`) and `[Encrypted]` (`CHR0052`) on event source ids. Screenplay reports `PLAY0515` for either marker on a command identifier, an explicit `for` destination or an event source identifier. Use a surrogate identity and keep the protected value as a property.

Encryption uses `[Encrypted]`'s default scope. Scope syntax can come later if a model needs one.

## Options considered

- **`[Encrypted]` + `[NotAudited]` for `@sensitive` (chosen).** Both are needed to protect event values at rest and withhold command inputs from causation.
- **`[NotAudited]` alone.** Leaves the value in plaintext on events.
- **`[Encrypted]` alone.** Does not keep command inputs out of Arc's causation chain.
- **Classification only.** Does not provide either protection.
- **`[PII]` for operational secrets.** Wrongly enrolls non-personal secrets in right-to-erasure.
- **`[PII]` + `[Encrypted]` for combined markers.** Chronicle rejects the combination with `CHR0053`; `[PII]` alone already withholds causation values.

## Default if unanswered

The meaning stays unresolved. Models cannot rely on `@sensitive` for encryption or causation withholding, and Screenplay permits sensitive identities that Chronicle cannot encrypt.

## Timeline and scope

The ruling holds until superseded. Stage already implements the C# mapping and refuses both classifications on identities in [Stage #197](https://github.com/Cratis/Stage/issues/197), released in Stage v4.29.0.

In scope: the meaning of `@sensitive`, the C# provider table, `PLAY0515` for both classifications on identities, and matching concept documentation and editor wording.

Out of scope: encryption-scope syntax and executable-semantic-model admission. Compliance attributes remain refused at binding with `PLAY0268`.

## Verification

**Done when** both compilers reject `@sensitive` identities through the same paths as `@pii` with `PLAY0515`, name the present attribute and the surrogate-identity remedy, accept combined markers on ordinary values, and the documentation and hover state the ruling and C# mapping.

**Verify by** C# and TypeScript identifier and destination specifications, shared diagnostic conformance vectors, Monaco and VS Code diagnostic specifications, and warning-free sample compilation. Check the C# rendering table against the Stage #197 implementation; Screenplay does not render provider attributes itself.

## Consequences

Operational secrets receive encryption without enrollment in erasure and stay out of causation. Personal values keep their existing `[PII]` mapping, even with both markers. Models using either marker for identity must introduce a surrogate identity. The ruling rules out treating `@sensitive` as classification only or as another spelling for personal data.
