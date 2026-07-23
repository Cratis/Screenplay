# Grammar

The full EBNF grammar of the Screenplay DSL. `INDENT`/`DEDENT` are synthesized by the lexer from changes in indentation (offside rule), as in Python. The PDL and CDL bodies are embedded sub-grammars — see [Sub-language Pluggability](sub-languages.md).

```ebnf
(* ============================================================ *)
(* Screenplay DSL — Full EBNF                                    *)
(* ============================================================ *)

Document       = [ DomainDecl ], { Import }, { ConceptDecl }, { PolicyDecl }, { PersonaDecl }, { Module } ;

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
                   [ INDENT, { ConceptValidate }, DEDENT ]
               | "concept", Ident, ":", "Enum", NL,
                   INDENT, { Ident, NL }, { ConceptValidate }, DEDENT ;

ConceptValidate = "validate", NL,
                   INDENT, { ConceptRule }, DEDENT
               | "validate", "csharp", NL, InlineBlock ;

ConceptRule    = RuleOp, [ "message", StringLiteral ], NL ;

PrimitiveType  = "Uuid" | "String" | "Int" | "Decimal" | "Bool"
               | "Date" | "DateTime" ;

Attribute      = "@pii" | "@sensitive" ;

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

PropertyLine   = Ident, TypeRef, NL ;

TypeRef        = Ident, [ "[]" ], [ "?" ] ;

(* -------------------------------------------------------------- *)
(* Commands                                                        *)
(* -------------------------------------------------------------- *)

CommandDecl    = "command", Ident, NL,
                 INDENT,
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

ValidationRule = Ident, RuleOp, [ "message", StringLiteral ], NL ;

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

PropertyMapping = Ident, "=", MappingSource, NL ;

MappingSource  = Ident                         (* command property   *)
               | "$context.occurred"
               | "$context.identity.id"
               | "$env.", Ident
               | StringLiteral
               | Number
               | "true" | "false"
               | Expression ;

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
                     [ ByClause ],
                     { FilterClause },
                     [ AuthorizeDecl ],
                   DEDENT ] ;

ByClause       = "by", Ident, TypeRef, NL ;
FilterClause   = "filter", Ident, TypeRef, NL ;

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

SpecificationGiven = "given", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

SpecificationWhen = "when", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

SpecificationThen = "then", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "then", "error", StringLiteral, NL ;

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
                   "on", Ident, NL,
                   ( FileDirective | InlineBlock ),
                 DEDENT ;

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
               | "label", StringLiteral, NL ;

NavigateDecl   = "navigate", "to", Ident, [ "by", Ident ], NL ;

LayoutRef      = "layout", Ident, NL,
                 INDENT, { SlotDecl }, DEDENT ;

SlotDecl       = Ident, NL,
                 [ INDENT, { ScreenDirective }, DEDENT ] ;

SectionDecl    = "section", Ident, NL,
                 INDENT, { ScreenDirective | WidgetDecl }, DEDENT
               | "title", StringLiteral, NL ;

WidgetDecl     = ( "table" | "summary" ) , ( TypeRef | Ident ), NL,
                 [ INDENT, { WidgetOption }, DEDENT ] ;

WidgetOption   = "column", Ident, [ "label", StringLiteral ], NL
               | "field",  Ident, "label", StringLiteral, NL
               | "on", "row-click", NavigateDecl ;

(* -------------------------------------------------------------- *)
(* Shared                                                          *)
(* -------------------------------------------------------------- *)

DescriptionDecl = "description", StringLiteral, NL ;

FileDirective  = "file", FilePath, NL ;
FilePath       = (* relative path string *) ;

InlineBlock    = LanguageTag, NL, "```", NL, { AnyLine }, "```", NL ;
LanguageTag    = "csharp" | "typescript" | "react" | "html" ;

StringLiteral  = '"', { ? any char except '"' ? }, '"' ;
Number         = [ "-" ], { "0".."9" }, [ ".", { "0".."9" } ] ;
Ident          = Letter, { Letter | Digit | "_" } ;
Letter         = "A".."Z" | "a".."z" ;
Digit          = "0".."9" ;

NL             = ? newline ? ;
INDENT         = ? increase in indentation level ? ;
DEDENT         = ? decrease in indentation level ? ;
AnyLine        = ? any text until newline ? ;
```
