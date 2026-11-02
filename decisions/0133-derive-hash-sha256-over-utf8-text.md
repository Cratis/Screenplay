---
id: 0133
title: "`derive` hashes with SHA-256 over UTF-8 text, as lowercase hex, for high-entropy secrets and fingerprints"
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-03
decider: woksin (delegated through L0)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Decision [0054](0054-concept-generation-strategies-and-freshness.md) item 10 lets a generated `secret` concept use `bytes`, `nanoid` or `pattern` with at least 128 bits of exact entropy. It admits generated secrets only after `derive` hashing exists, "so the stored value is a hash (#308)", and after compliance-marker admission (`PLAY0268` today). Decision [0023](0023-command-production-model.md) orders `derive` and `provide` and admits only already-admitted portable expression forms at first (line 132), so `hash` needs its own admission through [0004](0004-admission-and-governance-of-portable-executable-semantics.md).

Cratis/Screenplay#308 asks for a one-way hash with a fixed, documented algorithm: events keep the hash of a token and the token is returned once (woksin, comment 6094914073). einari asked for explicit keywords and no C# or TypeScript notation (comment 5951683391). woksin ruled that UUIDv5, slug and hash need separately documented portable semantics (comment 6492042895). This record is that documentation for `hash`. Cratis/Fundamentals#1229 adds the matching `GenerateValue.Sha256` and the shared vectors.

State on `origin/main` at the time of writing:

- Screenplay has no `derive` hash and no function-call syntax. Expressions use keyword prefixes such as `literal …` (`ExpressionParser.cs`).
- Screenplay already uses SHA-256 over UTF-8 as lowercase hex for requirement identity, with `len:`-prefixed framing for multi-part input (`ImplementationRequirementIdentity.cs`).
- Decision [0041](0041-personal-data-secrets-and-processing-purposes.md) defines the `pii` and `secret` markers and their Chronicle mapping. It says nothing about digests.

If nobody chooses, nothing is admitted and generated secrets stay refused.

## Decision

The nine choices below are accepted as written, each with the options considered. They include the cross-provider review amendments, which override any weaker reading.

### D1. Purpose: `hash` serves two jobs only, and a verifier needs server-generated issuance

(a) The stored verifier of a generated high-entropy bearer secret: events and read models keep `hash of token`, and the plaintext is returned once. (b) Deterministic fingerprints and lookup keys from inputs.

SHA-256 is adequate for tokens from a cryptographically secure generator with at least 128 bits of min-entropy, because guessing, not preimage search, sets the security. A fast hash is **not** a password hash. Therefore:

- **A verifier requires server-generated issuance.** The hashed value must be a `generated` concept with at least 128 bits of exact entropy (0054 item 10). A caller-provided value is never accepted as the source of a verifier.
- The refusal in D2 is a guardrail, not proof. A password held in an unmarked `String` cannot be detected, so the rule is a second line behind the issuance requirement.
- Fundamentals documents it plainly: "Not for passwords or other low-entropy input; use a password hashing function for those."

Options considered:

- **Plain SHA-256, generated high-entropy input only.** Chosen.
- **HMAC with a server pepper.** Rejected: it needs key material and rotation that Screenplay models have no construct for, and it adds nothing at 128 bits or more.
- **A slow, salted password hash.** Rejected: it is non-deterministic per salt, so it cannot be a lookup key, and user passwords belong to the identity provider, not to Screenplay models. A model that needs one uses an implementation attachment.

### D2. Refusals and classification propagation

These are binder rules for the `hash of` increment. They are guardrails around D1.

- `hash of x` is an **error** when `x`'s concept is `secret` and is not `generated` with at least 128 bits of exact entropy (a password, PIN or short code). The message names password hashing as out of scope.
- Classification **propagates through `derive`**: a derived value keeps the `secret` or `pii` classification of its source.
- Hashing a classified `pii` or `secret` value into a target concept that does not carry the classification is a binder **error**, not a warning. Dropping `pii` drops the protection and erasure mapping (0041, items 1–4), and a digest of personal data is still personal data (pseudonymised, recomputable). The repair adds the marker to the target.
- The rule is concept-based, so verifying a presented token works: the presented value is an input of the same generated `secret` concept.
- An unknown or unadmitted algorithm is rejected in the model.
- That the raw secret is not stored is the binder rule of #616 phase 4, not this one.

Options considered:

- **Error plus propagation.** Chosen.
- **A warning with a repair.** Rejected in review: a warning lets classified data lose its protection and erasure mapping unnoticed.
- **Refuse all `secret` operands.** Rejected: it would forbid the verifier case that motivates the function.

### D3. Algorithm: SHA-256 (FIPS 180-4), fixed, no parameter

Precedent: Screenplay's own identity encoding. SHA-256 is available as `SHA256.HashData` on net8 to net10, is AOT and trim safe, and exists in Node and on every platform. Rejected: SHA-1 (collisions; acceptable only inside UUIDv5 where the RFC mandates it), SHA-512 (longer output, no gain), BLAKE2 and BLAKE3 (not in the BCL), and an algorithm argument (more surface and vectors, and it invites weak choices).

Algorithm agility comes from versioning: the ESM node carries `"algorithm":"sha256"`, so a future algorithm is a new explicit value under a new ESM version that consumers admit or refuse (0004, 0066). `hash of` keeps meaning SHA-256 forever.

### D4. Input canonicalisation: the operand's text, UTF-8, nothing else

- No Unicode normalization, no case folding, no trimming. No BOM is added or stripped: a literal leading U+FEFF is preserved and hashed as its three UTF-8 bytes.
- Unpaired UTF-16 surrogates are rejected: a typed exception in .NET and a binder or runtime error in Screenplay. The empty string is valid.
- In TypeScript, UTF-16 well-formedness is validated **explicitly before** encoding, because `TextEncoder` and `Buffer` silently replace a lone surrogate with U+FFFD and would hash a different input without error.
- Generated tokens are hashed as their text (the base64url, hex or base32 string), never decoded to bytes first.
- Transport normalization is **unverified**: whether an HTTP stack, form, editor or clipboard normalizes a token before it reaches the hash has not been checked, and the record does not claim it. Tokens should use alphabets that normalization does not touch (ASCII).

This mirrors `UuidV5` (`GenerateValue.NameBased.cs`), so the two deterministic functions share one rule. Verification must hash exactly the token as issued; normalizing would merge distinct inputs. Case-insensitive fingerprints need a separate, future canonicalizing function.

### D5. Admitted operands: exactly one operand of primitive `String`

A string literal, a `String`-typed path, or a concept over `String` (including generated `bytes`, `nanoid` and `pattern` values). Optional or absent operands are refused at binding. Non-string primitives (`Uuid`, numbers, dates, booleans) are refused: their canonical text differs between C# and TypeScript and no portable to-text rule exists. More than one operand is refused. If admitted later it must use length-prefixed framing (`len:value|…`), never plain concatenation (`"ab"+"c"` equals `"a"+"bc"`).

### D6. Output: 64 lowercase hexadecimal characters, a `String`

Precedent: requirement identity and `ValueEncoding.Hex`. It is recognisable (`sha256sum`), URL and file safe, and indexes cleanly. There is no `sha256:` prefix, since the algorithm is in the contract and the ESM node. Rejected: base64url (case-sensitive, less recognisable), raw bytes (Screenplay concepts have no byte primitive) and an encoding argument. Models should target a concept over `String`, for example `concept ApiKeyHash : String`.

Comparing digests: in .NET, compare equal-length digest bytes with `CryptographicOperations.FixedTimeEquals`. Lookup by a digest index is not constant-time and is acceptable only for tokens with at least 128 bits of entropy. No hand-written comparison loop in any language.

### D7. Name and syntax

Screenplay: `derive keyHash = hash of key`. It is keyword-prefixed like `literal …`, following einari's ruling on #308, and `of` composes for later functions. `hash(...)` in #308's prose is the idea, not the spelling. ESM node: `{"kind":"hash","algorithm":"sha256","encoding":"hex","operand":<expression>}`, with fixed `algorithm` and `encoding` so a consumer never infers them. Fundamentals: `GenerateValue.Sha256(string value)` and exception `InvalidSha256Input`. The surface spelling is reversible until the increment merges; D3 to D6 are the costly part.

### D8. Ownership and implementation: Fundamentals owns the function and the vectors; Screenplay copies the vectors

#1229 stays open and is delivered. The runtime that executes `derive` is Arc on .NET, which already depends on `Cratis.Concepts`, and the shared `sha-256.json` keeps Fundamentals and Screenplay's reference evaluator byte-identical, as `uuid-v5.json` does. Screenplay's compiler does not depend on Cratis.Fundamentals and keeps its own implementation.

- **.NET:** the temporary UTF-8 buffer is zeroed in `finally` (`CryptographicOperations.ZeroMemory`, from `ArrayPool` or `stackalloc` for small inputs). Strings cannot be erased, and the documentation says so.
- **TypeScript:** **no hand-written SHA-256.** Use native crypto (`node:crypto` `createHash`, synchronous) where available, or a vetted synchronous library if a browser or synchronous path is needed. The vectors alone do not justify bespoke cryptography. Fundamentals ships no TypeScript in this step.

Rejected: Screenplay-only (Arc would reimplement the contract without vectors) and a new `DeriveValue` class (it splits the deterministic helpers).

### D9. Phasing and admission

- **A (now):** Fundamentals `Sha256`, vectors and documentation. Additive.
- **B (now):** this record. No code and no canonical bytes. It is the accepted record 0004 requires before any version change.
- **C (after #308's first `derive` increment):** `hash of` on every surface under the language-change-surface rule, with a new ESM version (shared with the `derive` increment only if both ship in one release, 0105), explicit consumer admission and tracking issues in Stage, CLI, Studio and Generation (0004), and the vectors copied verbatim.
- **D (#616 phase 4, generated secrets):** additionally needs compliance-marker ESM admission (0054 item 10, `PLAY0268` today).

`uuidv5` and `slug` are separate decisions and are not covered here.

## Default if unanswered

Nothing is admitted. `derive` has no `hash`, and generated secrets stay refused with `PLAY0268`. Authors who need a digest use an implementation attachment.

## Timeline and scope

In scope: the semantics of D1 to D9. This record changes no code, no ESM bytes and no documentation page.

Out of scope: HMAC and password hashing, other algorithms, `uuidv5`, `slug`, case folding and normalizing functions, multi-operand hashing, non-string operands, a `@cratis/fundamentals` TypeScript API, and any ESM version number.

Work follows in the order of D9: A and B now, C after the first `derive` increment, D with #616 phase 4.

## Verification

Done when the `hash of` increment lands with:

- The shared `sha-256.json` copied verbatim from Fundamentals, with a README pin, passing in the reference evaluator and the binder. The vectors include the empty string, `abc`, the FIPS multi-block value, non-ASCII text, a leading U+FEFF, and invalid lone-surrogate inputs.
- A TypeScript check that a lone surrogate is rejected before encoding and never hashed as U+FFFD.
- Binder specifications: a `secret` that is not generated with at least 128 bits is refused; classification propagates through `derive`; a classified source into an unclassified target is an error; optional, non-string and multi-operand inputs are refused; an unknown algorithm is rejected.
- The TypeScript implementation uses native or vetted cryptography and contains no hand-written SHA-256.
- A reader check that `algorithm` and `encoding` are explicit and fixed.

## Consequences

- **Published digests are permanent.** A later change to D3 to D6 breaks stored verifiers. Vectors are only ever added, never edited.
- **Stage, Studio, CLI, Generation:** each admits the new ESM version explicitly and refuses it until then (0004, 0066). Arc emits runtime hashing by calling `GenerateValue.Sha256`.
- **AI corpus:** teaches `hash of`, the issuance requirement and the refusals when increment C starts.
- **A guardrail, not a guarantee:** a low-entropy secret hidden in an unmarked `String` can still be hashed. The issuance requirement and the documentation are the defence.
- Transport normalization of tokens is unverified (D4).

## Related

Decisions 0004, 0023, 0041, 0054, 0066 and 0105. Cratis/Screenplay#308 and #616. Cratis/Fundamentals#1229.
