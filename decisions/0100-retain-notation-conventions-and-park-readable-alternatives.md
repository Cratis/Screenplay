---
id: 0100
title: Retain Screenplay's notation conventions and park readable alternatives
status: accepted
stage: none
decided: 2026-10-19
decider: Sindre Wilting
class: product
reversibility: cheap
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
  - decisions/**
---

## Context

Issue #350 merged six reviews of notation in Screenplay that looks like programming-language syntax: `Type[]`, the
`=>` arrow, `==` and `!=`, the concept colon, backtick templates, `$` value sources, the `@` escape and a few smaller
symbols. Two acceptance items stayed open: a maintainer decision with cost and migration policy for each, and
documentation of the conventions kept on purpose. Decision 0023 left the audit as open question 2 and said it neither
adds new notation nor silently removes existing spelling.

The language is frozen at ESM v28 (decision 0092): no new constructs, keywords or grammar, and ESM bytes for existing
models do not change. A new spelling is new grammar, so it cannot ship during the freeze whatever its merit.

## Decision

1. **Retain every current spelling.** `Type[]`, the three `=>` meanings, `==`/`!=`, the `concept`/`example` colon,
   backtick templates, the `$` value sources and the sigil-free `case.`/`item.` roots, the `@` escape, the
   `observable?` exception, JSON values, `> < >= <=`, `=`, the portable regex dialect and `//` comments stay as they
   are. They are documented, with the reason for each, in
   [notation.md](../Documentation/screenplay/notation.md).
2. **Backtick guards on a capture `when` are the admitted exception.** Outside inline code (embedded C# and screen code),
   they are the only place `&&`, `||` and `!` are admitted, because the executable model already binds them. This
   departs from #350's position that `&&` and `||` stay rejected everywhere: removing them from capture guards would
   change admitted ESM meaning, which the freeze forbids. Everywhere else conditions use `and` and `or`, and policy
   requirements and `validate` rules also use `not`, with comparison symbols, as #350 asks.
3. **Readable alternatives stay `proposed` (parked) under 0092 §3.** They are listed below and do not become
   decisions until the freeze ends.
4. **Migration policy for any alternative that is later accepted.** The old spelling keeps working. It reports an
   Information diagnostic (not an error, so `--warnaserror` is unaffected) with a repair, on the pattern of `PLAY0479`
   (`String?`) and `PLAY0565` (`@pii`). ESM bytes and versions for existing models do not change. The compilers, the
   MCP server and the editors move together (decision 0092 and the language-change rule). The canonical printer
   switches to the new spelling only when the repair ships.

## Parked alternatives

| Notation | Alternative | Cost | Ambiguity and notes |
| --- | --- | --- | --- |
| `Type[]` (~207 sample lines) | `collection of Type` or `list of Type` | High: appears in every type-bearing position | Low priority. Easy to read now, expensive to migrate. |
| Query `=>` (~364 sample lines for all `=>` uses; ~205 are queries) | `query Q returns T` | High: most-used arrow | Command `returns` already means the response shape; a ruling is needed so one keyword does not carry two meanings. |
| Projection and reducer `=>` (~137 lines) | `builds` | High | Keep one direction, from the builder to the read model, whatever the spelling. |
| Translate `=>` | `"utkast" means draft` or `as draft` | Low: smallest blast radius | Could ship first when the freeze ends. |
| `==`, `!=` | `is`, `is not` | Medium | Must cover conditions, policy requirements, `validate` (`length ==`) and capture guards in one change. Word operators exist already (`contains`, `starts with`, `not contains`). |
| `concept X : String` and `example X : Type` | `concept X based on String`, with a matching `example` form | Medium (~282 sample lines: 253 concepts, 29 examples) | The colon has two users; a proposal must settle both or leave the colon inconsistent. |
| Backtick templates | `"{a} ({b})"` or `template "{a} ({b})"`, reusing the i18n placeholder | Medium | Needs an escape for a literal `{`. Rendered text must stay byte-identical. Backticks also wrap capture guards, which is a second meaning. |
| Capture guard `&&`, `||`, `!` | Let `when` take the shared condition grammar (`and`, `or`, `not`), then deprecate backtick guards | Medium | Changes meaning, not only spelling. Needs its own record, as #350 requires. |
| `$` value sources | Word scopes | Low value | Word scopes collide with identifiers and keywords. Not recommended. |
| `@` escape | Quoted identifiers | Low value | A deliberate tie-breaker. Not recommended. |
| `notify info` against `severity information` | `notify information` alias | Low | Vocabulary only. |
| `matches` (claim equality and pattern match) | A separate word for claim equality | Low | Two meanings for one word. |
| `\|` in `slice statechange\|stateview` | `or` | Low | Minor. |

Redundant spellings that already have an equivalent form (composite-key braces, `authorize A B` against
`authorize A and B`) need no new grammar. Whether to report them with an Information diagnostic and repair is a
separate maintainer ruling; documentation already shows the preferred forms.

## Options considered

- **Retain and document (chosen).** No grammar change, so it fits the freeze, and it answers the open question for the
  freeze period.
- **Reject `&&` and `||` in capture guards too (#350's position).** Rejected: capture guards are admitted ESM
  semantics today, so this would change what existing models mean, which the freeze forbids. Revisit as a separate
  record when the freeze ends (parked row above).
- **Replace spellings now.** Rejected: every one is new grammar, which 0092 forbids, and each would need all
  consumers (Stage, Arc, Studio, the AI skills) to adapt.
- **Reject the alternatives outright.** Rejected: several are reasonable. Parking keeps them for when the freeze ends.

## Default if unanswered

The conventions stay as they are today and the documentation in `notation.md` describes them. Nothing is replaced,
deprecated or removed, and no parked alternative takes effect.

## Timeline and scope

Retention holds for the language freeze (0092). In scope: the `notation.md` reference, the capture-guard
documentation, and this record's migration policy. Out of scope: every new spelling, any change to the admitted
meaning of capture guards, and any diagnostic or repair; each needs its own record once the freeze ends. Adding a
parked row to this table is additive; accepting one supersedes the retention for that row.

## Verification

Done when `Documentation/screenplay/notation.md` is in `toc.yml` and linked from `grammar.md` and `overview.md`,
`captures.md` and `captures/grammar.md` agree with the C# binder on the admitted guard operators, and 0023 points
here for its open question 2.

Verify by running `dotnet test Source/DotNET/Screenplay/Screenplay.csproj --filter
FullyQualifiedName~for_Documentation`, passing, and by reading `SemanticModelBinder.Captures.cs` against the capture
guard text.

## Consequences

- The conventions are documented in one place and the language does not move.
- Capture backtick guards are documented as an exception and keep their ESM meaning.
- When the freeze ends, each parked row needs its own record with a ruling, an ambiguity analysis and the migration
  above.

## Related

- Issue [#350](https://github.com/Cratis/Screenplay/issues/350)
- Decision 0023 (open question 2) and decision 0092
