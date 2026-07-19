# Grammar (EBNF)

This document provides the complete formal grammar for the Change Data Capture Language (CDL) - the sub-language a `capture` block delegates to - using Extended Backus-Naur Form (EBNF) notation. It mirrors the [Projection Declaration Language grammar](../projections/grammar.md), which documents PDL the same way.

## EBNF Notation

EBNF is a notation for formally describing syntax. Here are the key elements used:

| Notation | Meaning | Example |
|----------|---------|---------|
| `=` | Definition | `Rule = "value" ;` |
| `;` | End of rule | `Rule = "value" ;` |
| `{ }` | Zero or more | `{ "item" }` matches "", "item", "itemitem", etc. |
| `[ ]` | Optional (zero or one) | `[ "optional" ]` matches "" or "optional" |
| `( )` | Grouping | `( "a" \| "b" )` matches "a" or "b" |
| `\|` | Alternative (or) | `"a" \| "b"` matches either "a" or "b" |
| `" "` | Terminal (literal) | `"keyword"` matches the text "keyword" |
| `,` | Sequence | `"a", "b"` matches "a" followed by "b" |

**Special Symbols:**
- `NL` - Newline
- `INDENT` - Increased indentation level
- `DEDENT` - Decreased indentation level
- `Ident` - Identifier (letter followed by letters, digits, or underscores)
- `Path` - Dotted property path (`Ident`, optionally followed by `.`, `Ident`, repeated)

## Complete Grammar

```ebnf
Document        = { Capture } ;

Capture         = "capture", Ident, NL,
                  [ INDENT,
                    { Directive },
                  DEDENT ] ;

Directive       = SourceBlock
                | KeyDirective
                | MapBlock
                | AppendBlock
                | ChildrenBlock
                | NestedBlock ;

SourceBlock     = "source", Ident, NL,
                  INDENT,
                    { SourceSetting },
                  DEDENT ;

SourceSetting   = Ident, LineValue, NL ;

KeyDirective    = "key", LineValue, NL ;

MapBlock        = "map", NL,
                  INDENT,
                    { MapOperation },
                  DEDENT ;

MapOperation    = SplitOperation
                | MapEntry ;

MapEntry        = Ident, "=", Expr,
                  [ "translate", NL,
                    INDENT,
                      { TranslateEntry },
                    DEDENT
                  ], NL ;

TranslateEntry  = StringLiteral, "=>", Ident, NL ;

SplitOperation  = "split", Expr, "by", StringLiteral, NL,
                  INDENT,
                    { Path, NL },
                  DEDENT ;

AppendBlock     = "append", Ident, NL,
                  INDENT,
                    ( WhenClause, INDENT, { PropertyMapping }, DEDENT
                    | { PropertyMapping } ),
                  DEDENT ;

WhenClause      = "when", WhenExpr, NL ;

WhenExpr        = "added"
                | "removed"
                | Template
                | Path, [ "from", SimpleValue, "to", SimpleValue
                        | "or", Path, { "or", Path }
                        | "and", Path, { "and", Path } ] ;

PropertyMapping = Path, "=", MappingSource, NL ;

ChildrenBlock   = "children", Ident, "identified", "by", Path, NL,
                  INDENT,
                    [ MapBlock ],
                    { AppendBlock },
                  DEDENT ;

NestedBlock     = "nested", Path, NL,
                  INDENT,
                    [ MapBlock ],
                    { AppendBlock },
                  DEDENT ;

Expr            = Template
                | Path ;

MappingSource   = SourceItemExpr
                | ContextExpr
                | EnvExpr
                | Literal
                | Path
                | RawExpression ;

SourceItemExpr  = "$.", Path ;               (* a value from the current source item *)
ContextExpr     = "$context.", Path ;        (* a value from the capture context, such as $context.occurred *)
EnvExpr         = "$env.", Ident ;           (* an environment variable *)

Template        = "`", { TemplateChar | "${", Expr, "}" }, "`" ;
TemplateChar    = (* any character except ` *) ;

SimpleValue     = StringLiteral | NumberLiteral | "true" | "false" | "null" | Ident ;

Literal         = BoolLiteral
                | StringLiteral
                | NumberLiteral
                | NullLiteral ;

BoolLiteral     = "true" | "false" ;
StringLiteral   = '"', { StringChar }, '"' ;
NumberLiteral   = [ "-" ], Digit, { Digit }, [ ".", Digit, { Digit } ] ;
NullLiteral     = "null" ;

RawExpression   = (* freeform expression captured verbatim *) ;
LineValue       = (* remainder of the line, captured verbatim *) ;

Path            = Ident, { ".", Ident } ;
Ident           = Letter, { Letter | Digit | "_" } ;

Letter          = "A" | "B" | ... | "Z" | "a" | "b" | ... | "z" ;
Digit           = "0" | "1" | "2" | ... | "9" ;
StringChar      = (* any character except " and newline *) ;
```

## Grammar Breakdown

### Document Structure

A document contains one or more captures:

```ebnf
Document = { Capture } ;
```

### Capture

A capture names the construct and contains directives:

```ebnf
Capture = "capture", Ident, NL,
          INDENT,
            { Directive },
          DEDENT ;
```

**Example:**

```cdl
capture LegacyInvoiceCapture
  source api
    api LegacyInvoicingApi
    route /invoices
    poll 5m
  key id
```

### Source Block

Declares where the captured data comes from. The source kind and its settings are both free-form identifiers - `api`, `webhook` and `message` are the source kinds in active use, each with its own conventional settings (`api`/`route`/`poll` for `api`, `path` for `webhook`, `topic` for `message`), but the grammar itself does not restrict either:

```ebnf
SourceBlock = "source", Ident, NL,
              INDENT,
                { SourceSetting },
              DEDENT ;

SourceSetting = Ident, LineValue, NL ;
```

**Example:**

```cdl
source webhook
  path /invoices
```

### Key Directive

Names the source property identifying an instance:

```ebnf
KeyDirective = "key", LineValue, NL ;
```

### Map Block

Reshapes source values before they reach an `append` - renaming, templating, translating and splitting:

```ebnf
MapBlock = "map", NL,
           INDENT,
             { MapOperation },
           DEDENT ;

MapOperation = SplitOperation
             | MapEntry ;
```

A `map` block may appear at the capture level, or nested inside a `children` or `nested` block, where it applies to each child/nested instance.

#### Map Entry

Renames a source value onto a property, assigns a template built from source values, or both feed a `translate` block of value substitutions:

```ebnf
MapEntry = Ident, "=", Expr,
           [ "translate", NL,
             INDENT,
               { TranslateEntry },
             DEDENT
           ], NL ;

TranslateEntry = StringLiteral, "=>", Ident, NL ;

Expr = Template
     | Path ;
```

**Examples:**

```cdl
map
  status = status translate
    "utkast" => draft
    "sendt"  => sent
  summary = `${status} invoice`
```

`status = status` is a plain rename (the source property and target property happen to share a name); `summary = \`...\`` is a template - the same backtick/`${...}` syntax `produces` and PDL expressions use.

#### Split Operation

Splits one source value into several target properties by a separator:

```ebnf
SplitOperation = "split", Expr, "by", StringLiteral, NL,
                 INDENT,
                   { Path, NL },
                 DEDENT ;
```

**Example:**

```cdl
split contactName by ","
  firstName
  lastName
```

### Append Block

Declares the event to append and the trigger condition:

```ebnf
AppendBlock = "append", Ident, NL,
              INDENT,
                ( WhenClause, INDENT, { PropertyMapping }, DEDENT
                | { PropertyMapping } ),
              DEDENT ;

PropertyMapping = Path, "=", MappingSource, NL ;
```

**Note:** when an `append` declares a `when` clause, its property mappings are indented one level deeper than `when` (the mappings belong to the trigger). An `append` may omit `when` entirely, in which case its property mappings sit directly under `append`.

#### When Clause

The trigger condition guarding an append:

```ebnf
WhenClause = "when", WhenExpr, NL ;

WhenExpr = "added"
         | "removed"
         | Template
         | Path, [ "from", SimpleValue, "to", SimpleValue
                 | "or", Path, { "or", Path }
                 | "and", Path, { "and", Path } ] ;
```

| Form | Meaning |
| --- | --- |
| `when added` | Appends when an item appears in the source. |
| `when removed` | Appends when an item disappears from the source. |
| `` when `<expression>` `` | Appends when a raw template-literal expression evaluates to true. The expression is captured verbatim, unparsed. |
| `when <Path>` | Appends when the named property changes. |
| `when <Path> from <value> to <value>` | Appends when the named property transitions from one specific value to another. |
| `when <Path> or <Path> { or <Path> }` | Appends when any of the named properties change. |
| `when <Path> and <Path> { and <Path> }` | Appends when all of the named properties change. |

`or` and `and` cannot be mixed within a single `when` clause - `when a or b and c` is a compile error.

### Children Block

Captures changes in a child collection, with an optional `map` applied to every child before its appends run:

```ebnf
ChildrenBlock = "children", Ident, "identified", "by", Path, NL,
                INDENT,
                  [ MapBlock ],
                  { AppendBlock },
                DEDENT ;
```

**Example:**

```cdl
children lineItems identified by lineNumber
  map
    productName = name
  append InvoiceLineItemAdded
    when added
      invoiceId  = $.id
      lineNumber = $.lineNumber
  append InvoiceLineItemRemoved
    when removed
      invoiceId  = $.id
      lineNumber = $.lineNumber
```

### Nested Block

Captures changes to a single nullable child object - unlike `children`, which manages a collection - with the same optional `map`:

```ebnf
NestedBlock = "nested", Path, NL,
              INDENT,
                [ MapBlock ],
                { AppendBlock },
              DEDENT ;
```

**Example:**

```cdl
nested billingContact
  map
    contactName = name
  append BillingContactUpdated
    when email
      invoiceId = $.id
      email     = $.email
```

### Mapping Sources

Values a property mapping can be assigned from:

```ebnf
MappingSource = SourceItemExpr
              | ContextExpr
              | EnvExpr
              | Literal
              | Path
              | RawExpression ;

SourceItemExpr = "$.", Path ;
ContextExpr    = "$context.", Path ;
EnvExpr        = "$env.", Ident ;
```

| Expression | Meaning |
| --- | --- |
| `$.<property>` | A value from the current source item. |
| `$context.occurred` | The capture timestamp. |
| `$env.<NAME>` | An environment variable. |
| `"literal"`, `42`, `true`, `null` | A literal value. |
| `<property>` | A property on the command/event context being mapped into. |

## Indentation Rules

The grammar uses indentation to define structure, the same as every other Screenplay sub-language:

1. **INDENT**: Increase indentation by one level (typically 2 spaces)
2. **DEDENT**: Decrease indentation by one level
3. **Consistent Spacing**: All indentation must use spaces (no tabs)
4. **Block Structure**: Each block's content must be indented from its declaration

## Validation Rules

Beyond the grammar, the parser reports these as compile errors:

1. `capture <Name>` must match a valid identifier.
2. Each `map` entry must be `<property> = <source> [translate]`.
3. `split <source> by "<separator>"` requires a quoted separator; each indented line under it must be a valid property path.
4. `append <EventType>` requires a PascalCase-looking event type, matching event naming elsewhere in Screenplay.
5. A `when` clause must be `added`, `removed`, a backtick template with a matching closing backtick, or a property path optionally followed by a single `from ... to ...` transition or a chain of `or`/`and` properties - `or` and `and` cannot mix.
6. `children <collection> identified by <key>` and `nested <property>` must match their header shape.
7. An unrecognized construct inside a capture, `children` or `nested` body is reported as an error and the offending block is skipped.

## Example Using Grammar

This example demonstrates how the rules above combine:

```cdl
capture LegacyInvoiceCapture
  source api
    api LegacyInvoicingApi
    route /invoices
    poll 5m
  key id
  map
    status = status translate
      "utkast"     => draft
      "sendt"      => sent
      "betalt"     => paid
      "kansellert" => cancelled
    split contactName by ","
      firstName
      lastName
  append InvoiceStatusChanged
    when status
      invoiceId = $.id
      status    = $.status
      changedAt = $context.occurred
  append InvoicePaidFromSent
    when status from "sent" to "paid"
      invoiceId = $.id
      paidAt    = $context.occurred
  children lineItems identified by lineNumber
    map
      productName = name
    append InvoiceLineItemAdded
      when added
        invoiceId  = $.id
        lineNumber = $.lineNumber
        quantity   = $.quantity
        unitPrice  = $.unitPrice
    append InvoiceLineItemRemoved
      when removed
        invoiceId  = $.id
        lineNumber = $.lineNumber
  nested billingContact
    map
      contactName = name
    append BillingContactUpdated
      when email
        invoiceId = $.id
        email     = $.email
```

This capture uses:
- A `source` block with `api`, `route` and `poll` settings
- A `map` block with a `translate` mapping and a `split` operation
- Two capture-level `append` blocks - a plain property-changed `when` and a value-transition `when`
- A `children` block with its own `map` and two appends (`when added` / `when removed`)
- A `nested` block with its own `map` and append

## See Also

- [Captures](../captures.md) - The Captures overview and vocabulary at a glance
- [Sub-language Pluggability](../sub-languages.md) - How CDL plugs into the Screenplay parser
- [Projection Declaration Language grammar](../projections/grammar.md) - The equivalent authoritative grammar for PDL
