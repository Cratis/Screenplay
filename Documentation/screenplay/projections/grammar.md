# Grammar (EBNF)

This document provides the complete formal grammar for the Projection Declaration Language using Extended Backus-Naur Form (EBNF) notation.

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
- `TypeRef` - Type reference (identifier with optional dot notation)

## Complete Grammar

```ebnf
Document        = { Projection } ;

Projection      = "projection", Ident, [ "=>", TypeRef ], NL,
                  [ INDENT,
                    { ProjDirective | Block },
                  DEDENT ] ;

ProjDirective   = "no", "automap", NL
                | "sequence", Ident, NL
                | FileDirective
                | KeyDecl
                | CompositeKeyDecl ;

FileDirective   = "file", FilePath, NL ;
FilePath        = (* repository relative path, never absolute *) ;

Block           = EveryBlock
                | FromAllBlock
                | FromEventBlock
                | JoinBlock
                | ChildrenBlock
                | NestedBlock
                | RemoveWithBlock
                | RemoveWithJoinBlock
                | VariantBlock ;

VariantBlock    = "variant", Ident, NL,
                  INDENT,
                    EntersOnDecl, { EntersOnDecl },
                    { ProjDirective | Block },
                  DEDENT ;

(* A projection-level key is parsed but does not route events; put routing keys
   on each from block or its events. *)

EntersOnDecl    = "enters", "on", TypeRef, [ KeyInline ], NL ;

EveryBlock      = "every", NL,
                  INDENT,
                    [ "automap" | "no", "automap", NL ],
                    { MappingLine },
                    [ "exclude", "children", NL ],
                  DEDENT ;

FromAllBlock    = "all", NL,
                  [ INDENT,
                    [ "automap" | "no", "automap", NL ],
                    { MappingLine },
                  DEDENT ] ;

FromEventBlock  = "from", EventSpec, { ",", EventSpec },
                  NL,
                  [ INDENT,
                    [ ParentDecl ],
                    { MappingLine | KeyDecl | CompositeKeyDecl },
                  DEDENT ] ;

EventSpec       = TypeRef, [ "key", Expr ] ;

KeyInline       = "key", Expr ;

JoinBlock       = "join", Ident, "on", Ident, NL,
                  INDENT,
                    { WithEventBlock },
                  DEDENT ;

WithEventBlock  = "with", TypeRef, NL,
                  [ INDENT,
                      [ "automap" | "no", "automap", NL ],
                      { MappingLine },
                    DEDENT ] ;

ChildrenBlock   = "children", Ident, "identified", "by", Expr, NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { ChildBlock },
                  DEDENT ;

ChildBlock      = ChildEveryBlock
                | FromEventBlock
                | JoinBlock
                | RemoveWithBlock
                | RemoveWithJoinBlock
                | ChildrenBlock
                | NestedBlock
                | ClearWithBlock ;

NestedBlock     = "nested", Ident, NL,
                  INDENT,
                    [ "automap" | "no", "automap", NL ],
                    { ProjDirective | Block | NestedBlock | ClearWithBlock },
                  DEDENT ;

ClearWithBlock  = "clear", "with", TypeRef, NL ;

ChildEveryBlock = "every", NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { MappingLine },
                  DEDENT ;

RemoveWithBlock = "remove", "with", TypeRef, [ KeyInline ], NL,
                  [ INDENT,
                      [ ParentDecl ],
                    DEDENT ] ;

RemoveWithJoinBlock
               = "remove", "via", "join", "on", TypeRef, [ KeyInline ], NL ;

KeyDecl         = "key", Expr, NL ;

CompositeKeyDecl
               = "key", TypeRef, [ "{" ], NL,
                  INDENT,
                    KeyPart, { NL, KeyPart }, NL?,
                  DEDENT,
                 [ "}", NL ] ;

KeyPart         = Ident, "=", Expr ;

ParentDecl      = "parent", Expr, NL ;

MappingLine     = Assignment
                | ClearLine
                | IncLine
                | DecLine
                | CountLine
                | AddLine
                | SubLine ;

Assignment      = Target, "=", Expr, NL
                | "set", Target, ( "=" | "to" ), Expr, NL ;

ClearLine       = "clear", Target, NL ;

IncLine         = "increment", Target, NL ;
DecLine         = "decrement", Target, NL ;
CountLine       = "count",     Target, NL ;

AddLine         = "add",      Target, "by", Expr, NL ;
SubLine         = "subtract", Target, "by", Expr, NL ;

Target          = [ "@" ], Path, [ DynamicKey ] ;
DynamicKey      = ".", "$eventContext", ".", EventContextPath ;   (* one entry per distinct value *)

Expr            = Template
                | "literal", Literal
                | Literal
                | DollarExpr
                | Path ;

DollarExpr      = "$eventSourceId"
                | "$eventContext", ".", EventContextPath
                | "$causedBy", [ ".", Path ] ;

EventContextPath = Ident, { ".", Ident }, [ "()" ] ;   (* checked against the event context catalog *)

Path            = Ident, { ".", Ident } ;

Template        = "`", { TemplateChar | "${", Expr, "}" }, "`" ;
TemplateChar    = (* any character except ` *) ;

Literal         = BoolLiteral
                | StringLiteral
                | NumberLiteral
                | NullLiteral ;

BoolLiteral     = "true" | "false" ;
StringLiteral   = '"', { StringChar }, '"' ;
NumberLiteral   = [ "-" ], Digit, { Digit }, [ ".", Digit, { Digit } ] ;
NullLiteral     = "null" ;

TypeRef         = Ident, { ".", Ident } ;
Ident           = Letter, { Letter | Digit | "_" } ;

Letter          = "A" | "B" | ... | "Z" | "a" | "b" | ... | "z" ;
Digit           = "0" | "1" | "2" | ... | "9" ;
StringChar      = (* any character except " and newline *) ;
```

## Grammar Breakdown

### Document Structure

A document contains one or more projections:

```ebnf
Document = { Projection } ;
```

### Projection

A projection defines the read model and contains directives and blocks:

```ebnf
Projection = "projection", Ident, "=>", TypeRef, NL,
             INDENT,
               { ProjDirective | Block },
             DEDENT ;
```

**Example:**

```pdl
projection User => UserReadModel
  from UserCreated
    Name = name
```

**Note:** AutoMap is enabled by default. Use `no automap` to disable it.

### Directives

Projection-level directives:

```ebnf
ProjDirective = "no", "automap", NL
              | "sequence", Ident, NL
              | FileDirective
              | KeyDecl
              | CompositeKeyDecl ;
```

`file` names the repository relative file the projection is realized by, so a document can be navigated back to the code it describes. It is additive rather than an alternative — a projection still declares its blocks. The compiler never resolves the path, so one that has gone stale does not make the document invalid.

```pdl
projection User => UserReadModel
  file Users/UserProjection.cs
  from UserCreated
    Name = name
```

### Blocks

Main building blocks of a projection:

```ebnf
Block = EveryBlock
      | FromAllBlock
      | FromEventBlock
      | JoinBlock
      | ChildrenBlock
      | RemoveWithBlock
      | RemoveWithJoinBlock ;
```

### Every Block

Apply mappings to all events:

```ebnf
EveryBlock = "every", NL,
             INDENT,
               [ "no", "automap", NL ],
               { MappingLine },
               [ "exclude", "children", NL ],
             DEDENT ;
```

### From Event Block

Handle specific events:

```ebnf
FromEventBlock = "from", EventSpec, { ",", EventSpec },
                 NL,
                 [ INDENT,
                   [ ParentDecl ],
                   { MappingLine | KeyDecl | CompositeKeyDecl },
                 DEDENT ] ;

EventSpec = TypeRef, [ "key", Expr ] ;
```

**Note:** AutoMap for from blocks is controlled at the projection or children level, not within individual from blocks.

### Join Block

Enrich with joined events:

```ebnf
JoinBlock = "join", Ident, "on", Ident, NL,
            INDENT,
              { WithEventBlock },
            DEDENT ;

WithEventBlock = "with", TypeRef, NL,
                 [ INDENT,
                     [ "automap" | "no", "automap", NL ],
                     { MappingLine },
                   DEDENT ] ;
```

**Note:** Each `with` block may toggle AutoMap for its own mappings with `automap` or `no automap`.

### Children Block

Define nested collections:

```ebnf
ChildrenBlock = "children", Ident, "identified", "by", Expr, NL,
                INDENT,
                  [ "no", "automap", NL ],
                  { ChildBlock },
                DEDENT ;

ChildBlock = ChildEveryBlock
           | FromEventBlock
           | JoinBlock
           | RemoveWithBlock
           | RemoveWithJoinBlock
           | ChildrenBlock
           | NestedBlock
           | ClearWithBlock ;

ChildEveryBlock = "every", NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { MappingLine },
                  DEDENT ;
```

**Note:** `ChildEveryBlock` applies mappings to all events within the children collection. Unlike the top-level `EveryBlock`, it does not support the `exclude children` directive as it operates within a children context.

### Nested Block

Define a single nullable child object on the parent — unlike `children`, which manages a collection. A `nested` block must contain at least one `from` directive.

```ebnf
NestedBlock = "nested", Ident, NL,
              INDENT,
                [ "automap" | "no", "automap", NL ],
                { ProjDirective | Block | NestedBlock | ClearWithBlock },
              DEDENT ;

ClearWithBlock = "clear", "with", TypeRef, NL ;
```

A `nested` block may appear at the projection level, inside a `children` block, or inside another `nested` block. The `clear with` directive nulls the nested object when the given event occurs.

### Removal Blocks

Remove instances based on events:

```ebnf
RemoveWithBlock = "remove", "with", TypeRef, [ KeyInline ], NL,
                  [ INDENT,
                      [ ParentDecl ],
                    DEDENT ] ;

RemoveWithJoinBlock = "remove", "via", "join", "on", TypeRef, [ KeyInline ], NL ;
```

### Variant Block

Declare one of several mutually exclusive named read models sharing the projection's identity - see [Variants](variants.md):

```ebnf
VariantBlock = "variant", Ident, NL,
               INDENT,
                 EntersOnDecl, { EntersOnDecl },
                 { ProjDirective | Block },
               DEDENT ;

EntersOnDecl = "enters", "on", TypeRef, [ KeyInline ], NL ;
```

**Note:** A variant must declare at least one `enters on` - the event(s) allowed to create it. Every other event a variant subscribes to, and every block declared at the projection level outside any `variant`, is update-only for that variant: it can bring an already-active instance up to date, but never create or resurrect one. A `variant` cannot itself contain another `variant`.

### Key Declarations

Define instance keys:

```ebnf
KeyDecl = "key", Expr, NL ;

CompositeKeyDecl = "key", TypeRef, [ "{" ], NL,
                   INDENT,
                     KeyPart, { NL, KeyPart }, NL?,
                   DEDENT,
                   [ "}", NL ] ;

KeyPart = Ident, "=", Expr ;
```

### Mapping Lines

Operations that modify the read model:

```ebnf
MappingLine = Assignment
            | ClearLine
            | IncLine
            | DecLine
            | CountLine
            | AddLine
            | SubLine ;

Assignment = Target, "=", Expr, NL
           | "set", Target, ( "=" | "to" ), Expr, NL ;
ClearLine = "clear", Target, NL ;
IncLine = "increment", Target, NL ;
DecLine = "decrement", Target, NL ;
CountLine = "count", Target, NL ;
AddLine = "add", Target, "by", Expr, NL ;
SubLine = "subtract", Target, "by", Expr, NL ;

Target = [ "@" ], Path, [ DynamicKey ] ;
DynamicKey = ".", "$eventContext", ".", EventContextPath ;
```

**Note:** A `Target` is a dotted path on the read model, so a mapping can reach a property of a nested object. A leading `@` escapes a name that collides with a keyword. A target ending in a `DynamicKey` - `count eventCountByType.$eventContext.eventType.id` - writes under a dictionary key taken from the event context, so the property holds one entry per distinct value; see [Event Context](event-context.md#in-dynamic-dictionary-keys). Only `$eventContext` resolves in a key: another `$` source is kept as literal key text and warned about (`PLAY0299`).

**Note:** `ClearLine` removes the value a property holds, and takes a dotted `Target` so a property on a nested object can be cleared. It is a mapping line, distinct from `ClearWithBlock` — `clear with <EventType>` — which nulls a whole child object. The older `Assignment` spelling `<property> = null` still parses, and parses as an assignment of the null literal.

### Expressions

Values and references:

```ebnf
Expr = Template
     | "literal", Literal
     | Literal
     | DollarExpr
     | Path ;

DollarExpr = "$eventSourceId"
           | "$eventContext", ".", EventContextPath
           | "$causedBy", [ ".", Path ] ;

EventContextPath = Ident, { ".", Ident }, [ "()" ] ;

Path = Ident, { ".", Ident } ;

Template = "`", { TemplateChar | "${", Expr, "}" }, "`" ;

Literal = BoolLiteral
        | StringLiteral
        | NumberLiteral
        | NullLiteral ;
```

**Note:** `EventContextPath` is a dotted path into the event's metadata, such as `$eventContext.eventType.id` or `$eventContext.causedBy.subject`. Every segment is checked against the [event context catalog](event-context.md#available-properties): an unlisted member is a warning, a path below the collections `causation` and `tags` is an error. The trailing `()` is only meaningful on the derived function `Week` (`$eventContext.occurred.Week()`).

`literal` in front of a literal states explicitly that the value is a constant - most often a fixed key, `key literal "global"` (see [Keys](keys.md)).

`$causedBy` on its own, or followed by a path the catalog lists below `causedBy` - `subject`, `name`, `userName`, or `onBehalfOf` and its own members - is the short form of `$eventContext.causedBy`. It parses, but Chronicle cannot yet evaluate it when the projection runs ([Cratis/Chronicle#4119](https://github.com/Cratis/Chronicle/issues/4119)) - write `$eventContext.causedBy.<property>` instead.

`$eventSourceId` and `$eventContext.eventSourceId` are distinct expressions that resolve to the same value; see [Event Context](event-context.md#eventsourceid-and-eventcontexteventsourceid).

## Indentation Rules

The grammar uses indentation to define structure:

1. **INDENT**: Increase indentation by one level (typically 2 spaces)
2. **DEDENT**: Decrease indentation by one level
3. **Consistent Spacing**: All indentation must use spaces (no tabs)
4. **Block Structure**: Each block's content must be indented from its declaration

**Example:**

```pdl
projection User => UserReadModel    # Level 0
  from UserCreated                  # Level 1 (INDENT)
    Name = name                     # Level 2 (INDENT)
    Email = email                   # Level 2
                                    # (DEDENT, DEDENT)
```

## Validation Rules

Beyond the grammar, these semantic rules apply:

1. Event types must exist or be valid identifiers
2. Properties referenced must exist on events and read models
3. Type compatibility between expressions and target properties
4. Numeric operations only on numeric properties
5. `children` blocks must declare `identified by`
6. `remove via join` requires an available join key
7. Composite keys must contain at least one field
8. Parent keys required in children's from and remove blocks
9. `nested` blocks must contain at least one `from` directive
10. `variant` blocks must declare at least one `enters on` event
11. `variant` blocks cannot nest inside one another
12. Two variants of the same projection cannot share a name
13. A projection-level (shared) handler must map only properties every variant that has them declares - a variant lacking a mapped property is a declaration error

## Formatting Conventions

While not enforced by the grammar, these conventions improve readability:

1. **Indentation**: 2 spaces per level
2. **Blank Lines**: Between top-level blocks
3. **Alignment**: Align `=` signs when helpful
4. **Ordering**: Logical grouping of related mappings

## Example Using Grammar

This example demonstrates how various grammar rules combine:

```pdl
projection Order => OrderReadModel
  every
    LastUpdated = $eventContext.occurred
    exclude children

  from OrderPlaced key orderId
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    Status = "Pending"

  from OrderShipped
    Status = "Shipped"
    ShippedAt = $eventContext.occurred

  join Customer on CustomerId
    with CustomerCreated
      CustomerName = name
    with CustomerUpdated
      CustomerName = name

  children items identified by lineNumber
    every
      UpdatedAt = $eventContext.occurred

    from LineItemAdded key lineNumber
      parent orderId
      ProductId = productId
      Quantity = quantity
      UnitPrice = price

    remove with LineItemRemoved key lineNumber
      parent orderId

  remove with OrderCancelled
```

This projection uses:

- Projection declaration
- Every block with exclude children at the projection level
- Multiple from blocks with keys
- Join block with multiple with blocks
- Children block with:
  - Child every block for common mappings across all child events
  - Nested from and remove blocks
- Projection-level removal

## See Also

- [Variants](variants.md) - Mutually exclusive named read models sharing one projection identity
- [Expressions](expressions) - Understanding expression syntax
- All other topic pages for specific features described in the grammar
