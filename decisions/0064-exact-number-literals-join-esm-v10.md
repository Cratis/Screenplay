---
id: 0064
title: Join the claimed unreleased ESM v10 with exact Double-mode number literal lowering
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/interoperability.md
---

## Context

In Double numeric mode, the parser holds `9007199254740991` exactly, but the executable-model binder lowers it to `9007199254740990`. `Convert.ToDecimal(double)` keeps only 15 significant digits. The negative bound, other 16-digit integers and sufficiently precise fractions also change. [Screenplay#639](https://github.com/Cratis/Screenplay/issues/639) tracks the defect, found while rendering whole numbers as `long` in [Stage#263](https://github.com/Cratis/Stage/issues/263).

[0036](0036-admit-event-sources-streams-and-command-routes.md), item k, already requires lossless route-bearing integers and command inputs that feed routes, within the Double bound of ±(2^53−1). The released route path supplies them exactly; ordinary payloads, validation operands and specification values still round. Correcting their lowering in place changes canonical bytes, revisions and outcomes of models that previously bound. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) permits a defect in a released version to be corrected only by a new version or a byte-preserving extension.

ESM v9 was released in Screenplay 4.117.0. [0043](0043-reaction-identity-runs-as.md) claims the single unreleased ESM v10, joined by production routes and observer filters under [0063](0063-admit-production-routes-and-observer-filters-as-esm-v10.md). Its bytes have not been released or frozen.

## Decision

Lowering is a pure function of a Double-mode literal: compute the legacy value with `Convert.ToDecimal(double, InvariantCulture)` and parse the invariant shortest round-tripping representation (`"R"`) into a decimal. Use that decimal only when `Convert.ToDecimal((double)decimalValue, InvariantCulture) != decimalValue`; otherwise use the legacy value. The shared predicate returns true if that conversion overflows. This uses neither the double's full binary expansion nor digits the decimal cannot retain. For example, decimal scale rounds `0.00000000000000012345678901235` to a value the predicate cannot distinguish from legacy, so it uses the legacy `0.0000000000000001234567890123` and does not select v10.

Selection is a property of the retained bound model, not of binding calls. The binder and activation-by-use validator share the same value walk and predicate: a retained non-route number satisfying the predicate selects language and semantic version 10.0 and schemaVersion 10. Unused examples, overridden example values and other values bound only for validation do not count. A source that compiled at an earlier version and retains such a literal compiles at v10 with this compiler; the earlier bytes remain readable but are no longer produced from that source. Every literal in a model selecting v10, for this or another selecting feature, receives the pure lowering above. Models selecting v9 or earlier retain legacy values, canonical bytes, revisions and outcomes. Already-lossless route literals and route-feeding inputs do not select v10 merely because the ordinary legacy conversion would round them; their existing values and route formatting stay unchanged.

At v10, the activation-by-use check counts a retained non-route number satisfying the same predicate, even without another v10 feature. Already-lossless route literals and route-feeding inputs do not count as exact-number activation. Released-version readers keep accepting any decimal in an otherwise valid programmatic v1–v9 model: those models need not have come from the binder, so detection by value never refuses a released-version model. Released golden builders and bytes remain unchanged.

This changes lowering of values parsed in Double mode, not the source numeric mode. It neither admits `numbers exact` nor introduces a `numericMode` member. Exact source mode and values beyond its current Double integer bounds remain the separate #285 contract.

## Options considered

- **An in-place fix at released versions:** rejected by 0025 because previously bound models would change bytes and outcomes. This is not a byte-preserving extension.
- **Allocate v11:** rejected because 0025 permits only one unreleased entry, and v10 is already claimed with no released bytes. The correction joins that contract.
- **Wait for exact source mode (#285):** not taken. It addresses source precision and values beyond 2^53 through an explicit mode, not incorrect lowering of already-representable Double values. It remains a separate admission decision.
- **Keep the existing lowering:** preserves released behavior, but continues losing authored digits in admitted ordinary literals and makes Stage render the wrong value supplied by the model.

## Default if unanswered

The proposed correction does not merge. Released lowering remains unchanged, and #639 and Stage#263 retain the ordinary-literal precision defect. v10 stays claimed and unreleased for its already-admitted features; consumers that have not admitted v10 continue to refuse it.

## Timeline and scope

Requires human acceptance before merge and a recheck that v10 remains the only claimed, unreleased version at that checkpoint. Holds until superseded. Covers Double-mode executable literal lowering and feature-based v10 selection, with source-backed and shared golden evidence. Does not change parsing, the Double integer bound, route formatting, exact source mode, or any consumer's read, execute, render or reverse-recover admission. Stage must explicitly admit the corrected v10 contract before rendering these models.

## Verification

**Done when:** ordinary Double literals `9007199254740991`, `-9007199254740991`, `1234567890123456` and a 17-digit fraction bind exactly, select v10 and survive reference execution and strict serialization. A model using only `0.5` and `42` keeps its previous version. Unused and overridden `9007199254740991` example values do not promote the model; a retained route-feeding example value stays lossless at its previous version. The scale-rounded minimum `0.00000000000000012345678901235` binds at its previous version with the legacy value. Already-lossless routes and route inputs retain their released versions and values. A v10 model with only an exact non-route number validates, while a v10 model with no v10 feature is refused. A programmatic v9 model carrying the same decimal still validates at v9. Released v1 through v9 goldens and corpus bytes, revisions and outcomes remain unchanged.

**Verify by:** `for_SemanticModelBinder/when_binding_number_literals`, `when_binding_event_routes`, `for_ExecutableSemanticModel/when_validating_reaction_identity`, `NumberLiteralsCorpus.ExactLiteralsV10`, the extended `full-esm-v10.json`, existing golden and corpus compatibility specifications, and CI-equivalent local gates.

## Consequences

Consumers can explicitly admit the corrected lowering through v10, while released contracts remain immutable. Retaining a precise ordinary literal that satisfies the shared predicate can now advance a model to v10 even without another v10 feature; validation-only binding cannot. Route-bearing values alone keep their existing admission. This correction does not promise arbitrary-precision arithmetic or exact-mode execution.

## Related

Builds on 0004, 0025, 0036, 0043 and 0063. Related work: Screenplay#639, Screenplay#285 and Stage#263.
