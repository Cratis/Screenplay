# Concepts

Concepts are formalized value types that wrap a primitive. They give every domain value a precise, strongly-typed name — you never pass a raw `Uuid` or `String` around — and they are where compliance is declared. Bare markers declare the protection a concept needs: `pii` marks personal data (GDPR Art. 4(1)); `secret` marks an operational secret, encrypted at rest without erasure and withheld from the causation chain. C# providers map these to Chronicle and Arc attributes as described below.

Concepts declare `generate <strategy>` for server-created values; command properties declare `generated`. High-entropy strategies select ESM v15, except explicit unmodified `generate uuid` on Uuid, which is identical to the implicit default. See [Generated values](generated-values.md). Rules on generated concepts select ESM V20: Proven rules bind silently, Refuted rules fail with `PLAY0688` and a witness, and Unproven rules warn with `PLAY0689` and a reason. Runtime fixtures must satisfy both shape and rules. Explicit generation with compliance markers remains refused. Finite `pattern`, inclusive Int64 `random`, attached `rule` generation and String-concept `validate checksum luhn|mod11|mod97` select ESM v21. Checksum validation may be inline or nested under `validate`; it checks exact trailing digits on supplied and stored values independently of generation. Pattern dates use a pinned UTC fixture clock when present; see [patterns and checksums](generated-values/patterns.md).

Editors can [check a concept declaration](concept-checks.md) and discover generation options through the public .NET catalog without compiling a whole application. To check an actual value instead, use [value-rule checks](value-rule-checks.md).

A concept names one primitive value. For a shape made of several — the child records events carry — see [Types](types.md).

## Syntax

```screenplay
concept <Name> : <PrimitiveType> [<attributes>]
  [file <path>]
  [<attribute> reason "<text>"]*
  [generate <strategy> [modifiers]]
  [validate ...]*

concept <Name> : Enum
  [file <path>]
  [<attribute> reason "<text>"]*
  (<value> [= <int32>])+
  [validate ...]*
```

The optional `file` line names the repository relative file this declaration is realized by, so a document can be navigated back to the code it describes. It is additive - it never stands in for any part of the declaration. See [File references](file-references.md).

## Primitive types

`Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`

## Attributes

| Attribute | Meaning |
| --- | --- |
| `pii` | Personal data (GDPR Art. 4(1)); renders Chronicle `[PII]`. |
| `personal` | Alias of `pii`, accepted without a diagnostic; canonical printing writes `pii`. |
| `secret` | Operational secret, not personal data: encrypted at rest without erasure and withheld from the causation chain. |

C# providers render these attributes on the concept:

| Screenplay | C# attributes |
| --- | --- |
| `secret` | `[Encrypted]` + `[NotAudited]` |
| `pii` | `[PII]` |
| `pii secret` | `[PII]` only |

`[NotAudited]` alone leaves event values in plaintext; `[Encrypted]` alone does not withhold command inputs from Arc's causation chain. `[PII]` already withholds the value, and Chronicle refuses `[PII]` combined with `[Encrypted]` (`CHR0053`). An omitted scope retains Chronicle's Subject default. Explicit `secret scope subject|namespace|global` records the requested scope; changing it for concepts used by persisted events requires a new event generation. Stage v4.29.0 implements this mapping ([Stage #197](https://github.com/Cratis/Stage/issues/197)).

Compliance markers select executable semantic model (ESM) v13 under [decision 0068](https://github.com/Cratis/Screenplay/blob/main/decisions/0068-admit-concept-compliance-markers-as-esm-v13.md). Consumers must explicitly admit v13 before realizing this protection. The reference evaluator runs marked models with unchanged outcomes; it has no storage, encryption or causation chain.

### When to mark a value

Use `pii` when the value is personal data, such as a person's name or email address. Use `secret` for a credential or operational secret that needs encryption without enrollment in personal-data erasure. If both apply, declare both: the executable model retains both markers, but personal-data protection takes precedence. Do not use compliance to classify an enum or an event-source/stream identity; use a surrogate identity and a separate protected value instead.

## Examples

```screenplay
concept InvoiceId        : Uuid
concept EmailAddress     : String   pii
concept ApiKey           : String   secret
concept NationalIdNumber : String   pii secret
concept DateOfBirth      : Date     pii
```

## Describe the value

A concept accepts one indented `description "…"` or fenced-text description. It prints first, before `file`, classification and validation lines; a one-line concept gains a body. This is report-only authoring metadata (`PLAY0270`), not a compliance reason or an executable rule. In an enum, bare `description` without a child fence stays a value and prints as `@description`. Markdown `documentation` is not supported here.

## Why a value is personal data

The marker classifies the value. A `reason` is a free-text concept note, not a machine-checked lawful basis, purpose or retention policy. Existing notes remain unchanged during migration; the compiler never guesses their legal meaning. Declare [processing purposes](purposes.md), basis and retention separately and reference them on the slices that use the value.

An indented `<attribute> reason "<text>"` line records it:

```screenplay
concept BankAccount : String pii secret
  pii reason "A payout bank account identifies sole-proprietor partners."
  secret reason "Fraud-sensitive - a leaked account number enables direct financial harm, so it never leaves the payout path."
```

Each attribute the concept declares may carry at most one reason, and a reason may only be given for an attribute the concept actually declares — `secret reason "…"` on a concept that is only `pii` is a compile error. A reason is optional throughout: bare `pii` stays valid and prints on one line.

## Scope and personal-data qualifiers

```screenplay
concept PartnerApiKey : String secret
  secret scope namespace
  secret reason "Credential for the partner API"
concept MedicalNote : String personal
  personal special health
concept ConvictionNote : String pii
  pii criminal
```

Scope has the closed values `subject`, `namespace`, `global`. Scope on `pii`, duplicate scope and unknown scope values are errors. Scope on `pii secret` warns: only `[PII]` renders, so the explicit secret scope is ignored.

### Personal-data qualifiers

A `pii` concept can carry two qualifiers that record why the value needs more care than ordinary personal data. `pii special <category>` names one category of [GDPR Art. 9(1)](https://gdpr-info.eu/art-9-gdpr/); `pii criminal` marks data covered by [Art. 10](https://gdpr-info.eu/art-10-gdpr/). The two may appear together on one concept. The official regulation text is [Regulation (EU) 2016/679 on EUR-Lex](https://eur-lex.europa.eu/eli/reg/2016/679/oj).

| Keyword | GDPR category | What it covers | Example value |
| --- | --- | --- | --- |
| `pii special racialOrEthnicOrigin` | Art. 9(1) | Data revealing a person's racial or ethnic origin. | A self-declared ethnicity |
| `pii special politicalOpinions` | Art. 9(1) | Data revealing a person's political opinions. | A party membership note |
| `pii special religiousOrPhilosophicalBeliefs` | Art. 9(1) | Data revealing a person's religious or philosophical beliefs. | A religion field on a profile |
| `pii special tradeUnionMembership` | Art. 9(1) | Data revealing whether a person belongs to a trade union. | A union membership number |
| `pii special genetic` | Art. 9(1), [Art. 4(13)](https://gdpr-info.eu/art-4-gdpr/) | Data about inherited or acquired genetic characteristics that gives unique information about a person's physiology or health, typically from a biological sample. | A DNA test result |
| `pii special biometric` | Art. 9(1), [Art. 4(14)](https://gdpr-info.eu/art-4-gdpr/) | Data from technical processing of a person's physical, physiological or behavioral characteristics, special only when processed to uniquely identify that person. | A fingerprint template used to sign in |
| `pii special health` | Art. 9(1), [Art. 4(15)](https://gdpr-info.eu/art-4-gdpr/) | Data about a person's physical or mental health, including health care services, that reveals information about their health status. | A diagnosis in a medical note |
| `pii special sexLifeOrSexualOrientation` | Art. 9(1) | Data concerning a person's sex life or sexual orientation. | A stated sexual orientation |
| `pii criminal` | Art. 10 | Personal data relating to criminal convictions and offenses or related security measures. | A conviction record |

A photograph is not biometric data by itself; a face template used to identify someone is.

Declaring a qualifier records the author's statement on the concept. It does not change how the value is validated or stored. What it does:

- Providers that support it render the qualifier into the C# compliance details of the concept.
- The opt-in `--check purposes` asks for an Art. 9(2) `condition` on purposes that cover special-category data, and for an `authorization` on purposes that cover criminal data (`PLAY0603`, `PLAY0604`).
- The record of processing flags these concepts and prompts for a data protection impact assessment, as [Art. 35(3)(b)](https://gdpr-info.eu/art-35-gdpr/) can require for large-scale processing of such data.

See [Processing purposes](purposes.md) for the checks and the record, and [Diagnostics](diagnostics.md) for the codes. The qualifiers are not legal advice: they say what the author declared, not whether processing is lawful.

Qualifiers require the personal-data marker, and an unknown category is an error (`PLAY0570`); more than one `special` line is an error too (`PLAY0571`). The `personal` alias works on all personal-data body lines too.

ESM v13 stores the classification, qualifiers and scope on the concept, not on each property. Type references carry that meaning to command, event, read-model, composite-type and response members, including collection elements. Reasons stay report-only (`PLAY0270`) and are not stored. The canonical shapes are:

```json
{"personal":{"special":"health","criminal":true}}
{"secret":{"scope":"namespace"}}
{"personal":{},"secret":{}}
```

These are the values of the optional concept `compliance` member. Bare `pii` writes `{"personal":{}}`; absent or false qualifiers are omitted. No secret scope is stored next to personal classification. The strict reader refuses such a scope, unknown categories or scopes, and compliance on identity or enum concepts. See [interoperability](interoperability.md) for the wire contract.

### Subject and testing limits

In v13 a personal value's data subject is the event source. Binding refuses an event containing a personal value or an explicitly `secret scope subject` value together with an explicit event-property `subject` mark (`PLAY0268`), including protected values inside composite members and collections. Subject lineage is not admitted yet; remove the explicit mark only when the event source really is the intended subject. Subject marks otherwise remain report-only and add no ESM bytes.

Run your specifications as usual: compliance does not turn a supported specification into typed-unsupported execution, and concept validation rules still check command inputs. Reference execution cannot test encryption or erasure; verify those with the realizing provider. Markers and qualifiers do not promise lawful processing or retention enforcement, and provider support for newly declared details and scope must be checked separately.

## Legacy spelling

Repeating a marker on a concept header reports one `PLAY0653` Warning per header. This includes `pii personal`, because `personal` is an alias of `pii`, as well as `pii pii` and `secret secret`. Both compilers retain the first marker for each wire identity; body settings attach to that attribute, and canonical printing writes one marker. The **Remove duplicate compliance markers** line repair keeps the first spellings and preserves comments, quoted notes and line endings.

`@pii`, `sensitive` and `@sensitive` are deprecated but accepted with one `PLAY0565` Information diagnostic per line. Repairs migrate one line or the whole document to bare `pii`/`secret`, preserving comments and quoted notes. Unknown markers such as `@encrypted` are errors (`PLAY0566`). No classification marker may be placed on a property or composite type. The [event-property `subject` role](events.md#data-subject) is report-only lineage metadata, not classification; it requires an unprotected scalar identity concept (String, Uuid or Int-backed), or bare String/Uuid, under [decision 0047](https://github.com/Cratis/Screenplay/blob/main/decisions/0047-data-subject-mark-on-event-properties.md); `secret scope subject` selects encryption scope, not that event role.

## Enum concepts

An enum concept declares a fixed set of values as an indented list:

```screenplay
concept InvoiceStatus : Enum
  draft
  sent
  paid
  overdue
  cancelled

concept PaymentTerms : Enum
  net30
  net60
  immediate
```

### Pin stored member identity

An unnumbered enum assigns numbers by zero-based declaration position. Reordering it or inserting a member before existing ones changes the numbers stored by a generated application. Pin every member before making such a change:

```screenplay
concept InvoiceStatus : Enum
  draft = 0
  sent = 1
  paid = 2
  overdue = 3
  cancelled = 4
```

Numbers are all or none within one enum, unique, and in the int32 range (-2147483648 through 2147483647). Gaps and negative numbers are allowed. Write decimal digits with an optional minus sign, no leading zeros, fraction, exponent or plus sign. `-0` prints as `0`. Escape keyword-named members as usual, for example `@validate = 7`; descriptions and validation blocks still work.

Use numbers for enums stored in event properties and when preserving a legacy system's member identities. A short-lived enum used only in command inputs or views may stay positional. `PLAY0671` is an opt-in Information completeness hint for an unnumbered enum used by an event property, including through a composite type. Select `--check enum-pinning` (or `PLAY0671` or `all`) in the CLI, or `checks: "enum-pinning"` in MCP `screenplay_diagnostics`; ordinary compilation does not report it. The opt-in [numbering refactoring](mcp/authoring-tools.md#number-enum-members) writes the current positions; this is not a correctness repair.

Never renumber a published member or reuse its number for another meaning. [Model comparison](model-comparison.md#stored-enum-numbers) reports changed numeric identities against a baseline. To pin an existing enum, use its **current stored numbers**, not a newly reordered list. Specifications, fixtures and conditions still refer to member names (for example `status = "sent"`), never their numbers; the reference evaluator compares names.

**Executable admission:** pins equal to `0..n-1` preserve the existing executable version, bytes and revision. Other numbers select ESM v12: for example, `draft = 1` / `issued = 4` binds `numbers: [1, 4]` parallel to the member names. Consumers must explicitly admit v12 before rendering or extracting these stored identities. Specifications and conditions still use member names; numbers are not alternative values or flags. See [interoperability](interoperability.md).

## Validation

Date and DateTime concept rules remain authoring-only (`PLAY0268`): they also reach query arguments and clients, whose date-comparison contract is not admitted. ESM v26 instead admits date comparisons on command properties, including properties typed by a date concept. Date compares calendar dates; DateTime compares UTC instants at seven-digit tick precision. Only these command comparisons use instant equality; keys, constraints and specification expectations remain ordinal. See [chronological command date comparisons](commands.md#chronological-command-date-comparisons).

Uuid concept rules remain authoring-only (`PLAY0268`) for the same reason. ESM v28 instead admits `==` and `!=` between Uuid command properties in `require` and `produces when`, including properties typed by a Uuid concept or identity; two concepts must then be the same concept. See [Uuid equality in command conditions](commands.md#uuid-equality-in-command-conditions).

For .NET consumers of the executable semantic model, [check values against validation rules](value-rule-checks.md) with `SemanticValidationRules.Check` instead of copying the evaluator.

A concept can declare validation rules in an optional indented body — business rules that travel with the value everywhere it appears. The rules use the same shapes as command validation (see [Commands](commands.md)): declarative `validate` blocks and imperative `validate` blocks with a ` ```csharp ` fence. The one difference is that the rules omit the property subject — the concept's own value is implied.

`not empty` has the same meaning on a text concept as on a command property: null, empty text and text made only of Unicode `White_Space` characters fail. The [command rule reference](commands.md#what-the-executable-model-admits) lists the exact set. U+0085, U+00A0 and U+3000 are whitespace; U+FEFF is not. `" a "` passes without trimming the value. This applies to every ESM version; it does not change the separate emptiness rule for event-source and stream-id identity text.

`contains "text"`, `not contains "text"`, `starts with "text"` and `ends with "text"` compare literal UTF-16 text ordinally, without case folding or normalization. They apply to one String value, not an enumeration or collection. An absent optional value passes; presence is a separate `not empty` rule. Empty operands are valid: contains, starts with and ends with pass, while not contains fails for present text.

`matches` searches for a matching substring unless you add anchors. The `$` anchor means the very end of the input, not the position before a trailing newline: `matches "^[A-Z]{3}$"` accepts `ABC` but rejects `ABC\n`. Escaped `\$` and `$` inside a character class stay literal. The built-in `matches email` rule also rejects a trailing newline.

````screenplay
concept EmailAddress : String pii
  validate
    not empty          message "Email is required"
    matches email      severity warning message "Must be a valid email address"
  validate
    ```csharp
    string email = context.Value;
    if (email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
    {
        yield return "Example addresses are not allowed";
    }
    ```
````

The optional severity (`information`, `warning` or `error`) comes before a rule's message and defaults to `error`. A failing concept rule rejects a command even if its severity is `warning` or `information`; the level only controls how the failure is presented. The printer omits the default.

The named `email` pattern has the fixed definition documented under [command validation](commands.md#validation-rules); it is not RFC 5322 validation and rejects JavaScript whitespace, including U+00A0. Quoted patterns use the [portable match dialect](commands.md#portable-match-dialect), with the same UTF-16 meanings and authoring limits in both compilers. Both forms apply only to a text concept, and a match without anchors can match a substring. Exact legacy Studio lookaround text-rule shapes warn with `PLAY0787` and bind to ordinal literal rules; use `contains`, `not contains`, `starts with`, `ends with`, `==` or `!=` instead. Other lookarounds remain refused. The [pattern check API](commands.md#checking-patterns-before-authoring) reports the equivalent rule before an authoring proposal is made.

Inside the block `context` is the [`RuleContext`](context.md) — for a concept rule there is no surrounding artifact, so `context.Artifact` and `context.Value` are both the concept's own value. The block yields the message of every rule the value breaks, and yields nothing when the value is valid.

Enum concepts can combine their values with validate blocks — the values remain identifiers with optional numbers and the blocks are recognized by the `validate` keyword:

```screenplay
concept InvoiceStatus : Enum
  draft
  sent
  validate
    not empty  message "Status is required"
```

Rules on a concept take their meaning from the concept's type, exactly as on a command property: `max` and `min` bound the length of a `String` concept and the value of an `Int` or `Decimal` concept, and `length ==` fixes the length of text:

```screenplay
concept InvoiceNumber : String
  validate
    not empty  message "An invoice needs a number"
    length == 10

concept Quantity : Int
  validate
    min 1      message "Order at least one"
```

A `Decimal` concept can also limit its decimal places with `decimals <= <n>`, so the rule is stated once on the concept and holds wherever the concept is used:

```screenplay
concept Money : Decimal
  validate
    >= 0
    decimals <= 2  message "Money has at most two decimals"
```

In `decimals <= <n>` the operand is a whole number from 0 to 28 and the subject is a `Decimal` value. See [command validation](commands.md#validation-rules).

The executable semantic model enforces portable concept rules on **command input and query arguments**: every value of the concept, including each collection element and values inside composite [types](types.md). Query authorization runs first, then argument shape checks, then the rules on the `by` key and every supplied filter value; omitted optional filters are not validated. A violation rejects with `Validation`, not an empty result, in every ESM version. From ESM v18, one actionless queried call with `then error` asserts that rejection; see [query validation rejections](specifications.md#query-validation-rejections). A named rule with an inline or file body and a fenced concept `validate` block bind as opaque v3 attachments with a `pure` requirement; the reference evaluator cannot execute them and reports `SemanticUnsupported` for commands or supplied query arguments carrying that concept, including nested and collection properties. It does not enforce those rules on event payloads or read-model state. The executable semantic model admits the same rules for a concept as for a command property, and rejects the same ones for the same reasons — see [what the executable model admits](commands.md#what-the-executable-model-admits). `all >` and `all >=` quantify over a collection, which a concept's own value never is, so they belong on the command property instead.

In the compiled syntax tree the implied subject is represented by the well-known property name `value` — the `ValidationRuleSyntax.ConceptValue` constant — so consumers can treat concept rules and command rules uniformly.

## Identifiers cannot be personal data or operational secrets

An event source identifier cannot be encrypted or erased. A concept marked `pii` or `secret` cannot be used as a command `identifier`, as an explicit `for` destination, or as an `eventsource` identifier (`PLAY0515`, mirroring Chronicle `CHR0034` for `[PII]` and `CHR0052` for `[Encrypted]`). Use a surrogate `Uuid` concept for identity and keep the personal value or operational secret in an ordinary property:

```screenplay
concept PatientId : Uuid
concept NationalId : String pii
```

The mapping follows [decision 0034](https://github.com/Cratis/Screenplay/blob/main/decisions/0034-sensitive-means-operational-secret.md); names, scope and qualifiers follow [decision 0041](https://github.com/Cratis/Screenplay/blob/main/decisions/0041-personal-data-secrets-and-processing-purposes.md). Processing-purpose declarations, purpose coverage checks and a processing record are later phases, not available language features.

## Attribute inheritance

When a concept is used as a property type on a command or event, its attributes are inherited — you never annotate at the property level. Declaring `EmailAddress` as `pii` once means every event property, command property, and read model field typed as `EmailAddress` is treated as PII automatically.

This holds through composite [types](types.md) too: a `pii` concept inside a `type` is personal data wherever that type is used.
