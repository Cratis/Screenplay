---
id: 0084
title: Define matches patterns as a portable linear dialect and evaluate them without a timeout
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/SemanticMatchPattern.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Validations.cs
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay/Semantics/Generation/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
---

## Context

A `matches` rule is a regular expression written by the model's author. The C# compiler, evaluator and generation
checks run it with .NET's ECMAScript mode on the backtracking engine with a 1-second wall-clock timeout (#800). On a slow
host the timeout turns a valid value into a rejection or a rule into Unproven (PLAY0689), so the same model gives
different verdicts on different machines. Separately, C# and the JavaScript that Arc emits for generated clients
already disagree on what some patterns mean: `\s` (no-break space and other Unicode spaces), `.` (`\r`, U+2028/U+2029),
`\w` (U+0130), `[]`, `\p{…}`, class subtraction and `\A`/`\z`. The TypeScript compiler and Monaco do not check patterns
at all. .NET cannot combine ECMAScript mode with its linear-time engine.

## Decision

1. **Dialect.** A `matches` pattern is written as a JavaScript `RegExp` without flags and is limited to the forms that
   JavaScript and .NET's linear-time engine both run with the same meaning. Characters are UTF-16 code units, as in
   flagless JavaScript. The admitted forms:
   - Literal characters; `.`; `^` and `$`, both anchored to the whole input (`$` means end of input, as documented today).
   - Alternation `|`. Groups `( … )`, `(?: … )` and `(?<name> … )`, where `name` matches `[A-Za-z_][A-Za-z0-9_]*`.
   - Quantifiers `* + ? {n} {n,} {n,m}` and their lazy forms. Outside a class, `{` must start one of these quantifiers.
   - The escapes `\d \D \w \W \s \S \t \n \r \f \v`, `\0` when no digit follows it, `\xHH` and `\uHHHH`.
   - Escaped syntax characters `\^ \$ \\ \. \* \+ \? \( \) \[ \] \{ \} \| \/`, and `\-` inside a class.
   - Character classes `[ … ]` and negated classes `[^ … ]` that hold at least one item. An item is a literal (including
     `{` and `}`), an escape listed above, or a range whose two endpoints are single characters.
2. **Refused constructs.** These are refused by authoring error PLAY0699 (`PatternOutsidePortableMatchDialect`) in both
   compilers. The error points at the construct, names it and suggests an equivalent where one exists:
   - Lookarounds, backreferences (`\1`–`\9`, `\k<…>`), `\b` and `\B`. These are refused because the linear engine cannot
     run them or because the two engines disagree; some of them are still regular.
   - `\p`/`\P`, `\u{…}`, `\cX`, `[\b]`, and identity escapes not listed in item 1.
   - Every `(?` form except `(?:` and `(?<name>`, and every other .NET-only syntax (`\A`, `\z`, `\G`, class
     subtraction, inline options).
   - `[]`, `[^]`, a lone or leading `]`, an outside-class `{` that does not start a quantifier, and a lone `}` outside a class.
   - A range with a class escape as an endpoint (`[\d-a]`).
   - A pattern whose expanded size is over the budget. The expanded size counts the nodes Screenplay's matcher builds:
     each literal, class and anchor counts 1, and each optional, loop or alternative choice point counts 1. A counted
     repeat multiplies its body by its upper bound, or by its lower bound plus one when it has no upper bound, and adds a
     choice point for each optional copy. So `x{1500}` counts 1,500, `a{0,1500}` counts 3,000 and `(a*){1500}` counts
     3,000. The budget is 3,000 nodes, and the matcher's state count is at most a small constant times the expanded size.
     Patterns longer than 1,000 characters are refused too.
   Invalid syntax keeps PLAY0367 and unknown named patterns keep PLAY0366. Both compilers report all three codes.
3. **Evaluation.** One dialect parser, written in C#, ported to TypeScript and held equal by shared conformance
   vectors, turns an admitted pattern into a normalized form:
   - Classes and class escapes are computed as sets of UTF-16 code units, including complements and unions under an
     outer negation, and kept as flat ranges. `\s` is JavaScript's fixed whitespace list; `.` is everything except
     `\n \r \u2028 \u2029`.
   - `^` and `$` anchor to the start and end of the input.
   - Groups do not capture. Captures cannot be observed without backreferences.
   - Search semantics follow `RegExp.test`: an unanchored pattern may match anywhere in the input.
   Screenplay's own matcher evaluates the normalized form. It is a Thompson automaton over the code-unit ranges,
   simulated breadth-first with no backtracking, so each input code unit costs at most the automaton's size. No
   platform regex engine runs a `matches` pattern, so no timeout, engine limit, host setting or target framework can
   change a verdict. The budget in item 2 bounds the automaton's states, including the choice points, not only its character positions. Generation checks (RuleConsistency,
   FiniteGenerationRules) never run a backtracking regex.
4. **Meaning changes are fixes.** Where the translation changes a C# verdict (`\s` with Unicode spaces such as U+00A0,
   including the built-in `email` rule; `.` with `\r`, U+2028 and U+2029; `\w` with U+0130), the new verdict is the one
   JavaScript and the documented ECMAScript dialect always gave. The release notes name each case. This needs no ESM
   version bump under decisions 0004 and 0071: the ESM bytes are unchanged (the authored text is still stored verbatim),
   and no new outcome kind is introduced.
5. **Older documents.** Deserialized ESM is not rejected for a pattern outside the dialect. The evaluator classifies
   such a pattern at plan time as `UnsupportedValidation`, naming the construct, and generation checks return a
   deterministic Unproven ("outside the portable match dialect: …"). Both are existing outcomes. Re-authoring the model
   surfaces PLAY0699.
6. **Corpus.** The released `GeneratedConceptRules/v20` vector keeps its source bytes, and its expected generation
   result becomes the deterministic Unproven of item 5. That is a deliberate, named change. A new vector holds an
   equivalent pattern inside the dialect (an alternation without lookahead) with the original expected result.
7. **Generated clients.** Stage renders the translated .NET pattern (no timeout) for server validation and gives Arc the
   authored text as the JavaScript expression, refuses patterns outside the dialect at admission, and drops its own anchor
   rewriting (Cratis/Stage issue). A pattern inside the dialect can still backtrack badly in a browser engine
   (`(a+)+$`); client validation is advisory and the server check is authoritative. Warning about such patterns is a
   separate issue.

## Options considered

- **Keep full ECMAScript and report a timeout as Unsupported.** Verdicts still differ between runs and C#/JS disagree.
- **Own backtracking interpreter with a step budget.** Large, two engines to maintain, generated apps still use platform
  engines, and browser ReDoS remains.

## Default if unanswered

Verdicts keep depending on host speed, and C# and generated clients keep disagreeing on some patterns.

## Verification

- **Authoring:** both compilers refuse every construct in item 2 with PLAY0699 and its suggestion, and accept every form
  in item 1, through shared conformance vectors.
- **Equivalence for admitted patterns:** the translation's verdicts equal `node` flagless `RegExp.test` on a shared vector
  set. It includes the class algebra cases (`[\s\d]`, `[^\w]`, `[\w\W]`, `[^\w\W]`, `.` versus `[^\n]`), the `\s`/U+00A0
  and built-in `email` cases, `.` with `\r`/U+2028, `\w` with U+0130, class braces, `\/` and `\-`, and group names.
- **Older documents:** the original v20 vector and an ESM document with a lookahead load and give `UnsupportedValidation`
  at plan time and a deterministic Unproven in generation.
- **Engine use:** an architecture spec shows that no `matches` code path builds a platform `Regex`. The verdicts are
  identical on every target framework (net8.0, net9.0, net10.0). Boundary specs show that a pattern at the expanded-size budget evaluates and one node more is refused, and that the built automaton's state count stays within the stated constant times the expanded size, including nested nullable repeats such as `((a*)*)*` under a counted repeat.
- **Generation:** results for admitted patterns are unchanged, except for the cases item 4 names.
- **Browser engines are out of scope here:** client checks stay advisory, and warning about patterns that backtrack is
  issue #806.

## Amendment (2026-10-15)

Item 3 originally ran admitted patterns on .NET's `RegexOptions.NonBacktracking` engine. During implementation it
turned out that .NET 8 builds that engine's automaton with a default limit of 1,000 nodes, about 200 pattern positions,
while .NET 9 and .NET 10 allow 10,000 nodes. An ordinary admitted pattern such as `^.{1,255}$` would therefore be accepted
on one target framework and fail to build on another. Item 3 now names Screenplay's own matcher instead, so a verdict
cannot depend on the target framework or on a host setting. The dialect, the refusals and PLAY0699 are unchanged.
Accepted by the L0 coordinator on behalf of Sindre Alstad Wilting, on these conditions:
- seeded, deterministic differential tests compare verdicts against flagless JavaScript `RegExp` and against .NET 10
  `NonBacktracking`, over generated patterns and inputs across the whole dialect, plus the corpus and documentation
  examples;
- an independent algorithm review of the matcher;
- this section.

Generated applications target .NET 10. Rendered validators may keep using .NET's engine there, provided the differential
tests show the same verdicts as the dialect.

A second amendment, made the same day after the algorithm review, changes what the budget counts. It used to count
character positions only, which let nested nullable repeats such as `((a*)*)…` under a counted repeat build hundreds of
thousands of states within a 1,500-position budget. The budget now counts choice points as well, with the limit at
3,000 nodes, so it bounds the matcher's real size.

## Related

#800, #796; decisions 0004 (outcome kinds), 0054 (generation), 0071; Cratis/Stage (renderer), Cratis/Arc (client validators).
