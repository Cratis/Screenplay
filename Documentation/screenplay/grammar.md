# Grammar

The full EBNF grammar of the Screenplay DSL. `INDENT`/`DEDENT` are synthesized by the lexer from changes in indentation (offside rule), as in Python. The PDL and CDL bodies are embedded sub-grammars — see [Sub-language Pluggability](sub-languages.md).

```ebnf
(* ============================================================ *)
(* Screenplay DSL — Full EBNF                                    *)
(* ============================================================ *)

Document       = [ DomainDecl ], { Import }, { ConceptDecl }, { TypeDecl }, { PolicyDecl }, { PersonaDecl }, [ AuthenticationDecl ], { Module }, { SeedDecl } ;

(* -------------------------------------------------------------- *)
(* Domain                                                          *)
(* -------------------------------------------------------------- *)

DomainDecl     = "domain", QualifiedName, NL ;

(* -------------------------------------------------------------- *)
(* Imports                                                         *)
(* -------------------------------------------------------------- *)

Import         = "import", QualifiedName, NL ;
QualifiedName  = Ident, { ".", Ident } ;

(* -------------------------------------------------------------- *)
(* Concepts                                                        *)
(* -------------------------------------------------------------- *)

ConceptDecl    = "concept", Ident, ":", PrimitiveType, { Attribute }, NL,
                   [ INDENT, { AttributeReason }, { ConceptValidate }, DEDENT ]
               | "concept", Ident, ":", "Enum", { Attribute }, NL,
                   INDENT, { AttributeReason }, { [ "@" ], Ident, NL }, { ConceptValidate }, DEDENT ;

AttributeReason = AttributeName, "reason", StringLiteral, NL ;

ConceptValidate = "validate", NL,
                   INDENT, { ConceptRule }, DEDENT
               | "validate", "csharp", NL, InlineBlock ;

ConceptRule    = RuleOp, [ "message", LocalizableString ], NL ;

PrimitiveType  = "Uuid" | "String" | "Int" | "Decimal" | "Bool"
               | "Date" | "DateTime" ;

Attribute      = "@", AttributeName ;
AttributeName  = "pii" | "sensitive" ;

(* -------------------------------------------------------------- *)
(* Composite value types                                           *)
(* -------------------------------------------------------------- *)

TypeDecl       = "type", Ident, NL,
                 INDENT, [ DescriptionDecl ], PropertyLine, { PropertyLine }, DEDENT ;

(* -------------------------------------------------------------- *)
(* Policies                                                        *)
(* -------------------------------------------------------------- *)

PolicyDecl     = "policy", Ident, NL,
                 INDENT, PolicyBody, DEDENT ;

PolicyBody     = PolicyExpr
               | InlineBlock ;

PolicyExpr     = "require", PolicyCondition, { ( "or" | "and" ), PolicyCondition } ;

PolicyCondition = "authenticated"
               | "role", StringLiteral
               | "claim", StringLiteral, "matches", ( "subject" | StringLiteral )
               | "(", PolicyCondition, { ( "or" | "and" ), PolicyCondition }, ")" ;

(* -------------------------------------------------------------- *)
(* Personas                                                        *)
(* -------------------------------------------------------------- *)

PersonaDecl    = "persona", Ident, NL,
                 INDENT,
                   [ DescriptionDecl ],
                   { "policy", Ident, NL },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Authentication                                                  *)
(* -------------------------------------------------------------- *)

AuthenticationDecl = "authentication", NL,
                 INDENT, { ProviderDecl }, DEDENT ;

ProviderDecl   = "provider", Ident, NL,
                 [ INDENT, { ProviderSetting }, DEDENT ] ;

ProviderSetting = Ident, MappingSource, NL ;

(* -------------------------------------------------------------- *)
(* Module                                                          *)
(* -------------------------------------------------------------- *)

Module         = "module", Ident, NL,
                 INDENT,
                   [ DescriptionDecl ],
                   { LayoutDecl },
                   { Feature },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Layouts                                                         *)
(* -------------------------------------------------------------- *)

LayoutDecl     = "layout", Ident, NL,
                 INDENT,
                   "template", NL,
                   INDENT, { Ident, NL }, DEDENT,
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Features                                                        *)
(* -------------------------------------------------------------- *)

Feature        = "feature", Ident, NL,
                 INDENT,
                   [ DescriptionDecl ],
                   { Feature },
                   { SliceDecl },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Slices                                                          *)
(* -------------------------------------------------------------- *)

SliceDecl      = "slice", SliceType, Ident, NL,
                 INDENT, [ DescriptionDecl ], { SliceBody }, DEDENT ;

SliceType      = "StateChange" | "StateView" | "Automation" | "Translate" ;

SliceBody      = EventDecl
               | CommandDecl
               | QueryDecl
               | ProjectionDecl
               | CaptureDecl
               | SpecificationDecl
               | ReactorDecl
               | ScreenDecl
               | ConstraintDecl ;

(* -------------------------------------------------------------- *)
(* Events                                                          *)
(* -------------------------------------------------------------- *)

EventDecl      = "event", Ident, NL,
                 INDENT, { TagDecl }, { PropertyLine }, DEDENT ;

TagDecl        = "tag", TagValue, NL ;

TagValue       = Ident
               | StringLiteral
               | "$context.", Path
               | "$env.", Ident ;

Path           = Ident, { ".", Ident } ;

PropertyLine   = [ "@" ], Ident, TypeRef, [ "identifier" ], NL ;

(* "identifier" is only accepted on a command property, and on at most one of
   them - it marks the property a runtime resolves the event source id from.  *)

TypeRef        = Ident, [ "[]" ], [ "?" ] ;

(* -------------------------------------------------------------- *)
(* Commands                                                        *)
(* -------------------------------------------------------------- *)

CommandDecl    = "command", Ident, NL,
                 INDENT,
                   [ DescriptionDecl ],
                   { PropertyLine },
                   [ AuthorizeDecl ],
                   { ValidateDecl },
                   ( { ProducesDecl } | HandlerDecl ),
                   [ ConcurrencyDecl ],
                 DEDENT ;

ConcurrencyDecl = "concurrency", NL,
                 INDENT, { ConcurrencyDim }, DEDENT ;

ConcurrencyDim = "eventSource", NL
               | "sourceType", Ident, NL
               | "streamType", Ident, NL
               | "streamId", Ident, NL
               | "events", Ident, { ",", Ident }, NL ;

AuthorizeDecl  = "authorize", PolicyRef, { ( NL, PolicyRef ) | ( "or", PolicyRef ) }, NL ;

PolicyRef      = Ident ;

ValidateDecl   = "validate", NL,
                   INDENT, { ValidationRule }, DEDENT
               | "validate", "csharp", NL, InlineBlock ;

ValidationRule = Ident, RuleOp, [ "message", LocalizableString ], NL ;

RuleOp         = "not empty"
               | "max", Number
               | "min", Number
               | ">", Value
               | ">=", Value
               | "<", Value
               | "<=", Value
               | "==", Value
               | "length", "==", Number
               | "matches", ( "email" | StringLiteral )
               | "all", ">", Value
               | "all", ">=", Value ;

Value          = Number | StringLiteral | "today" | "true" | "false" ;

(* -------------------------------------------------------------- *)
(* Produces                                                        *)
(* -------------------------------------------------------------- *)

ProducesDecl   = "produces", Ident, NL,
                   [ INDENT, { TagDecl }, { PropertyMapping }, DEDENT ]
               | "produces", "when", Condition, NL,
                   INDENT, Ident, NL,
                   [ INDENT, { TagDecl }, { PropertyMapping }, DEDENT ],
                   DEDENT ;

Condition      = ConditionExpr, { ( "and" | "or" ), ConditionExpr } ;

ConditionExpr  = Ident, CompOp, Value
               | Ident, CompOp, Ident
               | "(" Condition ")" ;

CompOp         = "==" | "!=" | ">" | ">=" | "<" | "<=" ;

PropertyMapping = [ "@" ], Ident, "=", MappingSource, NL ;

MappingSource  = Ident                         (* command property   *)
               | ContextPath
               | "$env.", Ident
               | "$secrets.", Path
               | "$strings.", Path
               | StringLiteral
               | Number
               | "true" | "false"
               | Expression ;

(* The context paths mirror the members of CommandContext / QueryContext -
   see Documentation/screenplay/context.md.                                  *)

ContextPath    = "$context.", ContextRoot, { ".", Ident } ;

ContextRoot    = "command" | "arguments" | "tenant" | "causedBy"
               | "causation" | "occurred" | "identity" ;

Expression     = (* arithmetic / method-call expression — freeform *) ;

(* -------------------------------------------------------------- *)
(* Handler                                                         *)
(* -------------------------------------------------------------- *)

HandlerDecl    = "handler", NL,
                 INDENT, ( FileDirective | InlineBlock ), DEDENT ;

(* -------------------------------------------------------------- *)
(* Queries                                                         *)
(* -------------------------------------------------------------- *)

QueryDecl      = "query", Ident, "=>", TypeRef, NL,
                 [ INDENT,
                     [ DescriptionDecl ],
                     [ ByClause ],
                     { FilterClause },
                     [ AuthorizeDecl ],
                     [ PerformerDecl ],
                   DEDENT ] ;

ByClause       = "by", Ident, TypeRef, [ FromClause ], NL ;
FilterClause   = "filter", Ident, TypeRef, [ FromClause ], NL ;

(* "from" fills a parameter from the query context instead of the caller.     *)

FromClause     = "from", MappingSource ;

PerformerDecl  = "performer", NL,
                 INDENT, ( FileDirective | InlineBlock ), DEDENT ;

(* -------------------------------------------------------------- *)
(* Projections — PDL sub-language                                  *)
(* -------------------------------------------------------------- *)

ProjectionDecl = "projection", Ident, "=>", Ident, NL,
                 INDENT, PDLBody, DEDENT ;

PDLBody        = (* Projection Declaration Language grammar —
                    see https://cratis.io/chronicle/projections/
                    projection-declaration-language/grammar/ *) ;

(* -------------------------------------------------------------- *)
(* Captures — CDL sub-language                                     *)
(* -------------------------------------------------------------- *)

CaptureDecl    = "capture", Ident, NL,
                 INDENT, CDLBody, DEDENT ;

CDLBody        = (* Change Data Capture Language grammar - covers source/key/map
                    (including split), append/when (added, removed, template,
                    property, value-transition, or/and-chains), children and
                    nested - see Documentation/screenplay/captures/grammar.md *) ;

(* -------------------------------------------------------------- *)
(* Specifications — Given/When/Then sub-language                   *)
(* -------------------------------------------------------------- *)

SpecificationDecl = "specification", Ident, NL,
                 INDENT, { SpecificationGiven | SpecificationWhen | SpecificationThen }, DEDENT ;

SpecificationGiven = "given", [ "readmodel" ], Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

SpecificationWhen = "when", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

SpecificationThen = "then", [ "readmodel" ], Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "then", "error", [ StringLiteral ], NL ;

(* A bare "then error" states a rejection whose reason the specification does
   not name; the quoted form names it. Both may appear in one specification.  *)

(* -------------------------------------------------------------- *)
(* Event seeding                                                   *)
(* -------------------------------------------------------------- *)

SeedDecl       = "seed", NL,
                 INDENT, { SeedGroup }, DEDENT ;

SeedGroup      = "for", StringLiteral, NL,
                 INDENT, { SeedEvent }, DEDENT ;

SeedEvent      = Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

(* -------------------------------------------------------------- *)
(* Sub-language extension point                                    *)
(* -------------------------------------------------------------- *)

(* Any registered keyword not listed above may appear as a
   SliceBody construct. The parser delegates to the registered
   sub-parser for the indented body.                               *)

ExtensionConstruct = Ident, Ident, NL,
                     [ INDENT, { AnyLine }, DEDENT ] ;

(* -------------------------------------------------------------- *)
(* Constraints                                                     *)
(* -------------------------------------------------------------- *)

ConstraintDecl = "constraint", Ident, NL,
                 INDENT, ConstraintBody, DEDENT ;

ConstraintBody = "unique", Ident, "on", Ident, NL   (* unique property  *)
               | "unique", "event", Ident, NL         (* unique event     *)
               | FileDirective ;                       (* custom C#        *)

(* -------------------------------------------------------------- *)
(* Reactors                                                        *)
(* -------------------------------------------------------------- *)

ReactorDecl    = "reactor", Ident, NL,
                 INDENT,
                   [ DescriptionDecl ],
                   ReactorTrigger, { ReactorTrigger },
                 DEDENT ;

(* A trigger with no body is a complete statement of intent - the reactor
   observes the event. The file reference and the inline block are optional
   realization metadata.                                                      *)

ReactorTrigger = "on", Ident, NL,
                 [ INDENT,
                     [ DescriptionDecl ],
                     [ FileDirective | InlineBlock ],
                   DEDENT ] ;

(* -------------------------------------------------------------- *)
(* Screens                                                         *)
(* -------------------------------------------------------------- *)

ScreenDecl     = "screen", Ident, NL,
                 INDENT, ScreenBody, DEDENT ;

ScreenBody     = FileDirective                          (* full external file  *)
               | { ScreenDirective } ;                  (* declarative levels  *)

ScreenDirective = DataDecl
               | ActionDecl
               | SectionDecl
               | LayoutRef
               | InlineBlock ;

DataDecl       = "data", TypeRef, "via", "query", Ident,
                 [ "by", Ident ], NL ;

ActionDecl     = "action", Ident, NL,
                 [ INDENT, { ActionOption }, DEDENT ] ;

ActionOption   = NavigateDecl
               | "label", LocalizableString, NL ;

NavigateDecl   = "navigate", "to", Ident, [ "by", Ident ], NL ;

LayoutRef      = "layout", Ident, NL,
                 INDENT, { SlotDecl }, DEDENT ;

SlotDecl       = Ident, NL,
                 [ INDENT, { ScreenDirective }, DEDENT ] ;

SectionDecl    = "section", Ident, NL,
                 INDENT, { ScreenDirective | WidgetDecl }, DEDENT
               | "title", LocalizableString, NL ;

WidgetDecl     = ( "table" | "summary" ) , ( TypeRef | Ident ), NL,
                 [ INDENT, { WidgetOption }, DEDENT ] ;

WidgetOption   = "column", Ident, [ "label", LocalizableString ], NL
               | "field",  Ident, "label", LocalizableString, NL
               | "on", "row-click", NavigateDecl ;

(* -------------------------------------------------------------- *)
(* Shared                                                          *)
(* -------------------------------------------------------------- *)

DescriptionDecl = "description", ( StringLiteral | FencedText ), NL ;

FencedText     = NL, "```", NL, { AnyLine }, "```" ;

LocalizableString = StringLiteral
               | "$strings.", Path ;

FileDirective  = "file", FilePath, NL ;
FilePath       = (* relative path string *) ;

InlineBlock    = LanguageTag, NL, "```", NL, { AnyLine }, "```", NL ;
LanguageTag    = "csharp" | "typescript" | "react" | "html" | "sql" ;

StringLiteral  = '"', { StringChar }, '"' ;
StringChar     = ? any char except '"', '\' and newline ? | Escape ;
Escape         = "\", ( '"' | "\" | "n" | "r" | "t" ) ;
Number         = [ "-" ], { "0".."9" }, [ ".", { "0".."9" } ] ;
Ident          = Letter, { Letter | Digit | "_" } ;
Letter         = "A".."Z" | "a".."z" ;
Digit          = "0".."9" ;

NL             = ? newline ? ;
INDENT         = ? increase in indentation level ? ;
DEDENT         = ? decrease in indentation level ? ;
AnyLine        = ? any text until newline ? ;
```

## Declarative first — `file` is never required

Screenplay's workflow is *author the document first, then Stage performs it*. That only holds if the language can describe everything **before any code exists**, so the language guarantees one thing:

> **A document must be expressible — and meaningful — with zero `file` references.**

`file <path>` is **realization metadata**: a pointer attached once a slice has been implemented. It is an alternative to a declarative body, never the only way to give a construct meaning. Hand-authored documents precede code and *gain* `file` lines as slices get built; generated documents arrive with them already attached. Same language, two directions.

| Construct | Declarative story | Realization escape hatch |
| --- | --- | --- |
| `concept` / `type` | primitive or properties, attributes, `validate` | `validate csharp` |
| `command` | `produces` with mappings and conditions | `handler` |
| `query` | `=>` return type, `by`/`filter`, `description` | `performer` |
| `policy` | `require` conditions | inline `csharp` |
| `reactor` | `description` on the reactor and on each `on` trigger | `file` / inline block |
| `screen` | title, sections, tables, `data`, `action`, `navigate`, layout | `file` |
| `constraint` | `unique …` forms | `file` |
| `projection` / `capture` | fully declarative (PDL / CDL) | — |

So this is a complete, valid statement of intent for a reactor nobody has written yet:

```screenplay
reactor AcceptedInvitationProvisioner
  description "Provisions the account when an invitation to join is accepted"
  on InvitationAccepted
```

Any construct added to the language follows the same rule: declarative meaning first, code pointer optional.

## Keyword escape

Screenplay is line based: a block decides what a line is from its first word. That makes a handful of words reserved inside each block, and `description` or `tag` is an ordinary name for a domain field.

Most of the time shape settles it. The directives that take no operand cannot be confused with a property, so a line with property shape is a property:

```screenplay
command RegisterInvoice
  description String     // a property called description
  description "Registers a new invoice"   // the directive
```

The same holds for `validate`, `handler` and `concurrency`.

Where shape cannot settle it - `authorize CanManageInvoice` and `tag Audit` are legitimate directives *and* legitimate property lines - prefix the name with `@`:

```screenplay
command RegisterInvoice
  @authorize AuthorizationCode   // a property called authorize
  authorize  CanManageInvoice    // the directive

event InvoiceRegistered
  @tag TagType                   // a property called tag
  tag  audit                     // a static tag
```

The escape works wherever a name of your choosing meets a reserved first word - property lines, property mappings, enumeration values, and projection `from` mappings (`@key`, `@parent`). The `@` is not part of the name, and the printer puts it back when it is needed.

| Block | Reserved first words |
|---|---|
| `command` body | `authorize`, `produces` (`description`, `validate`, `handler` and `concurrency` resolve by shape) |
| `event` body | `tag` |
| mapping block | `tag` |
| projection `from` block | `key`, `parent` |
| enumeration `concept` body | `validate` |

An unescaped `tag Audit` or a bare `validate` enumeration value keeps the meaning it has always had - the directive - and the compiler warns that the line does not declare what it looks like.

## String escapes

A string literal carries `"` and `\` through the backslash escapes above, so a value survives the trip out to text and back:

```screenplay
description "He said \"hello\" loudly"
```

Only `\"`, `\\`, `\n`, `\r` and `\t` are recognized. Any other backslash sequence is kept verbatim - `\d` stays `\d` - which is what lets a regular expression operand read naturally:

```screenplay
invoiceNumber matches "^INV-\d{6}$"
```

The printer escapes on the way out, so a value holding a quote prints as `\"` and compiles back to the same value. That is what makes [printing](printing.md) the inverse of compiling.
