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
| `PLAY0025` | Error | A slot of a layout template is not an identifier. |
| `PLAY0026` | Error | A line in a layout body opens with a word a layout declares nothing by. |
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
| `PLAY0135` | Error | A constraint states more than one rule, and a constraint states one. |

### Reactors

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0136` | Error | A `reactor` line is not `reactor <Name>`. |
| `PLAY0137` | Error | A line in a reactor body is not `on <EventType>`. |
| `PLAY0138` | Error | A reactor observes no events, so nothing ever reaches it. |
| `PLAY0139` | Error | A line in a reactor trigger body opens with a word a trigger declares nothing by. |

### Validation rules

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0140` | Error | A `validate` line is neither `validate` nor `validate csharp`. |
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
| `PLAY0151` | Error | A $causedBy expression names a property the cause does not carry. |
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

### Layout arrangement

| Code | Severity | Reported when |
|---|---|---|
| `PLAY0233` | Error | A layout's `arrangement` line is not `arrangement flow` or `arrangement freeform`. |
| `PLAY0234` | Error | A layout declares `arrangement` more than once. |
| `PLAY0235` | Error | A `row`, `column` or `grid` line in a template is malformed. |
| `PLAY0236` | Error | A slot leaf within a template tree has malformed sizing attributes. |
| `PLAY0237` | Error | A `when` override line in a template is not a valid width/height size-class condition. |
| `PLAY0238` | Error | A template declares more than one `when` override for the same width/height size-class combination. |
| `PLAY0239` | Error | A layout's body does not match its declared (or default) `arrangement` - a `flow` layout declares a `variant` block, or a `freeform` layout declares a `template` block. |
| `PLAY0240` | Error | A `variant` line is not `variant width <compact\|regular>, height <compact\|regular>`. |
| `PLAY0241` | Error | A layout declares more than one `variant` for the same width/height size-class combination. |
| `PLAY0242` | Error | A `place` line is not `place <Slot> hidden` or `place <Slot> at x,y size w,h`. |
| `PLAY0243` | Error | A `variant` places (or hides) the same slot more than once. |
| `PLAY0244` | Warning | A `freeform` layout's `variant` does not mention (place or hide) a slot another variant of the same layout places. |

## Retired codes

None yet. When a code is retired it is listed here with the release it went in, and its number stays out of use forever.
