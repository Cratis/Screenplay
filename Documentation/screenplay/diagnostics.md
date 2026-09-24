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
| `PLAY0005` | Error | An `import` line is not `import <Qualified.Name>`. |
| `PLAY0006` | Warning | A line is indented with tabs, and Screenplay decides nesting from spaces. |

### Concepts

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0007` | Error | A `concept` line is not `concept <Name> : <Type>`. |
| `PLAY0008` | Error | A concept is declared over a primitive the language does not have. |
| `PLAY0009` | Error | A value of an enumeration concept is not an identifier. |
| `PLAY0010` | Error | A line in a concept body opens with a word a concept declares nothing by. |
| `PLAY0011` | Warning | A value of an enumeration is called `validate`, which the concept body reads as an empty validate block. |
| `PLAY0012` | Error | A concept gives the reason for an attribute it does not carry. |
| `PLAY0013` | Error | A concept gives the reason for one attribute more than once. |

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
| `PLAY0018` | Error | An `event` line is not `event <Name>`. |
| `PLAY0019` | Error | A property of an event is marked as the identifier, and an event never carries its event source id. |
| `PLAY0020` | Warning | A property called `tag` is read by the event body as a static tag rather than as a property. |

### Modules, features, slices and layouts

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0021` | Error | A `module` line is not `module <Name>`. |
| `PLAY0022` | Error | A line in a module body opens with a word a module declares nothing by. |
| `PLAY0023` | Error | A `feature` line is not `feature <Name>`. |
| `PLAY0024` | Error | A line in a feature body opens with a word a feature declares nothing by. |
| `PLAY0025` | Error | A slot declared by a layout, screen template or dialog template is not an identifier optionally followed by `contributes`. |
| `PLAY0026` | Error | A line in a layout, screen template or dialog template body opens a block none of them declares anything by. |
| `PLAY0027` | Error | A `slice` line is not `slice <Type> <Name>`. |
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
| `PLAY0045` | Error | A handler names neither a `file` nor an inline code block. |
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
| `PLAY0092` | Error | A specification document holds a top level line that does not open a `specification`. |
| `PLAY0093` | Error | A specification document declares no specification at all. |
| `PLAY0094` | Error | A `specification` line is not `specification <Name>`. |
| `PLAY0095` | Error | A line in a specification body opens with a word a specification declares nothing by. |
| `PLAY0096` | Error | A `when` line is not `when <CommandType>`. |
| `PLAY0097` | Error | A specification issues more than one command, and a specification is one example. |
| `PLAY0098` | Error | A `then error` line is neither `then error` nor `then error "<reason>"`. |
| `PLAY0099` | Error | A `given readmodel` or `then readmodel` line does not name a read model type. |
| `PLAY0100` | Error | A `given` or `then` line does not name an event type. |
| `PLAY0101` | Error | A value a specification step states is not `<property> = <value>`. |

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
| `PLAY0166` | Warning | An event is referred to that nothing in the document or its imports declares. |
| `PLAY0167` | Warning | A policy is referred to that nothing in the document declares. |
| `PLAY0168` | Error | A concept and a type, or two of either, are declared under one name. |
| `PLAY0169` | Error | An authentication block declares two providers under one name. |
| `PLAY0170` | Error | A seed block seeds nothing. |
| `PLAY0171` | Error | A concurrency block narrows nothing. |

### A folder compiled as one application

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0172` | Error | Two files of a folder each declare something the application has at most one of. |
| `PLAY0173` | Error | Two files of a folder declare the same name. |
| `PLAY0174` | Warning | Two files of a folder describe the same thing differently, and the first description is kept. |

### What a command reads to decide

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0175` | Error | A `reads` line is not `reads <ReadModel> [as <alias>] [by <property>]`, or uses `as`, `by`, or `reads` as an alias. |
| `PLAY0177` | Warning | A command reads a read model no projection in the document produces. |
| `PLAY0178` | Warning | The `by` of a `reads` declaration does not name a property of the command. |

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
| `PLAY0198` | Warning | A bare name matches more than one declaration at the same depth, so which one it means is undecided. |

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
| `PLAY0268` | Error | Source syntax carries portable behavior ESM v1 cannot represent, including unsupported scalar `$context` produces paths (tenant is not event namespace; claims and roles are not portable scalar values), an unsupported validation rule, concept `require`, command `require` or production conditions over read-model paths (#129), date/`today` conditions, non-deterministic `$env` conditions, `$context` tag values, and code validation (see [Commands](commands.md#what-the-executable-model-admits)). |
| `PLAY0269` | Information | Source syntax is explicitly deferred from the current backend semantic profile. |
| `PLAY0270` | Information | Source syntax is realization or operational metadata rather than portable behavior. |
| `PLAY0271` | Information or error | Source syntax keeps its legacy meaning and cannot be strengthened into ESM v1 implicitly. |
| `PLAY0272` | Error | Source syntax requires an explicit reviewed semantic migration before binding. |
| `PLAY0273` | Error | Syntax and identity information cannot produce a coherent semantic compilation, including an event-source `for` assertion without one unambiguous required scalar command destination type. |
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
| `PLAY0389` | Error | A specification exercises an authorized command or query without an explicit `given caller` fixture. |

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

### Repeated command reads

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0410` | Error | A command reads the same view more than once without an alias on every instance. |
| `PLAY0411` | Error | Two reads in a command use the same alias. |
| `PLAY0412` | Error | A reads alias collides with one of the command's property names. |

## Retired codes

A retired code stays out of use forever.

| Code | Retired in | Replacement |
|---|---|---|
| `PLAY0176` | v4.25.0 | Repeated reads without aliases are reported as `PLAY0410`. |
