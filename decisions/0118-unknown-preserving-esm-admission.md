---
id: 0118
title: Opt-in unknown-preserving ESM admission and minor allocation
status: accepted
stage: none
class: contract
reversibility: reversible
decided: 2026-10-26
decider: L13 (owner orchestration)
amends: 0004, 0025, 0066
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/interoperability.md
---

## Context

Screenplay#1067 adds the first public unknown-preserving read surface. Stage needs a
validated known projection, original bytes and revision, and owner-addressable
unknown diagnostics. This is opt-in admission, not execution authority or trust in
a producer. The existing strict reader, writer and published bytes remain unchanged.
Acceptance is effective at merge under L13's delegated owner orchestration.

## Decision

### D1: Frozen baseline and canonical reconstruction

For each supported major M, freeze the complete canonical descriptor C(M.0) from
the published writer, including optional fields, variants and array rules. Baseline
fields retain their schema order. Post-baseline members are an ordinal extension
region after baseline fields forever within M, even after semantic promotion.
Canonical classification is independent of semantic reader knowledge. Reconstruct
the whole document and require exact byte equality, and verify the full `rev1:`
over canonical bytes without the envelope revision. Hashing input alone is insufficient.

### D2: Allocation, not reading

Only additive, ignorable members on the latest published major can receive a minor,
numbered at admission under 0025. After (M+1).0 publishes, allocate no new M.n.
Already allocated pairs remain registered, frozen and readable forever. A new
polymorphic discriminator value, required member or changed meaning requires a major.
No real minor is allocated by this decision. Phase 2 needs producer implementation,
typed-promotion conformance and consumer admission evidence before publication.

### D8: Caller-supplied reading range

The caller supplies one major and inclusive minimum/maximum minor bounds. Both wire
versions must lie in that range and the package must ship C(M.0). Unknowns are allowed
only above that major's highest registered minor; exact registered pairs admit none.
The latest-major allocation restriction does not restrict reading older admitted
majors. Cross-major ranges and reader-global latest-major admission are rejected.
The validated projection uses the highest registered pair of M; consumers must classify
admission from the document's preserved wire versions, not the projection's versions.

### D3: Structural security and whole-document limits

Unknowns inherit security relevance only through known structural positions:
authorization, policies, callers and claims; identity mapping; compliance, personal
and secret data; event-stream routing. Other unknowns are Unclassified and not
security flagged. Names, substrings and the mere presence of an identity confer no
security meaning. Never discover semantics inside an opaque unknown subtree.
Owner attribution uses the nearest known declaration, with feature/module/application
fallback. Unknown references remain opaque; known references must validate without
pruning declarations or references. Consumers decide degradation and owner disabling.

| Limit | Value |
| --- | --- |
| L1 container depth, including opaque subtrees | 64 |
| L2 document size, before parsing | 8,388,608 bytes |
| L3 unknown extension boundaries per known owner | 64 |
| L4 unknown extension boundaries per document | 1,024 |
| L5 unknown member names, including nested opaque names | ASCII `[a-z][A-Za-z0-9]*`, at most 64 characters |
| L6 sum of captured unknown member value bytes | 1,048,576 bytes |
| L7 placement and encoding | Extension region only; no unknown discriminator values; canonical UTF-8/NFC, numbers and strings, no duplicates |

Refuse the whole document on breach; never truncate. These limits apply only to the
new reader. Before Phase 2 a limit change updates this record. After Phase 2 raising
a limit is compatible; lowering one needs a new decision.

### Exception to 0066: conformance-only reserved pairs

A pair whose minor is `4294967295` (`uint.MaxValue`) is not an ESM version. It is
never registered in `Versions.cs` or any `EsmSchemaV*Support`, never a version-table
row, refused by the strict reader, and never allocated under D2. Its vector **bytes**
freeze on first publication as conformance fixtures: hash-pinned, never edited;
new cases use new files. 0066 freezes those bytes but not a version contract.
This exception is also carried by the dated 0066 note, the interoperability table
footnote, `EsmConformanceVectors.ReservedMinor` XML documentation and reserved-minor
and conformance specs. Synthetic vectors use the latest major and a separate resource
prefix/API, never `EsmGoldenVectors.Versions` or `Get`.

### D5–D7: Additive public surface

Ship the opt-in reader and diagnostics as a public, non-experimental API with a minor
package release. Keep StrictSemanticDocument internal and strict, with no leniency
flag. Published .0 goldens and their APIs stay unchanged. Existing strict admission
continues to refuse unregistered versions and every unknown member.

## D1 addendum: opaque-subtree canonical grammar (accepted by L13, 2026-10-26; goes verbatim into the decision record)
Correction (G1 rationale): the existing canonical writer does NOT order object members by name. At origin/main fdab783b6, SemanticModelCanonicalJson writes members in a fixed schema order (`id`, `name`, `concepts`, `types`, `modules`, …), and it sorts only array ELEMENTS by ordinal Id/Name. So G1 is a new rule, not one we inherit. State it as follows:
- G1 (accepted): inside an opaque extension subtree, object members are ordinal by name at every depth. Noncanonical order is refused at reconstruction.
- G1-P (new producer obligation, part of D2): any construct allocated in a minor must have its typed writer emit object members ordinal by name at every depth inside its extension member. That way promotion (a later reader typing the member) is byte-identical. Phase 1 adds a spec showing that a reserved-minor vector re-emitted by the opaque writer equals the input bytes. Phase 2 must add the typed-promotion byte-equality spec.

Clarifications:
- G2 (accepted): array order is preserved as received; the reader never sorts. Deterministic element order (e.g. ordinal Id/Name, as the existing writer does) is the PRODUCER's obligation under D2. Promotion must not re-sort.
- G3 (accepted): absent and null round-trip as received. Producer omission conventions (e.g. empty collections omitted, as in `if (!IsDefaultOrEmpty)`) are the producer's rule, not normalized by the reader.
- G4 (accepted): the existing CanonicalJson string and decimal encoding only, via the existing NFC and fixed ASCII escaping.
- L5 (accepted): it applies to nested opaque property names as well as to the extension-boundary names.

## Alternatives and verification

Reader-relative known-first ordering is invalid: promotion of `z` before later `a`
changes bytes. Universal security flags wrongly disable ordinary additions. A major-only
policy cannot support additive degraded rendering. Shipping synthetic 34.1 would
publish an unintended contract; keeping vectors private makes consumers copy and drift.

Done when: all 34 published goldens and strict refusals are unchanged; in-range
reserved-minor documents preserve full bytes/revision and owner diagnostics; every
limit and canonical/hash/major failure refuses whole; opaque-writer re-emission is
byte-identical; the complete Tier 1 gate passes. Phase 2 and typed-promotion evidence
remain separate work. Read, execute, render and reverse-recover admission stay separate.
