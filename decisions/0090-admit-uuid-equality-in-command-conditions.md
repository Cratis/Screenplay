---
id: 0090
title: Admit Uuid equality in command conditions
status: accepted
stage: none
decided: 2026-10-16
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/Compiler/Conformance/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Command conditions admit `==` and `!=` only for numbers, text, enumerations and booleans, and (since 0087) Date and
DateTime. A comparison between Uuid values, or concepts and identities over Uuid, is refused with PLAY0268. No decision
refuses it; it was never admitted.

The shape matters for authorization-style rules over protected command reads (0085): an identity read from state is
compared with an identity on the command, for example `require PlantAccessGrant.memberId == requestedBy`. KeldaKraft has
14 to 16 such conditions. They surface as PLAY0268 once the model declares `command reads protected`.

Uuid values already have one canonical spelling in the executable model: lowercase, hyphenated, 36 characters (the .NET
`D` format). Command inputs, generated values, given facts and read models, projected state, and facts produced by
captures and reactions are all validated against that spelling before any condition runs. So ordinal comparison of
the spelling is exact equality of the value.

## Decision

1. **Where, and with which operands.** A new ESM version admits `==` and `!=` with Uuid operands in these positions only:

   | Position | Left operand | Right operand |
   | --- | --- | --- |
   | `require` | a command property or a protected read path | a command property, a protected read path or a literal |
   | `produces when` | a command property | a command property or a literal |

   - An operand is a Uuid operand when its type is the Uuid primitive, or a concept or identity whose primitive is Uuid.
   - Both sides have the Uuid primitive. When both sides are concepts or identities, they must be the same concept: an
     identity of one kind compared with an identity of another is almost always a modeling error, and in an
     authorization rule `!=` across unrelated identities is always true, so the rule would pass silently. A bare Uuid
     on either side may be compared with any Uuid concept. A literal takes the type of the other side.
   - Both sides are required scalars. Optional and collection operands stay refused, as for every other primitive. For
     a read path, the read-model property behind it must be required. When the protected read finds no instance, 0085
     applies unchanged.
   - Still refused with PLAY0268: ordering (`>`, `>=`, `<`, `<=`), Uuid against text in either direction, `$context`
     operands, reaction `where`, Uuid in command and property validation rules, and Uuid in concept rules (0072: concept
     rules also run on query arguments and in client validators).
   - A comparison refused as a whole reports only PLAY0268; its literals are not checked.
   - Commands invoked by a reaction evaluate these conditions the same way.
2. **Meaning.** Ordinal equality of the canonical spelling. Because every value reaching a condition is canonical, this
   is value equality. Equality everywhere else (specification `then` matching, read-model keys, constraints, freshness)
   is unchanged.
3. **Literals.** A literal operand is written in canonical spelling. Any other spelling (uppercase, braces, no hyphens)
   is refused at bind time with PLAY0273. No new value variant or member: a literal is a text value. Validators at the new
   version or later type it from the other operand and check it as a canonical Uuid, so a hand-edited non-canonical
   literal fails validation too. Earlier readers keep rejecting the shape, because their validator refuses a Uuid
   comparison.
4. **Selection and number.** The version is selected only when a model uses an admitted Uuid comparison; every other
   model keeps its bytes and version. The working number is ESM v28, allocated by the version-table owner on top of
   v27 (composite read-model keys, #599); under 0025 and 0066 it is confirmed at the admission checkpoint against
   current main.
5. **Renderers.** Targets admit the version explicitly and compare either the canonical spelling or the parsed values;
   for canonical values the two are the same. This record adds no input rule: the executable model already requires
   canonical Uuid values everywhere, and how a target binds a non-canonical input it receives (rejecting it, or
   normalizing it to the canonical spelling before any condition runs) is unchanged by this record. Client-side
   validators do not evaluate command conditions, so nothing changes for them.

## Options considered

- **Case-insensitive or parsed Guid comparison.** Equivalent for canonical values; it only matters for non-canonical
  input, which is already rejected. Parsing would make targets accept spellings the evaluator refuses.
- **Allow any two concepts over Uuid, as text concepts are today.** Simpler, but it lets an authorization rule compare
  unrelated identities and pass silently. Every consumer site (KeldaKraft) compares the same concept, and widening
  later is additive while narrowing would break bound models.
- **Admit Uuid validation rules too.** No consumer needs them, and they reach query arguments and client validators.
- **Wait for #805.** Unrelated: #805 widens which read shapes protected reads may use; this widens the operand type.

## Default if unanswered

KeldaKraft's access-check conditions stay refused and its model cannot bind.

## Verification

- Binder specs: `==` and `!=` on Uuid primitive, concept and identity operands, command property and read path on
  either side, bare Uuid against a concept, canonical literal; two different Uuid concepts, ordering, reaction `where`,
  optional operands (including an optional read-model property behind a read path), collections, Uuid against text in
  both directions and `$context` stay refused with PLAY0268; non-canonical literals refused with PLAY0273; Uuid
  validation rules stay refused.
- Validator specs: v28 accepts; earlier versions reject the shape and the v28 golden file; a v28 model without a Uuid
  comparison is rejected; a non-canonical constant is rejected.
- Evaluator specs: `require` and `produces when` with equal and unequal values, read path against property; a
  non-canonical input is rejected before any requirement runs.
- Canonical vectors: a golden file and a corpus vector at v28, and controls showing other models keep their bytes.
- Samples: Invoicing gains a Uuid comparison. Monaco keyword documentation, the MCP syntax-schema description and the
  TypeScript compiler conformance documents follow it.

## Related issues

#844, #805, #599 (v27, which v28 builds on); Cratis/Stage and Cratis/Arc rendering issues; decisions 0004, 0025, 0066, 0072, 0085, 0087.
