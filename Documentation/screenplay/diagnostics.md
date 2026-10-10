# Diagnostics

Every problem the compiler finds is reported as a **diagnostic**: a severity, a stable code, a message, and the line and column it came from. This page is the catalogue of those codes.

A message is written for a person and gets reworded whenever a clearer wording is found. A **code never changes**. So a code is what you match on, suppress on, and group by - anything reading the message text breaks the next time the message is improved.

## Reading a diagnostic

The CLI prints a diagnostic in the format editors and build servers already parse - the file, the position, the severity, the code, and the message - followed by the offending line and a caret:

```text
nested/broken.play(3,5): error PLAY0028: Unknown slice type 'Wat' - expected StateChange, StateView, Automation or Translate
    3 |     slice Wat DoIt
      |     ^
```

There are three severities. **Error** means the document does not compile. **Warning** means the document compiles but something is very likely wrong - almost always a name nothing declares. **Information** means something worth knowing that changes nothing.

## Why the codes read `PLAY`

The prefix is `PLAY`, after the `.play` documents this compiler reads.

Cratis Arc has its own catalogue, the `SP` codes, for *generating* a `.play` document from C# source. The two run one after the other - Arc generates a document and hands it straight to this compiler to read back - so both sets of diagnostics land in the same build log. A shared prefix would make `SP0034` and a compiler code of the same number indistinguishable at a glance and identical to a `SP\d{4}` filter, which is why the prefixes deliberately have nothing in common. Arc's codes describe what the *generator* could not express; the codes here describe what the *compiler* could not read.

## Codes are permanent

A code is an identifier, not a position in a list.

- A number is **never reused**. When a diagnostic is retired, its number is retired with it and left behind as a gap in the sequence. Handing that number to something else would silently change what an existing suppression means.
- A number is **never renumbered**. Inserting a code in the middle of the catalogue would change the meaning of every code after it.
- A new code is **appended at the end** of the sequence whatever it is about, so the number says when a code was added rather than where it belongs. Use this page, not the numbering, to find the codes for an area.
- A code **outlives its message**. Two constructs hitting the same condition share one code - a property line the parser cannot read reports `PLAY0016` whether it sits in a `type` or in an `event`.

## Reacting to a code

`Diagnostic` carries the code alongside the severity, the message and the location, so a consumer filters on it directly:

```csharp
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

var result = new ScreenplayCompiler().Compile(source);

// A document assembled a piece at a time refers to events the pieces have not introduced yet,
// so that one warning is expected here and everything else is not.
var unexpected = result.Diagnostics
    .Where(diagnostic => diagnostic.Code != DiagnosticCodes.UnknownEvent)
    .ToList();
```

`DiagnosticCodes` declares every code in this catalogue as a named constant, so the compiler catches a typo that a string literal would not.

## In the editor

The language service behind the VS Code extension and the Monaco editor checks a subset of what the compiler
checks, and it reports **the compiler's code** for every condition in that subset. So the `Unknown type 'Foo'`
you get as a squiggle and the one the CLI prints are the same `PLAY0165`, and can be looked up here, filtered
on, and matched against a build log:

```typescript
import { diagnosticCodes, validateLines } from '@cratis/screenplay-language';

const unknownTypes = validateLines(lines).filter((issue) => issue.code === diagnosticCodes.unknownType);
```

`diagnosticCodes` names the codes the editor reports, the same way `DiagnosticCodes` names them for the
compiler. A `ValidationIssue` carries the code as `code`, and it reaches the editor: a Monaco marker gets it as
`code`, a VS Code diagnostic as `Diagnostic.code`.

**A few editor checks carry no code, deliberately.** The capture and projection validators enforce structural
rules that no compiler run reports - a capture must include a `source` block, an `append` block must include a
`when` clause - and there is no `PLAY` number for something the compiler never emits. Minting one would make
this catalogue describe two different tools, which is the thing the `PLAY` prefix was chosen to avoid. Those
conditions are reported without a code until the compiler checks them too.

## The catalogue

### The document and its top level

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0001` | Error | A line at the top level of a document opens with a word nothing at that level is declared by. |
| `PLAY0002` | Error | A `domain` line is not `domain <Qualified.Name>`. |
| `PLAY0003` | Error | A document declares a domain more than once, and a document has at most one. |
| `PLAY0004` | Error | `domain` is declared after another construct, and it names what the whole document is about. |
| `PLAY0005` | Error | An `import` line is not `import <Qualified.Name> [from "<origin>"]`, or its origin metadata is invalid. |
| `PLAY0006` | Warning | A line is indented with tabs, and Screenplay decides nesting from spaces. |

### Event sources and command streams

Event-source declarations and command stream routes are admitted by ESM v8.
Property-path mappings and handler commands remain refused with `PLAY0268`. Binding reports `PLAY0273`
for generated mappings, stored-name collisions (including pin-versus-name), the reserved stored source name
`Default`, or a routed fixture whose source identifier type cannot be resolved.
A stream reference selects a classification, never the identity destination supplied by `for`.
Handler commands may author routes without declaring their returned events.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0503` | Error | An event source or its source-owned stream has invalid syntax, a repeated directive or physical declaration, an invalid rename pin, or an optional, collection or known composite identifier type. Composite stream-id blocks also refuse fewer than two parts, duplicate names, modifiers, children, empty/repeated headers and mixed scalar/block forms. Duplicate physical sources make their child ownership ambiguous. |
| `PLAY0504` | Error | A known source's stream does not resolve uniquely, or a command route or stream-id mapping is invalid, missing or incompatible, including missing, unknown, duplicated or mismatched-shape composite parts. Scalar literals and composite literal parts refuse empty, non-NFC or ill-formed text and integers outside ±9007199254740991 in Double numeric mode. A command identifier or known production destination type differing from the source's nominal identifier type is refused, never silently retargeted. |
| `PLAY0505` | Error | An exact `stream Source.Stream` header resolves both to one source-owned stream and to a viable imported value type. Both candidates remain visible; neither is selected automatically. |
| `PLAY0506` | Error | A known scalar stream-id or composite part type falls outside text and UUID values and their nominal concepts, plus integer-backed concepts. Bare Int is rejected. Other types need a future portable formatting contract. Unavailable imported shapes remain unresolved. |
| `PLAY0507` | Information | A source or stream's rename-only `id` pin repeats its current name. New declarations omit the pin. |
| `PLAY0547` | Error | A specification routing directive or composite part block is malformed, empty, repeated, conflicting, has invalid children, or leaves effective `no stream` on a `given` or `when append` event. Event examples and redelivery locators may carry `no stream`. |
| `PLAY0548` | Error | A `when <Command>` occurrence declares `stream` or `no stream`; the route belongs to the command declaration. |
| `PLAY0549` | Error | A specification stream reference is missing or ambiguous, its key mapping is missing or superfluous, or the scalar stream id or composite part is nonliteral or incompatible, or contains empty, non-NFC or ill-formed text or an integer outside ±9007199254740991 in Double numeric mode. Composite routes require every declared part exactly once and refuse unknown/duplicate names and mismatched scalar/block forms. |
| `PLAY0550` | Error | A routed `given` or `when append` lacks `for`, a routed identity is not a compatible concrete literal, or a source without `identifier` has no unambiguous known producer destination type. |
| `PLAY0551` | Error | A `then` route or destination type contradicts the command under test, which is the event's only producer in the whole model. Other producers defer the comparison. |

Command headers are classified against the complete immutable compilation input, including resolved file
imports. `@stream Qualified.Type`, `stream String` and modified property forms remain properties;
production `stream = value` and `stream String = value` remain payload mappings. When neither a source
nor a property type resolves, the header keeps its legacy property interpretation and unknown-type
evidence, including deeper legacy command members. A misspelled source name cannot be distinguished
from an unresolved qualified property by spelling alone. Stream-id children do not override this rule.
Candidate discovery uses the same inline-language registry as the committed parse; registered code
payloads cannot declare sources or property types.

Syntax JSON retains every ambiguous or duplicate header in `CommandSyntax.streamCandidates`, including
its property interpretation when ambiguous. `stream` holds at most one unambiguous route. Deeper legacy
properties remain command members, without duplicating the candidate property. Such invalid drafts
cannot be printed or expanded into `.play` files: export throws `InvalidSyntaxJson` rather than selecting
an interpretation or dropping a header. Retain syntax JSON until you repair the draft. Use `@stream`
for an intended qualified property, or remove the competing type interpretation for an intended route;
remove duplicate route headers before export.

### Concepts

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0007` | Error | A `concept` line is not `concept <Name> : <Type>`. |
| `PLAY0008` | Error | A concept is declared over a primitive the language does not have. |
| `PLAY0009` | Error | A value of an enumeration concept is not an identifier. |
| `PLAY0010` | Error | A line in a concept body opens with a word a concept declares nothing by. |
| `PLAY0011` | Warning | A value of an enumeration is called `validate`, which the concept body reads as an empty validate block. |
| `PLAY0012` | Error | A concept gives a reason, scope or personal-data qualifier for a marker it does not carry. |
| `PLAY0013` | Error | A concept gives the reason for one attribute more than once. |
| `PLAY0515` | Error | A concept marked `pii` or `secret` is used as a command identifier, an explicit `for` destination, an event source identifier, a scalar stream id type, a composite stream id part type, or a command route mapping source (including nested property paths). The message names the attribute and position, never a value. A mapping using the same protected concept already reported at its resolved stream-id declaration is not reported again; a different protected source concept is still reported. Specification route literals rely on their declaration's check. Use a surrogate `Uuid` identifier and keep personal data or operational secrets as properties. Reaction destinations also check values typed directly in the trigger clause, even for an undeclared or registered trigger. When a reaction source names both an event and a declared trigger, a protected destination in the resolved event shape or the trigger clause is rejected. |
| `PLAY0565` | Information | Legacy `@pii`, `sensitive` or `@sensitive` spelling. Use bare `pii`/`secret`; per-line and document repairs preserve notes and trivia. |
| `PLAY0566` | Error | Unknown concept compliance marker; expected `pii`, `personal` or `secret`. |
| `PLAY0567` | Error | Scope belongs to `secret` and must be `subject`, `namespace` or `global`. |
| `PLAY0568` | Error | A concept declares secret scope more than once. |
| `PLAY0569` | Warning | Explicit secret scope on `pii secret` is ignored because only Chronicle `[PII]` renders. |
| `PLAY0570` | Error | Invalid personal-data qualifier or unknown Art. 9(1) category. |
| `PLAY0571` | Error | A concept declares more than one `pii special` category. |
| `PLAY0590` | Error | An event marks more than one data-subject property; each extra mark names the first. Choose one; no automatic repair is safe. |
| `PLAY0591` | Error | A subject must be a required scalar String, Uuid or their concepts, or an Int-backed concept. Optional values, collections, composites, bare Int, enums, Decimal, Bool, Date and DateTime are refused. |
| `PLAY0592` | Error | A subject concept is pii or secret; EventContext.Subject is plaintext. Use a surrogate identity. |
| `PLAY0593` | Error | The subject modifier belongs only to event properties, not commands, types, operation inputs, trigger or reaction data or response fields (decision 0008). |
| `PLAY0594` | Error | Read model subjects are not yet supported; #559 covers the mark and Chronicle's reserved _subject, __subject and __subjects names. |
| `PLAY0595` | Error | Subject is duplicated or out of modifier order. Write each modifier once: Type optional generated identifier subject. |

### Processing purposes

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0596` | Error | Invalid purpose declaration, reference or field syntax. |
| `PLAY0597` | Error | Unknown basis, condition or erasure-exception value. |
| `PLAY0598` | Error | A purpose repeats a singleton field. |
| `PLAY0599` | Error | A purpose name is declared more than once. |
| `PLAY0600` | Warning | A purpose reference does not resolve. |
| `PLAY0601` | Warning | Interest is declared without basis legitimateInterests, or that basis has no nonblank interest statement. |
| `PLAY0602` | Warning | Opt-in purposes check: a slice carries pii without a declared purpose in scope. |
| `PLAY0603` | Warning | Opt-in purposes check: special-category data lacks a purpose's condition. |
| `PLAY0604` | Warning | Opt-in purposes check: criminal data lacks a purpose's authorization. |
| `PLAY0605` | Warning | Opt-in purposes check: a purpose has no basis. |
| `PLAY0606` | Warning | Opt-in purposes check: a purpose is never referenced. |

These are structural findings, not legal verdicts. See [Processing purposes](purposes.md).

### Types

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0014` | Error | A `type` line is not `type <Name>`. |
| `PLAY0015` | Error | A type declares no properties, and a type is the properties it holds. |
| `PLAY0016` | Error | A property line is not `<name> <Type>`. |
| `PLAY0017` | Error | A property outside a command is marked as the identifier, which only a command property can be. |

### Events

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0018` | Error | An `event` line is not `[public] event <Name> [generation <N>] [from "<origin>"]`, or its visibility/origin metadata is invalid. |
| `PLAY0019` | Error | A property of an event is marked as the identifier, and an event never carries its event source id. |
| `PLAY0020` | Warning | A property called `tag` is read by the event body as a static tag rather than as a property. |
| `PLAY0446` | Error | An event generation is zero, exceeds the 32-bit generation range, or uses Chronicle's reserved unspecified value (4294967295). |
| `PLAY0447` | Error | The same event in one slice declares a generation twice; two unmarked declarations are both generation 1 and therefore duplicate. |
| `PLAY0448` | Error | An event in one slice omits a generation between 1 and its latest generation. |

### Modules, features, slices and layouts

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0021` | Error | A `module` line is not `module <Name>`. |
| `PLAY0022` | Error | A line in a module body opens with a word a module declares nothing by. |
| `PLAY0023` | Error | A `feature` line is not `feature <Name>`. |
| `PLAY0024` | Error | A line in a feature body opens with a word a feature declares nothing by. |
| `PLAY0025` | Error | A slot declared by a layout, screen template or dialog template is not an identifier optionally followed by `contributes`. |
| `PLAY0026` | Error | A line in a layout, screen template or dialog template body opens a block none of them declares anything by. |
| `PLAY0027` | Error | A `slice` line is not `slice <Type> <Name>`, or direction is malformed, repeated or declared outside Translate. Legacy directionless translations remain accepted. |
| `PLAY0028` | Error | A slice is declared with a type the language does not have. |
| `PLAY0029` | Warning | A line in a slice body opens with a word a slice declares nothing by. |

### Personas

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0030` | Error | A `persona` line is not `persona <Name>`. |
| `PLAY0031` | Error | A policy line in a persona body is not `policy <Name>`. |
| `PLAY0032` | Error | A line in a persona body opens with a word a persona declares nothing by. |

### Commands

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0033` | Error | A `command` line is not `command <Name>`. |
| `PLAY0034` | Error | A line in a command body opens with a word a command declares nothing by. |
| `PLAY0035` | Error | A command declares both `produces` and `handler`, which say the same thing two ways. |
| `PLAY0036` | Error | A command marks more than one property as its identifier. |
| `PLAY0037` | Error | A `concurrency` line carries anything beyond the keyword. |
| `PLAY0038` | Error | A command declares more than one concurrency block, and a command has at most one. |
| `PLAY0039` | Error | A line in a concurrency block names a dimension the block does not have. |
| `PLAY0040` | Error | A dimension of a concurrency block is not written the way that dimension is written. |
| `PLAY0041` | Error | A concurrency block states one dimension more than once. |
| `PLAY0042` | Error | A `produces` line is neither `produces <EventType>` nor `produces when <condition>`. |
| `PLAY0043` | Error | A `produces when` condition is followed by no event to produce. |
| `PLAY0044` | Error | A mapping line is not `<property> = <source>`. |
| `PLAY0045` | Error | A handler names neither a `file`, an inline code block nor an explicit `implementation` wrapper. |
| `PLAY0046` | Error | A line in a handler body opens with a word a handler declares nothing by. |

### Queries

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0047` | Error | A `query` line is not `query <Name> => [observable] <ReadModel>`. |
| `PLAY0048` | Error | A line in a query body opens with a word a query declares nothing by. |
| `PLAY0049` | Error | A `by` or `filter` parameter is not `<keyword> <name> <Type> [from <source>]`. |
| `PLAY0050` | Error | A `performer` line carries anything beyond the keyword. |
| `PLAY0051` | Error | A query declares more than one performer, and a query has at most one. |
| `PLAY0052` | Error | A performer names neither a `file` nor an inline code block. |
| `PLAY0053` | Error | A line in a performer body opens with a word a performer declares nothing by. |

### Projections

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0054` | Error | A projection document holds a top level line that does not open a `projection`. |
| `PLAY0055` | Error | A projection document declares no projection at all. |
| `PLAY0056` | Error | A `projection` line is not `projection <Name> [=> <ReadModel>]`. |
| `PLAY0057` | Error | A projection declares no directives, so it builds nothing. |
| `PLAY0058` | Error | A line in a projection body opens with a word a projection declares nothing by. |
| `PLAY0059` | Error | A projection declares more than one key. |
| `PLAY0060` | Error | A `from` block declares more than one key. |
| `PLAY0061` | Error | A `from` line names no event to read from. |
| `PLAY0062` | Error | An event reference is not a name the language can read as one. |
| `PLAY0063` | Error | A `join` line is not `join <property> on <key>`. |
| `PLAY0064` | Error | A join block holds a line that is not `with <EventType>`. |
| `PLAY0065` | Error | A `children` line is not `children <collection> identified by <key>`. |
| `PLAY0066` | Error | A `nested` line is not `nested <property>`. |
| `PLAY0067` | Error | A nested block reads from no event, so nothing ever fills it. |
| `PLAY0068` | Error | A `remove` line is neither `remove with <EventType>` nor `remove via join on <EventType>`. |
| `PLAY0069` | Error | A remove block holds a line other than `parent`. |
| `PLAY0070` | Error | A `clear` line is not `clear with <EventType>`. |
| `PLAY0071` | Error | `clear with` is written where there is nothing to clear. |
| `PLAY0072` | Error | A part of a composite key is not `<property> = <expression>`. |
| `PLAY0073` | Error | A composite key part is a template expression, which a key cannot be. |
| `PLAY0074` | Error | A composite key declares no parts. |
| `PLAY0075` | Error | A mapping line in a projection is not one the language can read. |
| `PLAY0452` | Warning | More than one `automap` or `no automap` appears in the same projection scope. The last setting wins; printing preserves the authored settings and their individual comments. |
| `PLAY0514` | Warning | A projection mapping, `children`, or `nested` target is absent from the declared read-model or element shape. Unknown or imported shapes are not guessed. |

### Captures

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0076` | Error | A capture document holds a top level line that does not open a `capture`. |
| `PLAY0077` | Error | A capture document declares no capture at all. |
| `PLAY0078` | Error | A `capture` line is not `capture <Name>`. |
| `PLAY0079` | Error | A line in a capture body opens with a word a capture declares nothing by. |
| `PLAY0080` | Error | A map entry is not `<property> = <source> [translate]`. |
| `PLAY0081` | Error | A translation is not `"<source>" => <target>`. |
| `PLAY0082` | Error | A `split` line is not `split <property> by "<separator>"`. |
| `PLAY0083` | Error | A target of a split is not a property path. |
| `PLAY0084` | Error | An `append` line is not `append <EventType>`. |
| `PLAY0085` | Error | A line in an append body opens with a word an append declares nothing by. |
| `PLAY0086` | Error | A `when` line names no trigger. |
| `PLAY0087` | Error | A `when` clause is not one of the shapes a trigger is written in. |
| `PLAY0088` | Error | A value transition is not `when <Path> from <value> to <value>`. |
| `PLAY0089` | Error | A `when` clause combines properties with both `and` and `or`. |
| `PLAY0090` | Error | A `when` combinator is followed by no property. |
| `PLAY0091` | Error | A line in a children or nested block is neither `map` nor `append`. |

### Specifications

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0092` | Error | A specification document holds a top level line that opens neither a `specification` nor an `example`. |
| `PLAY0093` | Error | A specification document declares no specification at all. |
| `PLAY0094` | Error | A `specification` line is not `specification <Name>`. |
| `PLAY0095` | Error | A line in a specification body opens with a word a specification declares nothing by. |
| `PLAY0096` | Error | A `when` line is not `when <CommandType>`. |
| `PLAY0097` | Error | A specification issues more than one command, and a specification is one example. |
| `PLAY0098` | Error | A `then error` line is neither `then error` nor `then error "<reason>"`. |
| `PLAY0099` | Error | A `given readmodel` or `then readmodel` line does not name a read model type. |
| `PLAY0100` | Error | A `given` or `then` line does not name an event type. |
| `PLAY0101` | Error | A value a specification step states is not `<property> = <value>`. |
| `PLAY0453` | Error | A `then no readmodel` line lacks a view or key, uses `exactly`, or has child mappings. |
| `PLAY0518` | Error | An example declaration is not `example <Name> : <EventOrCommandOrReadModel>`. |
| `PLAY0519` | Error | A fixture assigns the same property more than once, including across a step's inline assignment and indented body. Assign it once; overriding a value from an example is a separate operation. |
| `PLAY0520` | Error | An example's type or reference is unknown, ambiguous, or not an event, command, or read model. Qualify the declaration; example inheritance is not supported. |
| `PLAY0521` | Error | An example name collides with a type name or repeats in the same scope. Choose a distinct example name. |
| `PLAY0522` | Error | A step references an example of another kind. Use the corrected step spelling suggested by the diagnostic. |
| `PLAY0523` | Error | An example supplies an undeclared current-generation property, an invalid generated fixture, or `for` on a read model. Use only the current type's allowed fixture lines. |
| `PLAY0524` | Error | Binding an exact-shape specification step found a missing required property after expansion. The diagnostic names the step, property, and example when used. Supply the property in the example or step; no defaults are assumed. |
| `PLAY0525` | Error | Semantic admission found a stated example value incompatible with its type, or `for` without one unambiguous required scalar destination type. Fix the value or its destination contract, even if the example is unused or that value is overridden. Partial top-level examples remain allowed. Ordinary fixture diagnostics also apply to null, nonconcrete, structured and generated values. |
| `PLAY0526` | Error | A command or read-model example states a route. Only event examples carry routes; a command's route comes from its declaration, and read models have none. Top-level `streamId = value` is payload, not route metadata. |

### Screens

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0102` | Error | A `screen` line is not `screen <Name>`. |
| `PLAY0103` | Error | A line in a screen body opens with a word a screen declares nothing by. |
| `PLAY0104` | Error | A `data` line is not `data <ReadModel> via query <Query> [by <param>]`. |
| `PLAY0105` | Error | An `action` line is not `action <Command>`. |
| `PLAY0106` | Error | A line in an action body is neither `label` nor `navigate to`. |
| `PLAY0107` | Error | A navigation is not `navigate to <Screen> [by <param>]`. |
| `PLAY0108` | Error | A line under a screen layout does not name a slot. |
| `PLAY0109` | Error | A `title` line is not `title "<text>"`. |
| `PLAY0110` | Error | A line in a table body is neither `column` nor `on row-click navigate to`. |
| `PLAY0111` | Error | A line in a summary body is not `field <property> label "<text>"`. |

### Policies and authorization

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0112` | Error | A `policy` line is not `policy <Name>`. |
| `PLAY0113` | Error | A line in a policy body is neither `require` nor an inline code block. |
| `PLAY0114` | Error | A policy states nothing it requires of the caller. |
| `PLAY0115` | Error | A policy condition holds a token the language has no reading for. |
| `PLAY0116` | Error | A policy requirement states no condition. |
| `PLAY0117` | Error | A group opened in a policy condition is never closed. |
| `PLAY0118` | Error | A `role` requirement names no role. |
| `PLAY0119` | Error | A `claim` requirement names no claim. |
| `PLAY0120` | Error | A claim requirement does not say what the claim is matched against. |
| `PLAY0121` | Error | A claim match states nothing to match the claim to. |
| `PLAY0122` | Error | An `authorize` clause names no policy. |
| `PLAY0123` | Error | A policy is referred to by something that is not a policy name. |
| `PLAY0440` | Error | A policy combines `require` with an inline code block or a file implementation. Choose one form. |
| `PLAY0441` | Error | A policy has more than one `require` line; combine the conditions with `and`/`or` in one `require`. |

### Authentication

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0124` | Error | An `authentication` line carries anything beyond the keyword. |
| `PLAY0125` | Error | A document declares more than one authentication block, and a document has at most one. |
| `PLAY0126` | Error | A `provider` line is not `provider <Name>`. |
| `PLAY0127` | Error | A setting of a provider is not `<name> <value>`. |

### Event seeding

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0128` | Error | A `seed` line carries anything beyond the keyword. |
| `PLAY0129` | Error | A seed group is not `for "<event source id>"`. |
| `PLAY0130` | Error | A line in a seed group does not name an event type. |
| `PLAY0131` | Error | A value a seeded event carries is not `<property> = <value>`. |

### Constraints

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0132` | Error | A `constraint` line is not `constraint <Name>`. |
| `PLAY0133` | Error | A constraint states nothing it holds the application to. |
| `PLAY0134` | Error | A line in a constraint body is not one the language can read. |
| `PLAY0135` | Error | A constraint mixes unique event and unique property rules, combines a file rule with another rule, repeats a singular option, or repeats a target, release, or property. An event cannot both claim and release the same constraint. Several distinct unique rules of the same kind are allowed. |

### Reactions

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0136` | Error | A `reaction` line is not `reaction <Name>`. |
| `PLAY0137` | Error | A line in a reaction body is not a trigger the language reads. |
| `PLAY0138` | Error | A reaction states no trigger, so nothing ever sets it off. |
| `PLAY0139` | Error | A line in a reaction trigger body opens with a word a trigger declares nothing by. |

### Validation rules

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0140` | Error | A `validate` line is neither `validate` nor the deprecated `validate csharp` form. |
| `PLAY0141` | Error | A validation rule is not one the language can read. |
| `PLAY0142` | Error | A validation rule names a rule the language does not have. |
| `PLAY0143` | Error | A `rule` line does not name the rule with an identifier. |
| `PLAY0144` | Error | A named rule names neither a `file` nor an inline code block. |

### Descriptions and tags

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0145` | Error | A `description` line is not `description "<text>"`. |
| `PLAY0146` | Error | A fenced description holds no text. |
| `PLAY0147` | Error | Something is described more than once, and a description is given once. |
| `PLAY0148` | Error | A `tag` line carries no value. |
| `PLAY0149` | Error | A tag value is neither an identifier, a string literal nor a context expression. |

### Expressions

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0150` | Error | A `literal` expression carries no value. |
| `PLAY0151` | Error | A `$causedBy` expression names a property the cause does not carry. The short form admits what the [event context catalog](projections/event-context.md#available-properties) lists below `causedBy`: `subject`, `name`, `userName` and `onBehalfOf`, which is itself an identity with the same members. |
| `PLAY0152` | Error | An expression is not one the language can read. |
| `PLAY0153` | Warning | A $context path opens with a root the context does not have. |
| `PLAY0154` | Warning | A $context.causedBy path names a property the cause does not carry. |
| `PLAY0155` | Warning | A $context.identity path names a property the identity does not carry. |
| `PLAY0156` | Error | A template expression is never closed. |
| `PLAY0157` | Error | An interpolation inside a template expression is never closed. |

### Conditions

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0158` | Error | A condition holds a token the language has no reading for. |
| `PLAY0159` | Error | A condition is expected and nothing is written. |
| `PLAY0160` | Error | A group opened in a condition is never closed. |
| `PLAY0161` | Error | A comparison states what is compared and not how. |
| `PLAY0162` | Error | A comparison states nothing to compare against. |

### Inline code

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0163` | Error | A construct opening an inline code block is followed by no fence. |
| `PLAY0164` | Error | An inline code block is never closed. |

### Names the document does not resolve

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0165` | Warning | A property names a type nothing in the document or its imports declares. |
| `PLAY0166` | Warning | An event is referred to that nothing in the document or its imports declares, including projection `remove with`, `remove via join on`, and capture `append`. |
| `PLAY0167` | Warning or error | A policy is referred to that nothing in the document declares. A persona's unknown policy is an error during compilation, Safe authoring, and executable binding. Draft authoring retains it as a warning with explicit unresolved-reference debt; other unresolved policy references are warnings. |
| `PLAY0168` | Error | A concept and a type, or two of either, are declared under one name; also reused when an inline event repeats a typed payload property name. |
| `PLAY0169` | Error | An authentication block declares two providers under one name. |
| `PLAY0170` | Error | A seed block seeds nothing. |
| `PLAY0171` | Error | A concurrency block narrows nothing. |

### A folder compiled as one application

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0172` | Error | Two files of a folder each declare something the application has at most one of. |
| `PLAY0173` | Error | Two files of a folder declare the same name. |
| `PLAY0174` | Warning | Two files of a folder describe the same thing differently, and the first description is kept. |

### What a command or reaction trigger reads to decide

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0175` | Error | A `reads` line is not `reads <ReadModel> [as <alias>] [by <value>]`, or uses `as`, `by`, or `reads` as an alias. Under a reaction trigger, a bare `reads` line suggests `@reads` for a value named `reads`. |
| `PLAY0177` | Warning | A command or reaction trigger reads a read model no projection in the document produces. |
| `PLAY0178` | Warning | The `by` of a command's `reads` declaration does not name a property of the command. |
| `PLAY0442` | Warning | A reaction trigger reads by a value it does not take. |
| `PLAY0443` | Error | A clock trigger reads by a value, but clock triggers take no values. |
| `PLAY0444` | Warning | A reaction trigger reads a primitive type name as a view; use `@reads <PrimitiveType>` if `reads` is a trigger value. |
| `PLAY0451` | Error | A `reads` line in a command or reaction trigger has indented children; the block is skipped, not parsed as owner values or properties. |

### Rules about the whole artifact

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0179` | Error | A `require` rule carries no condition. |
| `PLAY0180` | Error | The body of a `require` rule holds something other than its `message`. |
| `PLAY0181` | Warning | A `require` operand is qualified by something the command does not read. |
| `PLAY0182` | Warning | A `require` operand names neither a property of the artifact nor state it reads. |

<a id="authentication-1"></a>

### Authentication provider configuration

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0183` | Error | An authentication provider carries a configuration body, which belongs where the application runs. |

### Authorize

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0184` | Error | Tokens are left over after the requirement of an `authorize`. |
| `PLAY0185` | Error | A parenthesised group in an `authorize` is never closed. |

### Read models and reducers

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0186` | Error | A `readmodel` line is not `readmodel <Name>`. |
| `PLAY0187` | Error | A `reducer` line is not `reducer <Name> => <ReadModel>`. |
| `PLAY0188` | Error | A line in a reducer body is not an `on <EventType>` rule. |
| `PLAY0189` | Error | A reducer declares no rule, so nothing it observes is stated. |
| `PLAY0190` | Error | The body of a reducer rule holds something other than a description, a file or inline code. |
| `PLAY0191` | Error | More than one projection or reducer builds the same read model. |
| `PLAY0192` | Error | A document declares the same read model more than once. |
| `PLAY0398` | Error | A reducer has some `on` rules with transition bodies and others without them. Give every rule an inline or file body. |
| `PLAY0399` | Error | A reducer observes the same resolved event in more than one `on` rule. |

### Where a produced event lands, and what a reaction sets off

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0193` | Error | A `produces` declares more than one `for`, and an event is appended to one event source. |
| `PLAY0194` | Error | An `invokes` line is not `invokes <Command>`. |
| `PLAY0195` | Warning | A reactor invokes a command the document does not declare. |

### What a screen binds to

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0196` | Warning | A screen binds data to a query nothing in scope declares. |
| `PLAY0197` | Warning | A screen navigates to a screen nothing in scope declares. |
| `PLAY0198` | Warning | A bare name matches more than one declaration at the same depth, or a qualified name matches more than one trailing container path, so which one it means is undecided. See [declared dependency targets](#declared-dependencies). |

### What a query's results are narrowed to

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0199` | Error | A `scoped` line is not `scoped to <scope>`. |
| `PLAY0200` | Error | A query declares more than one scope, and results are narrowed one way. |

### UI profiles

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0201` | Error | A `ui profile` line is not `ui profile <Name>`. |
| `PLAY0202` | Error | Two `ui profile` blocks in the same document declare the same name. |
| `PLAY0203` | Error | A `target` line under a `ui profile` is neither `target platform ...` nor `target size ...`. |
| `PLAY0204` | Error | A `ui profile` declares `target platform` or `target size` more than once. |
| `PLAY0205` | Error | A line under a `packages` block is not a valid package name. |
| `PLAY0206` | Error | A `ui profile`'s `packages` block lists the same package more than once. |
| `PLAY0207` | Error | A line in a `ui profile` body is not `target` or `packages`, or `packages` is declared more than once. |

### Forms

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0208` | Error | A `form` line is not `form <Name> for <Command>`. |
| `PLAY0209` | Error | Two `form` blocks in the same document declare the same name. |
| `PLAY0210` | Error | A line in a `form` body is not `populate`, `field` or `on submit`. |
| `PLAY0211` | Error | A `populate` line is neither `populate via query ...` nor `populate from item`. |
| `PLAY0212` | Error | A `form` declares `populate` more than once. |
| `PLAY0213` | Error | A `field` line is not `field <property> [from <source>\|compose using <Callback>] [label "..."]`. |
| `PLAY0214` | Error | An `on submit` line is not `on submit navigate to <Screen> [by <param>]`. |
| `PLAY0215` | Error | A `form` declares `on submit` more than once. |
| `PLAY0216` | Warning | A `field` binds to a property its form's command does not declare. |

### Contributions

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0217` | Error | A `contribute` line is not `contribute to <ContributionPoint>`. |
| `PLAY0218` | Error | A line in a `contribute to` body is not `navigate`, `label` or `order`. |
| `PLAY0219` | Error | A contribution declares `navigate to` more than once. |
| `PLAY0220` | Error | A contribution declares `label` more than once. |
| `PLAY0221` | Error | A contribution declares `order` more than once. |
| `PLAY0222` | Error | A contribution's `label` line is not `label "..."` or `label $strings....`. |
| `PLAY0223` | Error | A contribution's `order` line is not `order <number>`. |
| `PLAY0224` | Warning | A contribution names a contribution point nothing in scope declares. |

### Themes

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0225` | Error | A `theme` line is not `theme <Name>`. |
| `PLAY0226` | Error | Two `theme` blocks in the same document declare the same name. |
| `PLAY0227` | Error | A line in a `theme` body is not `compatible with <Package>`. |
| `PLAY0228` | Error | A `theme` declares compatibility with the same package more than once. |
| `PLAY0229` | Error | A `ui profile`'s `theme` line is not `theme <Name>`. |
| `PLAY0230` | Error | A `ui profile` declares `theme` more than once. |
| `PLAY0231` | Warning | A `ui profile` selects a theme nothing in the document declares. |
| `PLAY0232` | Warning | A `ui profile` selects a theme not declared compatible with one of the profile's own packages. |

### Arrangement

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0233` | Error | An `arrangement` line is not `arrangement flow` or `arrangement freeform`. |
| `PLAY0234` | Error | A layout, screen template or dialog template declares `arrangement` more than once. |
| `PLAY0235` | Error | A `row`, `column` or `grid` line in an arrangement is malformed. |
| `PLAY0236` | Error | A slot leaf within an arrangement tree has malformed sizing attributes. |
| `PLAY0237` | Error | A `when` override line in an arrangement is not a valid width/height size-class condition. |
| `PLAY0238` | Error | An arrangement declares more than one `when` override for the same width/height size-class combination. |
| `PLAY0239` | Error | An `arrangement` block's body does not match its mode - a `flow` arrangement declares a `variant`, or a `freeform` arrangement declares anything other than one. |
| `PLAY0240` | Error | A `variant` line is not `variant width <compact\|regular>, height <compact\|regular>`. |
| `PLAY0241` | Error | An arrangement declares more than one `variant` for the same width/height size-class combination. |
| `PLAY0242` | Error | A `place` line is not `place <Slot> hidden` or `place <Slot> at x,y size w,h`. |
| `PLAY0243` | Error | A `variant` places (or hides) the same slot more than once. |
| `PLAY0244` | Warning | A `freeform` arrangement's `variant` does not mention (place or hide) a slot another variant of the same arrangement places. |

### Public event boundaries

These whole-model C# checks run after file assembly and also on programmatic syntax passed to the semantic binder.
Public metadata and explicit direction select ESM v9 once these checks pass; a model that fails them never binds.
Unknown and ambiguous references retain `PLAY0166` and `PLAY0198`, rather than being guessed private/local.
Private origins continue to use the existing invalid event/import declaration diagnostics.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0607` | Error | A command produces a public event. Move publication to an outbound Translate slice. |
| `PLAY0608` | Error | A local public event is produced outside an explicitly outbound Translate slice. |
| `PLAY0609` | Error | An outbound Translate slice produces zero or multiple distinct local public event types. Repeated productions of the same type count once. |
| `PLAY0610` | Error | A foreign public event is consumed outside an explicitly inbound Translate slice. |
| `PLAY0611` | Error | An outbound translation consumes a public or foreign event rather than a private local event. |
| `PLAY0612` | Error | An inbound translation produces a public or foreign event rather than a private local event. |
| `PLAY0613` | Error | A foreign public event is produced locally. Translate it to a private event instead. |
| `PLAY0614` | Error | A Translate slice declares or uses public event metadata without explicit direction. Legacy translations without public metadata remain historically inbound. C# and the editors repair it when exactly one direction fits. |
| `PLAY0615` | Error | An outbound translation contains an external-data capture instead of consuming private local events. |
| `PLAY0616` | Error | An explicitly inbound translation consumes a private or local event instead of a foreign public event. |
| `PLAY0617` | Error | An outbound translation produces a private event instead of its one local public event. |
| `PLAY0618` | Error | A projection or reducer targets an event (`projection X => SomeEvent`) outside an explicitly outbound Translate slice. |
| `PLAY0619` | Error | A capture reads `source events` outside an explicitly inbound Translate slice. |
| `PLAY0620` | Error | A `source events` block has no `from <Event>` line, a line that is not `from <Event>`, an invalid event name or a repeated event. |

The operational edges checked are seed appends, command and reaction productions (including refusal branches), capture appends,
projection event sources and joins/removals, reducers and constraints. Projection `all` includes every declared
contract; `every` only maps the projection's existing inputs. Specification fixtures are not operational edges.
A projection or reducer whose `=>` target resolves to an event (and not to a declared read model) is an event-target
projection: its target counts as an output of the slice and its `from`/`on` events as inputs, so `PLAY0609`, `PLAY0611`,
`PLAY0613` and `PLAY0617` apply to it, and `PLAY0618` reports one outside an explicit outbound translation. Each
`from <Event>` under `source events` counts as an input of the capture, so `PLAY0610`, `PLAY0614` and `PLAY0616` apply,
and `PLAY0619` reports `source events` outside an explicit inbound translation. Both forms bind and select ESM v9.
Existing syntax restrictions remain: qualified reaction triggers do not parse, and qualified event productions
still report `PLAY0497`. Programmatic qualified references are resolved and checked, not treated as private.
The TypeScript compiler and both editors report the same codes (see [editor diagnostics](editor-diagnostics.md)).

Repairs: only `PLAY0614` has one. It declares `direction inbound` or `direction outbound` on the slice, and is offered
only when exactly one direction is consistent with the slice's events and constructs (C# `propose-repair` and
`read-workspace` view `repairs` over MCP; the TypeScript quick fix in Monaco and VS Code). Every other code in this
section has no automatic repair because each one is a contract decision for the author, not a spelling:
`PLAY0607`, `PLAY0608`, `PLAY0613` and `PLAY0617` would change which events a slice publishes or produces;
`PLAY0609`, `PLAY0611`, `PLAY0612` and `PLAY0616` would change which events a translation consumes or produces, or its
direction; `PLAY0610`, `PLAY0615`, `PLAY0618` and `PLAY0619` would move a construct between slices or change its direction;
`PLAY0620` is malformed `source events` input with no single intended correction.

### Screen composition

These checks run on the assembled model. The source stays in the syntax tree, so an authoring tool can still show and repair it.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0621` | Error | An `exposure for <Owner>` header or one of its `property` lines is malformed, or names an unknown collection operation. |
| `PLAY0622` | Error | An exposure's owner is not a layout, screen template or dialog template. |
| `PLAY0623` | Error | Re-exposures of one component property form a cycle. |
| `PLAY0624` | Error | A `reexposes <Owner>` names an owner that does not expose the same component property. |
| `PLAY0625` | Error | An `instance` header or one of its `set`, `items`, `item` or item value lines is malformed. |
| `PLAY0626` | Error | An instance is not a screen, screen template or dialog template. |
| `PLAY0627` | Error | An instance stores a value for a component property no exposure exposes. |
| `PLAY0628` | Error | An instance uses `set` on a collection exposure, or `items` on a single-value exposure. |
| `PLAY0629` | Error | A template's `content <slot>` names a slot the template does not declare. |
| `PLAY0630` | Error | A navigation's `outlet <name>` names an outlet no layout, template or component declares. |
| `PLAY0631` | Error | A screen or scoped `template` assignment uses a template whose `scopes` exclude that scope. |
| `PLAY0632` | Error | A component comes from a package no `ui profile` declares. Only checked when at least one profile declares packages. |

None of these has an automatic repair: each is a composition decision for the author.

### Triggers

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0245` | Error | A `trigger` line is not `trigger <Name>`. |
| `PLAY0246` | Error | A line in a trigger body is neither a description nor a value the trigger provides. |
| `PLAY0247` | Error | The document declares two triggers by the same name, leaving no answer to which one a reaction means. |
| `PLAY0248` | Warning | A `when` line names neither an event nor a trigger the document or the compiler knows. |
| `PLAY0249` | Error | An `every` line is not `every <n> <seconds\|minutes\|hours\|days>`. |
| `PLAY0250` | Error | An `at` line is not `at <HH:mm>`, optionally followed by `on <Weekday>` or `on day <n>`. |
| `PLAY0251` | Warning | A reaction takes a value from an occurrence that the trigger does not provide. |
| `PLAY0252` | Error | A reaction states more than one `where`, and a reaction is narrowed by one condition. |
| `PLAY0253` | Error | A reaction declares the same trigger more than once, so the second says nothing the first did not. |
| `PLAY0450` | Error | A value line appears under an `every` or `at` reaction trigger; clock triggers take no values. |

### Layouts, screen templates and dialog templates

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0254` | Error | A `screen template` line is not `screen template <Name>`. |
| `PLAY0255` | Error | A `dialog template` line is not `dialog template <Name>`. |
| `PLAY0256` | Error | A `fits slot` line is not `fits slot <name>`. |
| `PLAY0257` | Error | A screen template declares `fits slot` more than once. |
| `PLAY0258` | Error | A layout or a dialog template declares `fits slot` - neither fills a slot of a parent structure. |
| `PLAY0259` | Error | A top level `layout` line is not `layout <Name>`. |
| `PLAY0260` | Error | A document declares more than one layout by the same name. |
| `PLAY0261` | Error | A `ui profile`'s `layout` line is not `layout <Name>`. |
| `PLAY0262` | Error | A `ui profile` declares `layout` more than once. |
| `PLAY0263` | Warning | A `ui profile` selects a layout nothing in the document declares. |

### File references

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0264` | Warning | A `file` directive names an absolute path, and a file reference is relative to the repository root. |

A path that does not resolve is **not** a diagnostic, at any severity. A document is read in a designer, in a
build and on a machine where the tree is not present, so the compiler never holds a path against a file system -
a stale path must not be what makes a valid document invalid. A consumer that *can* resolve paths decides for
itself what an unresolvable one means.

### Specification query results

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0265` | Error | A `then query` line does not name a query. |
| `PLAY0266` | Error | A line in a `then query` body is neither `arguments` nor `result`. |
| `PLAY0267` | Error | A `then query` assertion declares `arguments` more than once. |

### Semantic binding

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0268` | Error | Source syntax carries behavior the supported ESM profile cannot represent, including generated properties whose concepts declare validation rules, and unsupported scalar `$context` produces paths (tenant is not event namespace; claims and roles are not portable scalar values), an unsupported validation rule, concept `require`, command `require` or production conditions over read-model paths ([#129](https://github.com/Cratis/Screenplay/issues/129)), date/`today` conditions, non-deterministic `$env` conditions, `$context` tag values, and a bare named rule with no implementation body (see [Commands](commands.md#what-the-executable-model-admits)). Bodied named rules, command/concept code validation, and inline/file policy predicates bind as opaque ESM v3 attachments rather than reporting this diagnostic. |
| `PLAY0269` | Information | Source syntax is explicitly deferred from the current backend semantic profile. |
| `PLAY0270` | Information | Source syntax, including a valid persona, is realization or authoring/operational metadata rather than portable behavior. |
| `PLAY0271` | Information or error | Source syntax keeps its legacy meaning and cannot be strengthened into ESM v1 implicitly. |
| `PLAY0272` | Error | Source syntax requires an explicit reviewed semantic migration before binding. |
| `PLAY0273` | Error | Syntax and identity information cannot produce a coherent semantic compilation, including an event-source `for` assertion without one unambiguous required scalar command destination type or a `produces` mapping, `then` event expectation, projection mapping, or constraint naming a property absent from the current event revision, or an authorization policy, property rule or requirement referencing a generated value before generation. |
| `PLAY0274` | Error | A syntax location cannot be mapped to a supplied semantic source document. |

### Specification event-source assertions

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0275` | Error | A specification event-source assertion is a bare `for` and does not provide a value. |
| `PLAY0276` | Error | One specification command or event step declares its `for <value>` event-source assertion more than once. |

### Projection variants

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0277` | Error | A `variant` line is not `variant <Name>`. |
| `PLAY0278` | Error | A variant declares no `enters on` event, so nothing ever activates it. |
| `PLAY0279` | Error | An `enters on` line is not `enters on <EventType> [key <expression>]`. |
| `PLAY0280` | Error | A `variant` is declared inside another variant, and variants do not nest. |
| `PLAY0281` | Error | Two variants of the same projection declare the same name. |

### Opt-in completeness

These structural warnings run only when selected, after error-free whole-application source compilation. See [Completeness checks](completeness.md) for selection and exemptions and [editor diagnostic support](editor-diagnostics.md) for the TypeScript/C# boundary.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0530` | Warning (opt-in: `--check data-bindings`) | Visible screen bindings share a name but disagree on cardinality, resolved query or `by` parameter. Identical rebinding and sibling sections are allowed. |
| `PLAY0531` | Warning (opt-in: `--check data-bindings`) | A screen binding's resolved read model or cardinality differs from its query's return. Optional and observable qualifiers are ignored; unresolved or ambiguous names are skipped. |
| `PLAY0532` | Warning (opt-in: `--check input-surfaces`) | An action has no module-level command-bound form or issuing screen in the command's own slice. Post-action navigation does not supply input; a title-only screen does not issue a command. Commands with only generated properties (including no properties) require no typed input. |
| `PLAY0533` | Warning (opt-in: `--check input-surfaces`) | A StateChange command has no used screen action or attached behavior execute, and no reaction invokes it. A form alone is not an issuer; attached navigation or open dialog may reach an issuing screen. Automation and Translate commands are exempt. |
| `PLAY0534` | Warning (opt-in: `--check field-origins`) | A declared read model has no builder or performer, or a top-level field lacks an identity, mapping, compatible AutoMap source, child or nested target. Variants are checked independently, including entering-event AutoMap; the owning slice's keyed-query property counts as identity. Opaque builders and unknown coverage are skipped. |
| `PLAY0535` | Warning (opt-in: `--check query-keys`) | A query parameter cannot be held by its view's known identity or fields. Identity comes from the owning slice's unambiguous keyed-query property or structurally resolved projection keys. Performer-served views, tenant-context parameters and unknown identity types are skipped. |
| `PLAY0536` | Warning (opt-in: `--check event-consumers`) | The newest generation of a local event has no declared projection, reducer, reaction, constraint or interaction consumer. Specifications and production do not count. Imported external contracts are exempt; legitimate terminal facts may still be reported. |
| `PLAY0537` | Warning (opt-in: `--check navigation`) | A screen is unreachable from contributions or attached shell-level behaviors, including through screen actions, row clicks, behaviors, discovered forms or opened dialog templates. Unattached named behaviors and unreachable cycles do not establish entry points. With no entry points, one application finding reports that no screen is reachable. |

### Model consistency

These errors are reported by ordinary compilation, including compilation of a folder as one application; semantic binding is not required. References resolve from the innermost scope outward. Unknown or ambiguous declarations and imported shapes are not guessed.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0282` | Error | A declarative `validate` rule targets a field absent from the command's declared shape, including named rules with implementation blocks. A dotted path is followed through declared composite `type`s; continuing past a primitive, concept or enum field is reported, while a path through an undeclared or imported type is left undecided. |
| `PLAY0283` | Error | The type supplied by `reads <View> by <field>` is incompatible with every declared query `by` parameter of that view. Filter parameters do not substitute for a key. Parameter names may differ; distinct concepts remain distinct types. |
| `PLAY0284` | Error | A `children` or `nested` block never populates a declared element field through its identity, explicit mappings, inherited `every` mappings, nested blocks, joins, or compatible AutoMap. Coverage is across the block's events, not a requirement that every update event fill every field. |
| `PLAY0285` | Error | A specification's expected event contradicts every possible declared producer of its `when` command, using decidable literals, property copies and equality conditions. |
| `PLAY0286` | Error | A specification value is not a member of the enum declared by that specific command, event, read-model field or query parameter. Bare members, qualified members and quoted member names are accepted. |
| `PLAY0287` | Error | A command or reaction producer, a capture append mapping, or a specification's `given`/`then` event step, assigns a field absent from the referenced event's declaration. Dotted paths follow the same rule as `PLAY0282`: `title.missing` is reported when `title` is a primitive, concept or enum, `detail.missing` when `Detail` is a declared `type` without that field. A collection or optional composite field is addressed element-wise. |
| `PLAY0290` | Warning | An `import` names an event, command, read model, concept or type the application declares itself. The declaration is what every reference resolves to and what these checks see, so the import has no effect - remove it. |
| `PLAY0291` | Error | A mapping value starts with `{` or `[` but is not a valid single-line JSON object or list (keys must be quoted). |
| `PLAY0292` | Error | An object key does not name a property of the target's declared composite `type`. Unknown or imported types remain undecided. |
| `PLAY0293` | Error | A statically known value has the wrong shape for its target: a collection expects a list, a declared composite expects an object, and a scalar cannot accept a list or object. |
| `PLAY0294` | Error | An inline JSON object repeats a key (including inside a nested object or list); each property may be stated once. |

An `import` never changes what these checks see: a name the application declares resolves to that declaration whether or not it is also imported, and an imported name nothing here declares keeps an unknown shape.

These checks do not execute handlers, custom predicates or opaque expressions. An undeclared query signature does not establish a read-key mismatch. An unknown event shape under AutoMap, or an open `all` subscription with AutoMap, leaves projection coverage undecidable. Outcome checks compare explicit producer mappings only; they do not invent mappings for omitted fields.

For executable-model dispositions, see `PLAY0268`–`PLAY0271` above, [projection semantic-model admission](projections/semantic-model.md), [what the executable model admits for commands](commands.md#what-the-executable-model-admits), and [constraints](constraints.md).

### AST authoring

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0288` | Warning | An accepted AST authoring operation canonically prints a touched document. The message states how many comments could not be retained and on which lines; attached comments follow their syntax owners, whitespace is normalized, and untouched documents retain their exact bytes. |
| `PLAY0289` | Error | An empty authoring workspace has no source documents to compile to an executable model. You can still propose its first typed document. |

Source-authoring acceptance and executable readiness are separate verdicts. An authoring proposal validates the complete `.play` application and identity continuity without claiming that every language construct is supported by the executable backend profile. Executable-only workspace transactions remain strict.

### Event context paths

Every `$eventContext.<path>` - in a projection expression or in a dynamic dictionary key such as `countByType.$eventContext.eventType.id` - is checked against the [event context catalog](projections/event-context.md#available-properties). Chronicle resolves the path by reflection when it builds the projection and fails on one it cannot resolve, so this is the only place the mistake can be caught early. An unlisted member is a warning because a runtime may resolve more than the catalog lists; a path that can never resolve is an error.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0295` | Warning | An `$eventContext.<path>` opens with a member the event context does not have, such as `$eventContext.causationId`. A member resolves written camelCase or with only its first letter uppercased. |
| `PLAY0296` | Warning | An `$eventContext.<path>` continues into something the member before it does not have, such as `$eventContext.eventType.name`. The derived function `Week` is case-sensitive: `occurred.Week` resolves and `occurred.week` does not. |
| `PLAY0297` | Error | An `$eventContext.<path>` continues below `causation` or `tags`. They are collections with no addressing grammar, so a path below them never resolves. |
| `PLAY0298` | Error | An `$eventContext` reference names no member, as in `$eventContext.` or a dynamic key ending in `.$eventContext`, or has an empty segment. |
| `PLAY0299` | Warning | A dynamic dictionary key names a `$` source other than `$eventContext`, such as `byUser.$causedBy.subject`. Only `$eventContext.<path>` is resolved; any other source becomes the literal key. Write `byUser.$eventContext.causedBy.subject`. |

### Specification semantics

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0350` | Error | A command or event specification value is `null`; in Chronicle an optional fact is a separate event. Optional read-model values may be null. |
| `PLAY0351` | Error | A `given readmodel` or `then readmodel` block does not state the identifier inferred from its keyed query. A `then query … result` block may derive it from the argument. |
| `PLAY0352` | Error | A specification without `when` asserts an event or error, or has no `then query` or `then readmodel` outcome. |
| `PLAY0353` | Error | A specification value is null for a required property, or a nested command/event property is null; only optional read-model properties admit null. |
| `PLAY0354` | Error | A structured specification object omits a required property of its declared composite type. |
| `PLAY0355` | Error | The composite type declared for a structured specification value cannot be resolved. |
| `PLAY0356` | Error | A typed structured specification object repeats a member; inline JSON duplicates are caught earlier by `PLAY0294`. |
| `PLAY0357` | Error | An ESM message beginning with `$strings.` has no valid dotted key. Keys follow `.strings` assignments: an ASCII letter or underscore first, then word characters; subsequent segments contain one or more word characters. |
| `PLAY0358` | Error | A specification declares more than one `when` action (command or appended event). |
| `PLAY0359` | Error | `then events in any order` is malformed or repeated. |

### Interaction

The interaction model - behaviors, the `on` clauses that start them, the actions they run and the continuations those actions branch into. The fifty codes from `PLAY0300` upwards are reserved for this band as a whole, so a later addition lands beside its siblings rather than wherever there happened to be room.

A behavior is *deferred* from the backend ESM v1 profile in the same way every other UI construct is - see `PLAY0269`. Deferred does not mean droppable: the syntax tree, the printer and the semantic model carry every interaction construct in full, and a target that cannot realize one reports it rather than dropping it.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0300` | Error | A behavior declaration is not of the form `behavior <Name>`. |
| `PLAY0301` | Error | A behavior name is declared more than once. |
| `PLAY0302` | Error | A line in a behavior body is neither `description`, `parameter`, `order` nor an `on` binding. |
| `PLAY0303` | Error | A behavior parameter declaration is not of the form `parameter <name> [<Type>]`. |
| `PLAY0304` | Error | A behavior declares the same parameter name more than once. |
| `PLAY0305` | Error | A behavior `order` is not an integer. |
| `PLAY0306` | Warning | A behavior declares no bindings, so nothing it is attached to can run anything. |
| `PLAY0307` | Error | An `on` clause names neither a built-in interaction kind, an `event`, an `interval`, nor a declared application trigger. |
| `PLAY0308` | Error | An interaction binding declares no actions. |
| `PLAY0309` | Error | An interaction binding declares `where` more than once. |
| `PLAY0310` | Error | A line where an action was expected does not name one of the action kinds. |
| `PLAY0311` | Error | An `execute` action is not of the form `execute <Command>`. |
| `PLAY0312` | Error | A `navigate` action is neither `navigate to <Screen>` nor `navigate back`. |
| `PLAY0313` | Error | An `open dialog` action does not name a dialog template, or a `close` action is not `close dialog`. |
| `PLAY0314` | Error | A `refresh` action does not name a query. |
| `PLAY0315` | Error | A `set` action is not of the form `set <target> to <value>`. |
| `PLAY0316` | Error | A `notify` action is not of the form `notify <info\|warning\|error> "<text>"`. |
| `PLAY0317` | Error | A `confirm` action carries no message. |
| `PLAY0318` | Error | A `raise` action does not name an application trigger. |
| `PLAY0319` | Error | An action argument is not of the form `with <name> from <binding>`. |
| `PLAY0320` | Error | A continuation is attached to an action that cannot fail, so it could never run. `navigate`, `notify`, `set` and `close dialog` have no outcome to branch on. |
| `PLAY0321` | Error | An `on result` continuation is attached to something other than `open dialog`. |
| `PLAY0322` | Error | A `uses` clause is not of the form `uses <Behavior>`. |
| `PLAY0323` | Error | An argument at a `uses` site is not of the form `<parameter> <value>`. |
| `PLAY0324` | Error | Interaction nesting went deeper than the compiler admits. Extract the inner actions into a named behavior. |
| `PLAY0325` | Warning | An `interval` trigger is below the floor a client can usefully honour. |
| `PLAY0326` | Error | An application trigger is declared with a name reserved as a built-in interaction kind. `on <Name>` would mean the interaction and never the trigger, so the declaration would be unreachable. |
| `PLAY0330` | Warning | An action names a command the document does not declare. |
| `PLAY0331` | Warning | A `navigate to` action names a screen the document does not declare. |
| `PLAY0332` | Warning | A `refresh` action names a query the document does not declare. |
| `PLAY0333` | Warning | An `open dialog` action names a dialog template the document does not declare. |
| `PLAY0334` | Warning | A `raise` action or an `on` clause names an application trigger the document does not declare. |
| `PLAY0335` | Warning | An `on event` clause names an event the document does not declare. |
| `PLAY0336` | Warning | A `uses` clause names a behavior the document does not declare. |
| `PLAY0337` | Error | A `uses` site supplies an argument the behavior declares no parameter for. |
| `PLAY0338` | Error | A `uses` site leaves a behavior parameter without an argument. |
| `PLAY0339` | Warning | Actions follow an unconditional navigation, so they could never run. |
| `PLAY0340` | Warning | Another file of a folder repeats an attachment of the same `module` or `feature` - a `uses` of the same behavior with the same arguments, or an inline `on` block identical to one already attached. Only the first, in file-path order, is kept; the repeat is ignored and the warning names the file that attached it first. Folders written by earlier versions restate a module's or feature's attachments in every descendant file, and report this once per copy. |
| `PLAY0341` | Error | A guarded action child is not `when <condition> execute <Command>`, `otherwise hidden`, `otherwise execute <Command>` or `navigate to …`; navigation is repeated, or `otherwise hidden` has command arguments. |
| `PLAY0342` | Error | A label-headed guarded action declares no `when` alternatives. |
| `PLAY0343` | Error | A guarded action repeats `otherwise`, or declares a `when` alternative after it. |
| `PLAY0344` | Error | A guarded action condition does not compare an `item.<field>` path with a literal. Ordering operators require a number; `contains` and `starts with` require a string. |
| `PLAY0345` | Warning | An item path in a guarded action condition or input binding names no subject field, continues past a scalar, or crosses a collection-valued field. |
| `PLAY0346` | Warning | A guarded action has no nearest `data` subject, or multiple data directives tie in its nearest container. Field checks are skipped until the subject resolves. |
| `PLAY0347` | Warning | Earlier alternatives provably shadow an alternative. Guards use first-match order; proof uses DNF comparison-set inclusion with a 64-disjunct expansion cap. Overlap alone is not reported. |
| `PLAY0348` | Warning | A guarded alternative or `otherwise execute` supplies a `with` argument the chosen command does not declare, or binds a subject field whose collection cardinality differs from the command input. |

Guarded-action checks `PLAY0341`–`PLAY0348` are shared by the TypeScript compiler, Monaco and VS Code. Subject and command shapes that are imported but undeclared, or ambiguous, remain unknown rather than guessed. See [Editor diagnostic support](editor-diagnostics.md) for the remaining C#-only checks.

An inline `on` block is an anonymous behavior, so it has no name to report against. Diagnostics inside one cite the position and the trigger instead.

### Match validation binding

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0366` | Error | A bare named `matches` pattern is not defined; only `email` is defined. Use a quoted ECMAScript pattern for custom matching. |
| `PLAY0367` | Error | A quoted `matches` operand is not a valid ECMAScript regular expression. |
| `PLAY0368` | Error | A validation rule names a severity other than `information`, `warning` or `error`. |
| `PLAY0369` | Error | A `require` body has an invalid or repeated `severity` directive. |

### Projection binding

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0380` | Warning | A projection construct binds, but Chronicle's projection lowering drops part of it: `all` inside a `children` or `nested` block loses its subscription to every event type and behaves as `every`, and an `automap` or `no automap` on a joined event is replaced by the auto-map of the level the join sits in. |
| `PLAY0381` | Warning | A projection-level `key` is parsed but does not route events in Chronicle or the executable semantic model. Declare keys on each `from` (or its events). |
| `PLAY0382` | Error | A variant declares no `enters on` event. |
| `PLAY0383` | Error | A projection-level shared handler maps a property absent from a variant's known read-model shape. Unknown shapes remain undecided. |
| `PLAY0384` | Error | Two variants in one projection have the same name. |
| `PLAY0385` | Error | An entering event is claimed more than once in one projection. |
| `PLAY0386` | Error | A `given caller` line is not `authenticated`, `role "<name>"`, or `claim "<type>" = "<value>"`. |
| `PLAY0387` | Error | A specification declares more than one `given caller` block or `then denied` outcome. |
| `PLAY0388` | Error | `then denied` contains extra text or is mixed with another outcome. |
| `PLAY0389` | Error | A specification exercises an authorized command or query without a `given caller` fixture or `given caller as <Persona>`. |

### Constraints in the semantic model

These diagnostics cover [constraint](constraints.md) binding and the parser warning for file-backed constraints.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0390` | Error | A `unique` constraint names an event the application does not declare. |
| `PLAY0391` | Error | A `unique <property> on <Event>` constraint names a property the event does not declare. |
| `PLAY0392` | Error | Two constraints in the application share a name. The name is a constraint's identity in the event store - it keys the constraint's index and its violations - so it is unique across the whole application, not just its slice. |
| `PLAY0393` | Error | `ignore casing` is declared on a `unique event` constraint; it applies only to unique property values. |
| `PLAY0394` | Warning | Another file declares an identical `authorize` gate on the same module or feature. The first is kept and the repeated gate is ignored; distinct gates accumulate with AND. |
| `PLAY0396` | Warning | A `file <Path>` constraint names a Chronicle `IConstraint` class, which can only declare uniqueness. Use `unique ...` for portable uniqueness; put other rules in command validation or a `require` condition. |

### Inline code migration

Use an opening fence with an info string, such as ` ```csharp ` (without the spaces), for every inline implementation. Use ` ```text ` for multiline descriptions. The old language-line form, `validate csharp`, and bare description fences still parse during the deprecation window; printing a document converts them to the new form.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0397` | Warning | An inline code block uses a separate language line, `validate csharp`, or a multiline description uses a bare fence. The message names the tagged-fence replacement. |

A compiler-authored typed repair is offered only when `PLAY0397` identifies a
`validate csharp` header. It uses an identity replacement and canonical printing
to produce a tagged `` ```csharp `` fence. The entire file is reprinted; other
legacy forms and whitespace may change, but a repair that would drop any comment
is refused. Preview it through the revision-checked AST authoring contract (or
MCP `read-workspace` repairs / `propose-repair`); review the bytes before applying.
Other legacy forms are not individually offered as repairs.

### Repeated command or reaction-trigger reads

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0410` | Error | A command or reaction trigger reads the same view more than once without an alias on every instance. |
| `PLAY0411` | Error | Two reads in a command or reaction trigger use the same alias. |
| `PLAY0412` | Error | A reads alias collides with a command property or reaction trigger value. |

### Implementation attachment loading (host-supplied)

These warnings are returned by `AttachmentFiles.Load` for implementation files that remain `UnresolvedFile`. Syntax compilation alone does not read files.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0430` | Warning | An attachment path is absolute, drive-qualified, escapes the model root, or is not portable. |
| `PLAY0431` | Warning | An attachment or a directory on its path is a symbolic link or reparse point. |
| `PLAY0432` | Warning | An attachment or a directory on its path is missing. |
| `PLAY0433` | Warning | An attachment exceeds 2 MiB or the attachment set exceeds 8 MiB. |
| `PLAY0434` | Warning | An attachment cannot safely be read as UTF-8 text. |

### Executable semantic model

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0445` | Warning | A v1–v3 flat executable-model projection transition carries deprecated `ZeroOrOne` or `Many` affected-instance cardinality. Chronicle routes one key per transition; use a join for structural many. Returned by `ExecutableSemanticModel.DeprecationDiagnostics` for constructed or deserialized ESM, with a model-level location because ESM does not retain source positions. The source binder produces only `One`; query cardinality is unaffected. |
| `PLAY0449` | Error | A `given` fact needs a historical event shape (message names the event and revision), which Screenplay does not consume yet; or an event's source revision disagrees with its persisted catalog revision and requires explicit forward advancement. There is no fallback from current to historical properties. |

### File imports

See [Imports](imports.md).

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0454` | Error | An `import` inside a module or feature does not name files as `import "<path or glob>"`. |
| `PLAY0455` | Warning | A file import pattern with wildcards matches no `.play` file. |
| `PLAY0456` | Error | A file import names one file, without wildcards, and that file does not exist. |
| `PLAY0457` | Error | Two imports place the same file in a module or feature where neither lies inside the other. A file belongs in one place. |
| `PLAY0458` | Error | Imports keep placing a file deeper than 32 levels - they form a cycle. |
| `PLAY0459` | Error | A file imported into a module or feature declares a module other than the one it is placed in. Restating the module it is placed in is allowed. |
| `PLAY0460` | Error | The top level of a file imported into a module or feature holds something that scope cannot hold, such as a `screen template` in a file placed in a feature. |

### Refusal branches, redelivery and no-event assertions

Refusal branches, `$refusal` values, redelivery and `then no events` are syntax-only and not yet executable: source-valid models still fail binding with `PLAY0268` naming the unadmitted feature.

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0538` | Error | An `on refused` header is malformed, uses an unsupported selector, or appears outside an `invokes` block. Use `on refused [by validation \| by constraint [<Name>] \| by authorization]`. |
| `PLAY0539` | Error | A refusal branch is empty, repeats or adds children to `acknowledge`, combines acknowledgement with productions, or contains another kind of effect. Use `acknowledge` alone or one or more `produces <Event>` blocks. |
| `PLAY0540` | Warning | A refusal branch is shadowed by an earlier selector, or its declared constraint targets none of the invoked command's events. Bare refusal covers validation and constraints, not authorization. |
| `PLAY0541` | Error | A `$refusal` value is outside a branch's event mapping, has an unknown member, uses `constraint` outside a constraint selector, or targets an incompatible property type. The values `reason`, `constraint` and `message` are String values. |
| `PLAY0542` | Error | A named constraint in a refusal selector does not resolve to a declared constraint. |
| `PLAY0557` | Warning | An `on refused by authorization` branch invokes a command gated by its own, feature or module authorization without a declared invoking identity. With no caller the reference runner always refuses the command, while Arc runs reactor commands as the system. Declare an invoking identity once [#383](https://github.com/Cratis/Screenplay/issues/383) supports it; identity syntax is not available yet. |
| `PLAY0543` | Error | `when redelivered <Event> to <Reaction>` is malformed, or its values, optional `for` and route locator do not identify exactly one definitely matching given event occurrence with no undecidable candidates. Use `for`, values, `stream` or `no stream` to narrow the locator. |
| `PLAY0544` | Error | The redelivery reaction is unknown or ambiguous, or has no event trigger on the stated event. |
| `PLAY0545` | Error | `then no events` is malformed, repeated, has child mappings, follows `when append`, or accompanies event, event-order, error or denial expectations. Use one leaf assertion after a non-append action; read-model, query and response assertions may accompany it. |

See [Refusal branches](reactions.md#refusal-branches-syntax-only), [Redelivery specifications](specifications.md#redelivery-specifications-syntax-only) and [Specification syntax](specifications.md#syntax).

### Specification actions

See [Specifications](specifications.md#clocks-triggers-and-captures).

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0461` | Error | A `given clock` or `when clock` does not state one ISO 8601 instant with an offset or `Z`, or `given clock` is repeated. |
| `PLAY0462` | Error | A `when trigger` line is not `when trigger <Trigger>`. |
| `PLAY0463` | Error | A `given capture` or `when capture` line is not `<given\|when> capture <Capture>`. |
| `PLAY0464` | Error | A `when query` line is not `when query <Query>`, a `then result` line is not `then result [exactly]`, or `then no result` is malformed or repeated. |
| `PLAY0465` | Error | A query result is asserted without `when query`, `when query` asserts neither a result, `then no result` nor `then denied`, or both results and no result are asserted. |
| `PLAY0466` | Warning | A `when trigger` names a trigger nothing declares or registers, or a value its declaration does not carry. |
| `PLAY0467` | Warning | A `given capture` or `when capture` names a capture the application does not declare. |
| `PLAY0468` | Error | A `when query` argument is not a `by` or `filter` parameter of the query. |

### Inline event declarations and metadata

These codes cover [inline command events](commands.md#declare-an-event-inline) and [event metadata](events.md#authoring-metadata).

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0469` | Warning (inline), information (plain) | A production copies the command identifier into payload while targeting that same identifier. The inline-only repair removes the property and mapping, explicitly changes the event contract, retires its property address, and is excluded from fix-all. Consumers, opaque implementation impact or comment loss refuse the repair. Plain/standalone contracts receive guidance only: review persistence and generation evolution before changing their shape. |
| `PLAY0470` | Error | A command targets another event source but one or more productions omit `for`, or both an inline and a plain production omit `for` and therefore have different defaults. The diagnostic names the production. State every destination explicitly; cross-source execution is still unsupported. |
| `PLAY0471` | Information | An event's `id` equals its current name. The typed removal repair covers inline and standalone declarations, preserves the executable model and catalog, and refuses comment loss. Monaco and VS Code also offer an [editor quick fix](vscode.md#event-quick-fixes) to remove the complete line unless it has a trailing comment. |
| `PLAY0472` | Error | Event `id` is missing its nonempty quoted value or is repeated. |
| `PLAY0473` | Error | An inline event name collides with a standalone declaration, import, or another inline declaration. |
| `PLAY0474` | Error | `produces event` occurs outside a command, such as in a reaction. |
| `PLAY0475` | Error | An inline event declares `generation` in its header or body. Extract it before evolving generations. |
| `PLAY0476` | Error | A production supplies unescaped system-assigned `namespace`, `sequence`, `correlation`, `causation`, `causedBy`, or `occurred`, or an inline event supplies `origin`. |
| `PLAY0477` | Error | Event documentation is not one nonempty fenced Markdown block, or is repeated. |
| `PLAY0558` | Error | Module, feature, slice, command, read-model or reaction documentation is not one nonempty fenced Markdown block, or is repeated. |
| `PLAY0559` | Warning | Files give different documentation for one module or feature. The first documentation is kept. |
| `PLAY0572` | Error | A `given caller as <Persona>` reference is malformed, has body lines, or names an unknown top-level persona. Unknown references list declared personas. |
| `PLAY0573` | Error | A persona caller cannot be synthesized or verified against its policies. Binding names the persona, policy and refusal (`negation`, `nonLiteralClaim`, `roleClaim`, `opaqueImplementation`, `unresolvedPolicy`, `noPolicies`); use an explicit `given caller`. |
| `PLAY0574` | Warning | Opt-in `personas` check: none of a persona's policies gates a command, query or inherited screen scope. |
| `PLAY0575` | Warning | Opt-in `personas` check: an effective command or query gate definitely denies every declared persona's synthesized caller; unsynthesizable or undecidable callers are unknown. |
| `PLAY0576` | Information | Opt-in `personas` check: required atoms do not pin an `or` with multiple buildable alternatives. Names an unchosen alternative and suggests adding a policy that pins it. |

Malformed typed mappings and duplicate destinations retain `PLAY0044` and `PLAY0193`. Descriptions retain their existing diagnostics. Event descriptions, documentation, and rename pins are authoring-only metadata (`PLAY0270`); none changes canonical ESM bytes.

### Production destination advice and repairs

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0478` | Information | A plain `produces <Event>` omits `for` and its command has one required scalar identifier. Conditional productions and optional or collection identifiers are not reported. |

`PLAY0478` does not change routing or executable-model bytes. Its typed repair
replaces the production with one that explicitly states `for <identifier>`.
Accept it only when that event should address the command's identifier: a plain
omission can intentionally use an allocated identity, or inherit another
production's explicit destination. The repair is not offered for an optional or
collection identifier. It is also refused when the original or candidate has no executable model,
when the repair would change the language or semantic version, or when any other production's
effective destination would change (including through a command default).
Discovery verifies these conditions and comment preservation once per subject on an
immutable workspace snapshot; a new snapshot cannot reuse those verification results.

The verified `PLAY0478` repair is available through the
[MCP repair workflow](mcp/authoring-tools.md#fix-a-diagnostic).
Monaco and VS Code do not offer a quick fix for `PLAY0478` or `PLAY0470`.

For `PLAY0166` on a command production, a typed repair can add an `event`
declaration to the producing slice. Properties follow mapping order; command
property paths retain their types and concepts (including compliance markings),
and `$context.occurred` becomes `DateTime`. No generation, tags, subject or origin
is inferred. Unknown sources, literals, computations, reads, conflicting producer
shapes, imported or already-declared events, and cross-file producers have no
repair. Any workspace document with parser errors also blocks inference, because its partial
syntax may hide a contract or another producer. Reaction and capture producers are not inferred.

Both repairs require canonical formatting consent and refuse any dropped comment.
Discovery verifies authoring acceptance for `PLAY0166` too: an inferred event whose
fields conflict with a specification is not offered. Discovery reuses cached acceptance
and conflicts on the same immutable workspace snapshot; diagnostics are never cached.
Every proposal runs one fresh transaction, even after discovery. A matched repair
that fails returns that transaction's typed conflicts and full diagnostics; `UnknownRepair`
means no recipe matches the code and original subject, not a verification failure.
Discovery and preview never write source. Review the write plan and explicitly
accept it through the [workspace authoring contract](ast-authoring.md) or
[MCP repair workflow](mcp/authoring-tools.md#fix-a-diagnostic). Stale workspace or catalog
revisions are rejected. Source authoring and executable readiness remain separate;
for example, compliance attributes and nested mapping paths can require capabilities
the executable model does not yet admit.

### Optional values and spelling repairs

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0479` | Information | A type uses the [legacy optional suffix](types.md#compatibility-note). Write `optional` after the type. This does not fail `--warnaserror`. |
| `PLAY0480` | Error | `optional` follows `identifier`. Write the modifiers in the order `Type optional identifier`; command identifiers must still be required and scalar. |
| `PLAY0481` | Error | A read uses `optional`. Optional reads are not yet supported; their absence behavior is reserved for [#308](https://github.com/Cratis/Screenplay/issues/308). |

`query Q => observable?` is the sole exception: its `?` is the only spelling that preserves a one-shot query returning an optional scalar type named `observable`. It produces no `PLAY0479` and is excluded from occurrence repairs and document migrations. See [Queries](queries.md#observable-queries).

`PLAY0479` points at the complete type reference. VS Code marks it deprecated.
Its workspace repair replaces only the spelling at that occurrence; a document
repair combines all such replacements in one verified transaction. Both reparse
the result, require unchanged syntax structure, and check that the selected
information diagnostics disappear. Use `PreserveTrivia` to keep comments, alignment,
line endings and encoding. Canonical reprinting is a separate explicit choice.
Discovery and preview never write files; acceptance still requires current workspace
and catalog revisions. Monaco and VS Code also offer verified TypeScript quick fixes
for one occurrence or the entire document, without requiring .NET.

### Generated values and command responses

Generated properties, command responses, generated fixtures and return expectations are admitted as **ESM v7**. A pre-generation reference from authorization (including inherited/composed policies or a generated identifier's implicit subject), a property rule or a requirement reports `PLAY0273`: reference an input property or remove `generated`. A generated concept with declarative, named or code validation rules reports `PLAY0268`; v7 has no post-generation validation phase and never drops those rules. Missing generation fixtures bind, but reached generation returns `Unsupported(IdentityAllocation)` and cannot pass a specification. Other unadmitted constructs still block executable binding. See [commands](commands.md#generated-values-and-responses) and [fixtures](specifications.md#generated-fixtures-and-return-expectations).

For response and generated-fixture compatibility checks only, `Date` values must be quoted `yyyy-MM-dd` calendar dates. `DateTime` values must be quoted `yyyy-MM-ddTHH:mm:ss`, optionally followed by a decimal fraction of 1–7 digits, and always end in uppercase `Z` or an explicit `+HH:mm` / `-HH:mm` offset. Years range from 0001 to 9999; calendar days must exist, hours range from 00 to 23, and minutes and seconds from 00 to 59. Offsets range from `-14:00` to `+14:00`; at 14 hours the minutes must be 00. These checks do not change general literal parsing. Imported or unresolved value shapes remain unknown rather than being inferred from their names.

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0482` | Error | A generated property is declared outside a command. |
| `PLAY0483` | Error | A generated property is not a required, noncollection concept backed by `Uuid`. |
| `PLAY0484` | Error | Property modifiers repeat or are out of order. Write `Type optional generated identifier`. |
| `PLAY0485` | Error | A generated property is supplied as request or form input. |
| `PLAY0486` | Error | A command response is malformed, empty, repeated or conditional. |
| `PLAY0487` | Error | A response source does not reference one direct command property. |
| `PLAY0488` | Error | A response block repeats a field name. |
| `PLAY0489` | Error | A response uses a collection or whole read model, or an explicit field type differs from its source's type, collection shape or optionality. |
| `PLAY0490` | Error | A generated fixture repeats a target, names an unknown, nongenerated or identifier property, or supplies an incompatible or nonconcrete value. |
| `PLAY0491` | Error | A return expectation is malformed, repeated, incompatible with the response contract, lacks a command action, or accompanies an error or denial. |

### Handler implementation intent

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0492` | Error | An implementation wrapper has an operand, duplicate wrapper, unknown child or nested file child. |
| `PLAY0493` | Error | A hint is not one nonblank quoted string, or has children. |
| `PLAY0494` | Error | A handler mixes direct/wrapped sources, or a wrapper selects more than one file/inline payload. |

These wrapper diagnostics also apply to operation phases. Pending or attached intent is not executable admission.

### Operations and external systems

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0495` | Error | An external system declaration is malformed or repeated. |
| `PLAY0496` | Error | An operation declaration or phase is malformed, or supplies event-only metadata. |
| `PLAY0497` | Error | A production reference is ambiguous, or a qualified production does not resolve to an explicit operation. |
| `PLAY0498` | Error | An operation name collides with an event or another operation in its owning slice. |
| `PLAY0499` | Error | An operation is produced outside a command. |
| `PLAY0500` | Error | An operation must reference exactly one uniquely declared external system. |
| `PLAY0501` | Error | An operation input mapping is missing, repeated, unknown or incompatible with its declared type. |
| `PLAY0502` | Error | An operation specification step is malformed, unresolved, duplicated, incompatible with its command action or asserts undeclared compensation. |
| `PLAY0508` | Error | A numeric preamble has an unknown or malformed spelling, or a `numbers exact` document nests another `numbers` directive inside a declaration; only a top-level `numbers exact` is recognized. |
| `PLAY0509` | Error | A physical document repeats its numeric preamble. |
| `PLAY0510` | Error | A numeric preamble follows domain, imports or declarations. |
| `PLAY0511` | Error | A complete number cannot be represented exactly in the bounded Decimal domain. |
| `PLAY0512` | Error | Declaration-bearing physical documents or marked import barrels disagree on numeric mode. |
| `PLAY0513` | Error | Source options or inserted numeric values disagree with their owning mode. |

The numeric rows are syntax diagnostics. A valid `numbers exact` document still cannot bind: `PLAY0268` reports that exact numeric mode is not admitted by any supported executable model (ESM) version yet, and unmarked documents keep their existing numeric behavior.

These are syntax diagnostics. A valid system, operation or operation specification still cannot bind: `PLAY0268` reports that these constructs are not admitted by any supported executable model (ESM) version yet. [Operations](operations.md) do not trigger event destination or payload-identity diagnostics. A valid pending or attached handler remains unsupported independently. Diagnostic repairs for `PLAY0471` and `PLAY0479` are unchanged.

### Timeline order

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0516` | Information | A projection, reducer rule or named reaction trigger uses an event from a later slice, or a command/reaction `reads` a read model built by a later slice (builder preferred over declaring slice). Reported once per consumer slice and event or read model at its first reference. Reads whose builder's projections or reducers for the read model being read consume any event produced inside the reader's child at the reader/builder's lowest common container are excluded, including from cycle grouping. Projections with a matching variant count, including their shared blocks and variant transitions; unrelated projections and reducers in the same slice do not. Produced events include declarations and command/reaction `produces` targets, including imported events. A producer in the consumer's own sub-feature cannot be fixed by reordering. This does not fail `--warnaserror`. |
| `PLAY0517` | Information | A sibling group depends on each other's events or non-feedback read models, so reordering cannot make every dependency flow left to right. Reported once per mutually dependent group, at its earliest backward reference, instead of individual `PLAY0516` findings within that group. This does not fail `--warnaserror`. |

See [Timeline diagnostics](imports.md#timeline-diagnostics) for the ordering root, grouping rules, checked references and C#/MCP repair conditions. `PLAY0516` offers verified typed moves or explicit pins before a retained glob where safe; `PLAY0517` and own-sub-feature findings have no repair. These findings and repairs do not change executable behavior.

### Declared dependencies

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0552` | Warning | An opted-in container has uncovered counted references into a producer module. One finding per container and producer module, at the container header, with source evidence. If the producers' shared container is an invalid ancestor target, the finding lists their outermost covering-eligible features instead. |
| `PLAY0553` | Information | A valid declaration has no counted explicit reference, including provisional ambiguous coverage. Reported at the declaration. |
| `PLAY0556` | Information | Two containers declare each other. Reported on each declaring line. |
| `PLAY0554` | Warning | A `depends on` target is self, an ancestor, a descendant, or does not resolve to a module or feature. |
| `PLAY0555` | Warning | The same target is declared again on a container, in one file or across files. Resolved aliases count as repeats; unresolved targets compare by text. The first is kept. |

An ambiguous target uses `PLAY0198`, naming the equally near candidates. See [Declared dependencies](slices.md#declared-dependencies) for the sibling and qualified-name rules. Malformed statements are parse errors (`PLAY0022` in a module, `PLAY0024` in a feature).

### Event sources and command streams

| Code | Condition |
| --- | --- |
| `PLAY0503` | Invalid or duplicate source/stream declaration, including non-scalar types or invalid composite part declarations |
| `PLAY0504` | Missing or ambiguous source-owned stream, invalid scalar/composite key mapping or literal formatting, or known incompatible command identifier, production destination or key type |
| `PLAY0505` | Both the route and qualified value-property interpretations are viable; neither is selected |
| `PLAY0506` | Known scalar or composite part type needs an unsupported portable formatter; bare `Int` is not supported |
| `PLAY0507` | Redundant rename-only stored-name pin |
| `PLAY0547` | Invalid, empty, duplicated, conflicting or misplaced specification route/part block |
| `PLAY0548` | Specification command occurrence declares routing metadata |
| `PLAY0549` | Unresolved specification route or missing, unknown, duplicated, mismatched-shape, nonliteral, incompatible or unformattable stream id/part |
| `PLAY0550` | Missing or incompatible routed source identity, or ambiguous producer fallback without a source identifier |
| `PLAY0551` | Expected route contradicts its sole producer, the command under test |

Valid [source/stream authoring](event-sources.md) selects ESM v8. Executable mappings use direct required, non-collection, non-generated command properties or literals; property paths and handler commands still fail with `PLAY0268`. Editors and MCP preserve original source evidence; unknown imported type shapes are not guessed. There is no new routing or source-pin quick fix. Existing `PLAY0470`/`PLAY0478` repairs still refuse when executable before/after routing proof is unavailable.

### Negated claim targets

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0546` | Warning | Semantic binding applies a policy with a claim under `not`, directly or through grouping, whose target is an optional path, a command's `subject` without an identifier, or a non-string type. An undecidable target evaluates to unknown even under negation; a final unknown policy result denies. Reported at the authorization reference against that command's properties or query argument. The TypeScript syntax compiler does not perform this semantic check. |

See [Policies](policies.md#portable-evaluation) for three-valued evaluation and the distinction between a missing caller claim and a missing comparison target.

### Guarded interaction alternatives

| Code | Severity | Reported when |
| --- | --- | --- |
| `PLAY0560` | Error | Alternatives occur on a trigger other than click, double click or select. Submit and non-item triggers have no structured subject in this version. |
| `PLAY0561` | Error | A binding mixes alternatives with plain actions or an opaque `where` guard. |
| `PLAY0562` | Error | A `when` or `otherwise` branch has no actions. |
| `PLAY0563` | Error | An interaction uses the labeled-action one-line `when … execute …` spelling. C# offers a typed block-form repair. |
| `PLAY0564` | Warning / Information | Opaque `where` on click, double click or select is deprecated. A strict item condition warns and has a C# typed repair; other text is Information without a repair. Other triggers are unchanged. |

Both compilers check these forms. Repairs require individual review (`CanFixAll: false`) and refuse trailing-comment relocation or comment loss. Conditions reuse PLAY0344–PLAY0348; fallback ordering uses PLAY0343 and missing alternatives uses PLAY0342. See [Interactions](interactions.md#choose-an-action-list-by-item-state). Interactions still report PLAY0269 at ESM binding; syntax support is not runtime admission.

## Specification case tables

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0577` | Error | A parameter requires a name and property-line type, without a body or default. |
| `PLAY0578` | Error | A case requires an identifier and at most one inline assignment. |
| `PLAY0579` | Error | A parameter name is repeated within a table. |
| `PLAY0580` | Error | A case name is repeated within a table. |
| `PLAY0581` | Error | Tables require parameters and at least one case. |
| `PLAY0582` | Error | A case omits, repeats or invents a parameter assignment. |
| `PLAY0583` | Error | A case value is not concrete or cannot be normalized to its declared parameter type. |
| `PLAY0584` | Error | A case reference names no declared table parameter or occurs in an excluded position. |
| `PLAY0585` | Error | An optional parameter feeds a required target. |
| `PLAY0586` | Error | A derived specification name collides in its scope. |
| `PLAY0587` | Error | The parameter type is unknown or incompatible with its target type. |
| `PLAY0588` | Warning | A declared parameter is never referenced. |
| `PLAY0589` | Error | Singular effective expansion cannot represent a table; use `ExpandAll`. |

See [Named case tables](specifications.md#named-case-tables).

## Retired codes

A retired code stays out of use forever.

| Code | Retired in | Replacement |
|---|---|---|
| `PLAY0176` | v4.25.0 | Repeated reads without aliases are reported as `PLAY0410`. |
