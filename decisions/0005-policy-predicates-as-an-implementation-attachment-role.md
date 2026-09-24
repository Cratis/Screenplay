---
id: 0005
title: "Policy predicates as an implementation attachment role, composed in authored order"
status: accepted
stage: implemented
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/PolicyParser.cs
  - Source/DotNET/Screenplay/Parsing/CommandParser.cs
  - Source/DotNET/Screenplay/Parsing/QueryParser.cs
  - Source/DotNET/Screenplay/Contexts/PolicyContext.cs
  - Source/DotNET/Screenplay/Semantics/SemanticPolicies.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Policies.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelValidator.Policies.cs
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticPolicyEvaluation.cs
  - Source/DotNET/Screenplay/Semantics/Serialization/**
  - Documentation/screenplay/policies.md
---

## Context

[Decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md) fixed the [#139](https://github.com/Cratis/Screenplay/issues/139) envelope, made reducer transitions its first role and rule predicates its second, and put the policy role out of scope. Today a `policy` with a `csharp` block or a `file` reference fails binding with `PLAY0268`, "portable implementation attachments are deferred to #139" ([`SemanticModelBinder.Policies.cs:30`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Policies.cs)). A model that uses one cannot produce an ESM, so none of its specifications run. The invoicing sample's `IsAdultCustomer` is one such policy.

The change in review on branch `feature/policy-predicate-role` binds these policies. Doing so raises a question the declarative-only evaluator never had to answer: what the reference evaluator reports when an authorization expression mixes portable conditions with a predicate it cannot run.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), the runtime defines the meaning. For authorization that runtime is Stage's rendering onto Arc:

- Stage renders each protected operation's effective authorization as one C# expression. It translates `and` to `&&` and `or` to `||` in authored order ([`SemanticPolicyArtifactRenderer.cs:92-98`](https://github.com/Cratis/Stage/blob/main/Source/Rendering.Cratis/Semantics/Policies/SemanticPolicyArtifactRenderer.cs)), inside `IsAuthorized` (lines 44-50). Today it throws `UnsupportedSemanticRendering` for any condition it does not know (line 107), so it fails closed on an opaque predicate.
- Arc evaluates native policies in declaration order and returns at the first false ([`ArcAuthorizationPolicyRuntime.cs:70-85`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Authorization/ArcAuthorizationPolicyRuntime.cs)). The ASP.NET Core host runs that native resolution first, then ASP.NET Core policies in declaration order, again stopping at the first false ([`AspNetAuthorizationPolicyRuntime.cs:158-179`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc/Authorization/AspNetAuthorizationPolicyRuntime.cs)).
- Neither the rendered expression nor either runtime catches exceptions. When a predicate throws, the request fails. It does not become a deny.

## Decision

Policy predicates are the third attachment role. The reference evaluator composes them in authored order, as the target does.

1. **Binding.** A `policy` with a `csharp` body or a `file` reference binds as an opaque attachment: role `PolicyPredicate` ([`SemanticImplementationRequirement.cs:24`](../Source/DotNET/Screenplay/Semantics/SemanticImplementationRequirement.cs)), capability `pure`, context contract v1 and result contract v1. The result is a `bool`. The context is `PolicyContext` v1, as documented in [`policies.md` "What the code can see"](../Documentation/screenplay/policies.md): `Identity` (`Id`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `Claims`), `Artifact`, `Subject`, `Tenant` and `Occurred` ([`PolicyContext.cs`](../Source/DotNET/Screenplay/Contexts/PolicyContext.cs)). Inline and file forms produce the same requirement identity, and the file form is identified by content, per decision 0002.
2. **One form per policy.** A policy has either a `require` condition or one implementation, never both. Combining `require` with code or a file is a compile error.
3. **ESM version.** The construct joins ESM v3 under the rule [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md) proposes: only models that use an opaque policy select v3, and portable policy entries keep their v1/v2 bytes.
4. **Composition.** The reference evaluator gives each operand one of three outcomes: allow, deny or unsupported. It evaluates left to right with short-circuit:
   - A left operand that decides the operator (deny for `and`, allow for `or`) decides the whole expression.
   - An unsupported left operand makes the whole expression unsupported. The right operand is not consulted, because the target would run the opaque predicate first.
   - Otherwise the right operand's outcome is the result.
5. **Enclosing gates.** The module gate, each enclosing feature gate, and the construct's own gate are ANDed in authored order, outermost first. Repeated `authorize` lines on one construct are ANDed in authored order as well. Module and feature lines already combine this way ([`ScreenplayParser.cs:484-489`](../Source/DotNET/Screenplay/Parsing/ScreenplayParser.cs)). The command and query parsers currently keep only the last line ([`CommandParser.cs:57`](../Source/DotNET/Screenplay/Parsing/CommandParser.cs), [`QueryParser.cs:60`](../Source/DotNET/Screenplay/Parsing/QueryParser.cs)).
6. **Outcome.** An unsupported authorization returns `SemanticUnsupported` with the `Authorization` capability and names the policy. It is never reported as allow or deny, so neither `then denied` nor a success assertion can pass on it.

## Options considered

- **Left-to-right three-valued short-circuit (proposed).** It mirrors what Stage renders and Arc runs. It claims allow or deny only when the target would reach that answer without running the opaque predicate.
- **Order-independent (Kleene) evaluation.** `false and unknown` is false and `true or unknown` is true, whichever side is unknown. Not taken: for `CustomAccess and ManagersOnly` it claims deny, but the target runs `CustomAccess` first and that call could throw. The reference evaluator would then promise an outcome the target does not guarantee. It could be admitted later, but only with a recorded total-result contract (a throw maps to deny) that Stage enforces in the rendered code.
- **Always unsupported when any operand is opaque.** Not taken: it is over-conservative. When a portable left operand decides first, the target never runs the predicate, and the reference outcome is certain.
- **Leave policies unbound.** Not taken: every model with a code policy stays outside the ESM, and the invoicing sample keeps reporting it as unsupported.

## Default if unanswered

Code policies keep failing binding with `PLAY0268`. Models that use them, including the invoicing sample, produce no ESM, and none of their specifications run. If the branch merges without this record, its current order-independent evaluation becomes the de facto contract. That promises outcomes the target cannot guarantee.

## Timeline and scope

Settle before `feature/policy-predicate-role` merges, and keep it until superseded.

In scope: binding a policy predicate (role, capability, `PolicyContext` v1, `bool` result v1), the `require`-plus-implementation compile error, three-valued composition in authored order including enclosing gates and repeated `authorize` lines, and the reference outcome.

Out of scope: persona binding; provider enforcement and rendering of opaque predicates in Stage; a total-result contract for predicates; executing predicate bodies in Screenplay; changes to Arc.

## Verification

**Done when:** A `csharp` or `file` policy binds to an ESM v3 opaque condition whose requirement has role `PolicyPredicate`, capability `pure` and contract versions 1, and both forms share one identity. A policy that combines `require` with code or a file fails compilation. With an authenticated caller who is not a `Manager`, `ManagersOnly and CustomAccess` denies. `AlwaysAllowed or CustomAccess` allows. `CustomAccess and ManagersOnly`, `CustomAccess or AlwaysAllowed`, `AlwaysAllowed and CustomAccess` and `CustomAccess or ManagersOnly` are unsupported. Two `authorize` lines on one command are ANDed in authored order. `policies.md` describes left-to-right composition.

**Verify by:** Specs in the branch: `when_binding_policy_predicates.cs` (composition outcomes, identity across inline and file forms, v3 activation, round trip), `when_binding_an_inline_csharp_policy.cs`, `when_binding_a_policy_file.cs`, `when_reading_malformed_policy_predicates.cs`, `when_listing_code_the_model_needs.cs`, `when_binding_the_invoicing_sample.cs`, and the `full-esm-v3.json` golden vector through `canonical_serialization_golden_vectors.V3.cs`. Add specs for the `require`-plus-implementation error and for repeated `authorize` lines on a command and a query. Check the `Decision: 0005` trailer on the merge.

## Consequences

Models with code policies reach the ESM, and their unrelated specifications run. The reference evaluator never claims more than the target guarantees. As a result, authoring order becomes observable: `CustomAccess and ManagersOnly` and `ManagersOnly and CustomAccess` are equivalent at runtime when the predicate succeeds, but only the second yields a reference outcome. Authors who want a reference verdict put portable conditions first. A future total-result contract could relax this. It would need its own record, because it would change reference outcomes.

## Related issues

Screenplay: [#139](https://github.com/Cratis/Screenplay/issues/139). Decisions: [0001](0001-chronicle-runtime-semantic-authority.md), [0002](0002-implementation-attachments-envelope-and-reducer-role.md), [0004](0004-admission-and-governance-of-portable-executable-semantics.md).

## Status notes

**2026-09-24 — accepted and implemented.** Accepted as written; the decision text is unchanged. The references above to what decision 0004 "proposes" now point at an accepted record. `feature/policy-predicate-role` merged as [#251](https://github.com/Cratis/Screenplay/pull/251) and shipped in v4.29.0, with the specs named under *Verification* in the tree. It is not yet `verified`.
