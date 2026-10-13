---
id: 0071
title: Whitespace-only text does not satisfy `not empty`
status: accepted
stage: implemented
decided: 2026-10-13
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Documentation/screenplay/**
---

## Context

[#680](https://github.com/Cratis/Screenplay/issues/680) asks whether whitespace-only text satisfies `not empty`. The language says the rule's meaning is fixed so every target agrees ([commands.md, What the executable model admits](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/commands.md#what-the-executable-model-admits)), but it never says what empty text is. The targets disagree:

| Where | `"   "` | Evidence |
| --- | --- | --- |
| Screenplay reference evaluator | passes | `SemanticEvaluator.IsEmpty` uses `string.IsNullOrEmpty` (`Semantics/Execution/SemanticEvaluator.cs:103-110`); `SemanticValidationRules.cs:49` |
| Stage, semantic renderer | passes | `!String.IsNullOrEmpty(value)` (`Rendering.Cratis/Semantics/SemanticValidationRendering.cs:104`) |
| Stage, legacy renderer and Stage's own command validator | fails | FluentValidation `.NotEmpty()` (`Validation/ValidationRuleRenderer.cs:36`, `Stage/Validation/StageCommandValidator.cs:51-52`) |
| Arc .NET backend | fails | FluentValidation `NotEmpty()`, which rejects null, empty and whitespace-only strings |
| Arc frontend (`@cratis/arc`) | fails | `value.trim() === ''` (`Arc/Source/JavaScript/Arc/validation/rules/NotEmptyRule.ts:20-31`) |
| Arc.TypeScript backend | fails | `!value.trim()` (`Arc.TypeScript/Source/Core/validation/evaluateRule.ts:7-9`) |

One ESM rule kind, `notEmpty`, serves concept rules and command property rules alike (`SemanticBehaviors.cs:26`, `SemanticModelBinder.Validations.cs:39-41`), so the disagreement is not between concepts and commands in the model. It comes from the renderer path a target takes. Studio exports its *Required* concept rule as `not empty` (Cratis/StudioIssues#608), so required text concepts accept whitespace in the reference evaluator and in Stage's semantic renderer, but not in Arc.

Event-source route parts are a different matter: `event-sources.md` says "whitespace is not empty" for stream-id parts, matching Chronicle, where only the exact empty string is the unspecified `EventSourceId` (`Chronicle/Source/Clients/DotNET/Events/EventSourceId.cs:19,32`). That is identity text, not a validation rule.

Under [0001](0001-chronicle-runtime-semantic-authority.md) the runtime defines the meaning, and for validation the runtime is Arc.

## Decision

1. **`not empty` on text requires at least one character that is not whitespace.** Null, the empty string and whitespace-only text are empty. `not empty` on a collection keeps its meaning: at least one element. Numbers, booleans and composite values are never empty, as today.
2. **Whitespace is the Unicode `White_Space` property**: U+0009 to U+000D, U+0020, U+0085, U+00A0, U+1680, U+2000 to U+200A, U+2028, U+2029, U+202F, U+205F and U+3000. This is exactly .NET `char.IsWhiteSpace`, which FluentValidation's `NotEmpty()` uses. JavaScript `String.prototype.trim` differs: it treats U+FEFF as whitespace and does not treat U+0085 as whitespace. TypeScript targets therefore test against this set, not `trim()`.
3. **One meaning for concepts and commands.** Every target renders concept rules and command property rules with the meaning above.
4. **Identity text is unchanged.** Event-source and stream-id parts keep Chronicle's rule: only the empty string is empty, and whitespace is a value.
5. **Applies to every ESM version.** The canonical bytes do not change; the reference outcome changes for whitespace-only text, which is what the runtime already does. This follows the precedent of the `$` fix in `matches` ([0054](0054-concept-generation-strategies-and-freshness.md) item 11, #617), which also corrected a reference outcome across versions to match the runtime. The change ships with a vector.

## Options considered

- **Whitespace is not empty (chosen).** It matches the Arc backend and both Arc TypeScript stacks, so a rendered application keeps its runtime behavior. It matches what *required* means to a modeler and to Studio's *Required* rule.
- **Whitespace is a value; only null and empty text are empty.** It matches the evaluator, Stage's semantic renderer and Chronicle identity text, but every Arc stack rejects whitespace. Targets would have to render a custom check instead of the framework rule, and required text fields would accept blank input.
- **Separate meanings for concepts and commands.** Not taken: the model has one rule kind, and the same value would be valid in one place and invalid in another.
- **Use each platform's trim.** Not taken: .NET and JavaScript disagree on U+0085 and U+FEFF, and the language promises one meaning.
- **Apply only from the next ESM version.** Not taken: the bytes do not change, the runtime already rejects whitespace in every version, and keeping the divergence in older versions would keep specifications that pass in the evaluator and fail in the rendered application.

## Default if unanswered

The evaluator and Stage's semantic renderer keep accepting whitespace while Arc rejects it. A specification that passes in the reference evaluator can fail against the rendered application, and Studio's required text concepts accept blank values.

## Timeline and scope

Proposed on 2026-10-12, for acceptance by the decider. Implementation follows acceptance.

In scope: the meaning of `not empty` on text and collections, the whitespace set, the reference evaluator, documentation (`commands.md`, `concepts.md`, the Monaco hover text) and vectors.

Out of scope: identity text (event-source and stream-id parts), `min length`, `matches` and other rules, and trimming or normalizing stored values.

## Verification

**Done when:** the reference evaluator rejects whitespace-only text, including U+0085, U+00A0 and U+3000, for a concept rule and a command rule; it accepts text with any non-whitespace character, including a lone U+FEFF; and the docs state the rule.

**Verify by:** evaluator specifications per case, a canonical corpus vector for whitespace-only text on a concept and a command, and the documentation.

## Consequences

Required text means non-blank text everywhere. The reference evaluator and Stage's semantic renderer change. Arc.TypeScript's rule, and Arc's frontend rule, need a fixed whitespace set instead of `trim()` to match on U+0085 and U+FEFF. Models whose specifications relied on whitespace being a value get a different outcome.

Consumer follow-ups: Stage's semantic validation rendering ([Stage#344](https://github.com/Cratis/Stage/issues/344)); Arc's frontend and Arc.TypeScript `NotEmptyRule` ([Arc#3224](https://github.com/Cratis/Arc/issues/3224)).

## Status notes

**2026-10-13 — accepted.** Accepted by Sindre Alstad Wilting (delegated through the coordinator after an independent review), as drafted.

**2026-10-13 — implemented (#680).** The reference evaluator uses .NET's Unicode whitespace check for text `not empty` in every ESM version, with concept and command property specifications and the `NotEmptyWhitespace` corpus vector. Collection rules and identity-text code paths are unchanged. Command/concept documentation and the shared Monaco/VS Code hover and completion descriptions state the contract. Canonical serialization is unchanged.

## Related

Screenplay [#680](https://github.com/Cratis/Screenplay/issues/680). Stage [#344](https://github.com/Cratis/Stage/issues/344). Studio [StudioIssues#608](https://github.com/Cratis/StudioIssues/issues/608). Builds on 0001; precedent 0054 item 11.
