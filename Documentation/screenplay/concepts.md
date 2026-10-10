# Concepts

Concepts are formalized value types that wrap a primitive. They give every domain value a precise, strongly-typed name — you never pass a raw `Uuid` or `String` around — and they are where compliance is declared. Bare markers declare the protection a concept needs: `pii` marks personal data (GDPR Art. 4(1)); `secret` marks an operational secret, encrypted at rest without erasure and withheld from the causation chain. C# providers map these to Chronicle and Arc attributes as described below.

A concept names one primitive value. For a shape made of several — the child records events carry — see [Types](types.md).

## Syntax

```screenplay
concept <Name> : <PrimitiveType> [<attributes>]
  [file <path>]
  [<attribute> reason "<text>"]*
  [validate ...]*

concept <Name> : Enum
  [file <path>]
  [<attribute> reason "<text>"]*
  <value>+
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

Compliance attributes are not admitted by the executable semantic model: binding a concept with either marker reports `PLAY0268`. The mapping above describes C# provider rendering, not reference execution.

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

These facts remain syntax-only: binding reports `PLAY0268`, including scopes and qualifiers. They do not promise lawful processing, retention enforcement or reference execution. Provider support for newly declared details and scope must be checked separately.

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

## Validation

A concept can declare validation rules in an optional indented body — business rules that travel with the value everywhere it appears. The rules use the same shapes as command validation (see [Commands](commands.md)): declarative `validate` blocks and imperative `validate` blocks with a ` ```csharp ` fence. The one difference is that the rules omit the property subject — the concept's own value is implied.

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

The named `email` pattern has the fixed ECMAScript definition documented under [command validation](commands.md#validation-rules); it is not RFC 5322 validation. A quoted ECMAScript pattern is also allowed. Both forms apply only to a text concept, and a match without anchors can match a substring.

Inside the block `context` is the [`RuleContext`](context.md) — for a concept rule there is no surrounding artifact, so `context.Artifact` and `context.Value` are both the concept's own value. The block yields the message of every rule the value breaks, and yields nothing when the value is valid.

Enum concepts can combine their values with validate blocks — the values remain bare identifiers and the blocks are recognized by the `validate` keyword:

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

The executable semantic model enforces portable concept rules on **command input**: every value of the concept, including each collection element and values inside composite [types](types.md). A named rule with an inline or file body and a fenced concept `validate` block bind as opaque v3 attachments with a `pure` requirement; the reference evaluator cannot execute them and reports `SemanticUnsupported` for commands carrying that concept, including nested and collection properties. It does not enforce those rules on event payloads, read-model state or query keys. The executable semantic model admits the same rules for a concept as for a command property, and rejects the same ones for the same reasons — see [what the executable model admits](commands.md#what-the-executable-model-admits). `all >` and `all >=` quantify over a collection, which a concept's own value never is, so they belong on the command property instead.

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
