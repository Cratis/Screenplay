---
id: 0152
title: Policy state conditions test text and collection membership in ESM v45
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-09
decider: Einar Ingebrigtsen (direct request to deliver Cratis/StudioIssues#1387)
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Documentation/screenplay/policies.md
---
## Context
A plan usually grants a set of features. The shared condition grammar has `contains` and `starts with`, but they parsed and refused executable binding, so "the plan includes feature X" could only be one boolean per feature ([Screenplay#1232](https://github.com/Cratis/Screenplay/issues/1232), epic [StudioIssues#1387](https://github.com/Cratis/StudioIssues/issues/1387)).
## Decision
- ESM v45 adds two comparison operators to the executable condition, written `"contains"` and `"startsWith"`. Only a model that uses one selects v45; every earlier model keeps its bytes and revision, and a reader below v45 refuses the operator strings.
- They are admitted in a policy state condition (decision 0151): `plan.features contains "DataQuality"` over a required text collection, `plan.name contains "x"` and `plan.tier starts with "pro"` over a required text property, each against a text constant.
- Text operators are ordinal and case-sensitive. Membership holds when an element equals the operand by value. An absent value never matches, so a missing fact cannot grant access.
- `not` composes with them (`not plan.features contains "X"`), and a state comparison is never unknown.
- Not admitted: collections of anything but text, optional operands, and non-constant right operands. `produces when`, `validate` and reaction `where` keep refusing the operators with the same diagnostic; widening them is a separate, additive step because those contexts also need runtime parity in Arc and Stage.
- One boolean property per feature remains valid and portable; membership is the choice when the feature set is open.
## Consequences
- Stage renders the operators as ordinal `string.Contains`/`StartsWith` and `Contains` on a string collection ([Stage#1048](https://github.com/Cratis/Stage/issues/1048)); Chronicle projections build a text collection through the read model's own mappings.
