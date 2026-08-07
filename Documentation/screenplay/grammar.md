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

ConceptRule    = RuleOp, [ "message", LocalizableString ], NL,
                   [ INDENT, RuleImplementation, DEDENT ] ;

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

(* An InlineBlock policy body compiles against PolicyContext and answers with
   a bool, exactly like the PolicyExpr it stands in for -
   see Documentation/screenplay/policies.md.                                  *)

PolicyExpr     = "require", PolicyCondition ;

PolicyCondition = PolicyAnd, { "or", PolicyAnd } ;

PolicyAnd      = PolicyOperand, { "and", PolicyOperand } ;

PolicyOperand  = "authenticated"
               | "role", StringLiteral
               | "claim", StringLiteral, "matches", ClaimTarget
               | "(", PolicyCondition, ")" ;

ClaimTarget    = "subject"
               | MappingSource ;

(* A quoted ClaimTarget is the literal value the claim must equal; every other
   MappingSource form - a path, "$context.", "$env." - names where
   the value to compare against is read from.                                 *)

(* Every condition in the language - a policy "require", a "produces when" -
   is the same grammar over different operands, and combines the same way:
   "and" binds tighter than "or", both are left associative, and parentheses
   override that. "a or b and c" therefore means "a or (b and c)", and
   "a or b or c" means "(a or b) or c" - what a general purpose language does.
   Printing writes the parentheses back wherever the grouping is not the one
   these rules produce, so a document always reads back as what it says.     *)

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

ProviderDecl   = "provider", Ident, [ "name", Ident ], NL ;

(* A provider names which identity provider signs users in, and nothing about
   how to reach one - an authority, a client id and its secret are what running
   the application needs to know rather than what it is, and they differ per
   environment while the document does not. "name" distinguishes two providers
   of the same kind, which a generic OpenId or OAuth provider needs.         *)

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
               | ReadModelDecl
               | ProjectionDecl
               | ReducerDecl
               | CaptureDecl
               | SpecificationDecl
               | ReactorDecl
               | ScreenDecl
               | ConstraintDecl ;

ReadModelDecl  = "readmodel", Ident, NL,
                 INDENT, [ DescriptionDecl ], { PropertyLine }, DEDENT ;

ReducerDecl    = "reducer", Ident, "=>", Ident, NL,
                 INDENT, [ DescriptionDecl ], { ReducerRule }, DEDENT ;

ReducerRule    = "on", Ident, NL,
                 [ INDENT, [ DescriptionDecl ], [ FileDirective | InlineBlock ], DEDENT ] ;

(* A read model declares what it is, never what composes it. Whatever builds it
   - a projection or a reducer - names it with "=>", so the arrow always points
   the same way and a reader follows one direction to find where state comes
   from. Exactly one thing may build a read model; two builders leave no answer
   to which produced the value in front of you.                              *)

(* A reducer is for the views a projection cannot express - current state plus
   an event gives the next state. Each rule reduces one event, inline or from a
   file, against ReducerContext. Its State is null on the first event, because
   nothing built the instance before the first fold.                         *)

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
                   { ReadsDecl },
                   [ AuthorizeDecl ],
                   { ValidateDecl },
                   ( { ProducesDecl } | HandlerDecl ),
                   [ ConcurrencyDecl ],
                 DEDENT ;

ReadsDecl      = "reads", Ident, [ "by", Ident ], NL ;

(* The read model a command consults before it decides. Declaring it puts the
   read model in scope for the rest of the command body, so a produces mapping
   can be fed from state - "consultantId = EngagementScope.consultantId" - and
   a validation rule can be stated against it. "by" names the command property
   the read model is looked up by, and is absent for a read model that is not
   looked up by a key.                                                       *)

ConcurrencyDecl = "concurrency", NL,
                 INDENT, { ConcurrencyDim }, DEDENT ;

ConcurrencyDim = "eventSource", NL
               | "sourceType", Ident, NL
               | "streamType", Ident, NL
               | "streamId", Ident, NL
               | "events", Ident, { ",", Ident }, NL ;

AuthorizeDecl  = "authorize", PolicyRequirement, NL ;

PolicyRequirement = PolicyAll, { "or", PolicyAll } ;

PolicyAll      = PolicyOperand, { [ "and" ], PolicyOperand } ;

PolicyOperand  = PolicyRef
               | "(", PolicyRequirement, ")" ;

PolicyRef      = Ident ;

(* Two policies written next to each other mean both, which is what "authorize
   A B" has always meant, and "and" says the same thing out loud. Combining is
   the language's one condition rule - "and" binds tighter than "or", both are
   left associative, parentheses override - so "A or B and C" groups here
   exactly as it groups in a policy. A requirement may continue on the next
   line at deeper indentation.                                              *)

ValidateDecl   = "validate", NL,
                   INDENT, { ValidationRule | RequireRule }, DEDENT
               | "validate", "csharp", NL, InlineBlock ;

ValidationRule = Ident, RuleOp, [ "message", LocalizableString ], NL,
                   [ INDENT, RuleImplementation, DEDENT ] ;

RequireRule    = "require", Condition, NL,
                   [ INDENT, "message", LocalizableString, NL, DEDENT ] ;

(* A rule about the whole artifact rather than one of its properties, and where
   a rule that guards the domain lands - "the month is not already started".
   Its operands are properties of the artifact, or paths into state a "reads"
   declaration brought into scope. The Condition is the one every construct
   shares, so "and" and "or" mean here what they mean in a policy.          *)

RuleOp         = "not empty"
               | "max", Number
               | "min", Number
               | ">", Value
               | ">=", Value
               | "<", Value
               | "<=", Value
               | "==", Value
               | "!=", Value
               | "length", "==", Number
               | "matches", ( "email" | StringLiteral )
               | "all", ">", Value
               | "all", ">=", Value
               | "rule", Ident ;

(* RuleImplementation is only meaningful after "rule", Ident - the other RuleOp
   forms are already fully declarative and take no implementation body. *)
RuleImplementation = FileDirective
                    | InlineBlock ;

(* A RuleImplementation and a "validate csharp" InlineBlock both compile against
   RuleContext. The rule implementation answers with a bool; the "validate csharp"
   block yields the message of every rule the artifact breaks -
   see Documentation/screenplay/context.md.                                  *)

Value          = Number | StringLiteral | "today" | "true" | "false" ;

(* -------------------------------------------------------------- *)
(* Produces                                                        *)
(* -------------------------------------------------------------- *)

ProducesDecl   = "produces", Ident, NL,
                   [ INDENT, [ ForDecl ], { TagDecl }, { PropertyMapping }, DEDENT ]
               | "produces", "when", Condition, NL,
                   INDENT, Ident, NL,
                   [ INDENT, [ ForDecl ], { TagDecl }, { PropertyMapping }, DEDENT ],
                   DEDENT ;

ForDecl        = "for", MappingSource, NL ;

(* Where the event lands. Absent, it lands on the command's own event source,
   which is the common case and stays unstated. A decision that appends to
   several event sources is several "produces", each saying where it goes -
   which is what the handler doing it already looks like.                    *)

(* Combines exactly as a policy condition does - see the note under Policies. *)

Condition      = ConditionAnd, { "or", ConditionAnd } ;

ConditionAnd   = ConditionOperand, { "and", ConditionOperand } ;

ConditionOperand = Ident, CompOp, Value
               | Ident, CompOp, Ident
               | "(", Condition, ")" ;

CompOp         = "==" | "!=" | ">" | ">=" | "<" | "<=" ;

PropertyMapping = [ "@" ], Ident, "=", MappingSource, NL ;

MappingSource  = Ident                         (* command property   *)
               | ContextPath
               | "$env.", Ident
               | "$strings.", Path
               | StringLiteral
               | Number
               | "true" | "false"
               | Expression ;

(* The context paths mirror the members of CommandContext / QueryContext -
   see Documentation/screenplay/context.md. Everything after
   "identity.claims." is the name of a claim and is not checked.             *)

ContextPath    = "$context.", ContextRoot, { ".", Ident } ;

ContextRoot    = "command" | "arguments" | "tenant" | "causedBy"
               | "causation" | "occurred" | "identity" ;

IdentityProp   = "id" | "name" | "userName" | "isAuthenticated"
               | "roles" | "claims" ;

Expression     = (* arithmetic / method-call expression — freeform *) ;

(* -------------------------------------------------------------- *)
(* Handler                                                         *)
(* -------------------------------------------------------------- *)

HandlerDecl    = "handler", NL,
                 INDENT, ( FileDirective | InlineBlock ), DEDENT ;

(* -------------------------------------------------------------- *)
(* Queries                                                         *)
(* -------------------------------------------------------------- *)

QueryDecl      = "query", Ident, "=>", [ "observable" ], TypeRef, NL,
                 [ INDENT,
                     [ DescriptionDecl ],
                     [ ByClause ],
                     { FilterClause },
                     [ ScopeDecl ],
                     [ AuthorizeDecl ],
                     [ PerformerDecl ],
                   DEDENT ] ;

(* "observable" qualifies the return type as a live read - the query keeps
   pushing as the read model changes. Without it the query answers once.      *)

ScopeDecl      = "scoped", "to", Ident, NL ;

(* What the caller sees, as distinct from who may call. Absent, a query is
   scoped to the tenant it runs for - the common case, so it stays unstated.
   "scoped to global" reaches past the tenant; "scoped to identity" narrows to
   the caller. The scope is a name rather than a closed set, because what
   scopes exist follows the identity model of whatever runs the document.   *)

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
                     { ProducesDecl },
                     { InvokesDecl },
                     [ FileDirective | InlineBlock ],
                   DEDENT ] ;

InvokesDecl    = "invokes", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

(* What the reaction sets off. "produces" is the same declaration a command
   carries, because appending an event is the same act wherever it happens.
   A command is not produced but asked for, so it is "invokes" - an event is a
   fact the reaction appends, a command is an intent it hands on, and something
   else may still reject it. One word for both would say those are the same
   kind of consequence.                                                      *)

(* -------------------------------------------------------------- *)
(* Screens                                                         *)
(* -------------------------------------------------------------- *)

(* A screen binds to a query, a command or another screen by name. A bare name
   resolves from the inside out - the slice it is written in, then the feature,
   then the module, then the document - and the innermost match wins, so a
   slice keeps its own vocabulary. A name matching two declarations equally
   well is a warning naming both, never a silent pick. Qualify with the scope
   that holds it - "Queue.All", "Preparation.Queue.All" - to reach across.  *)

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
LanguageTag    = "csharp" | "typescript" | "react" | "html" | "sql"
               | (* any language registered with the compiler *) ;

(* The five above are what the language ships with, and what the surrounding
   tooling understands end to end - a Stage renders them, an editor highlights
   them. A consumer adds to the set by handing the compiler a language
   registry; the compiler then carries a registered block as text without
   claiming to read it. See sub-languages.md.                                *)

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
| `query` | `=>` return type with optional `observable`, `by`/`filter`, `description` | `performer` |
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
