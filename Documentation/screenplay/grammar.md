# Grammar

> Systems, operations, operation phases and their specification forms below are syntax-only authoring. These constructs are not admitted by any supported executable model (ESM) version yet (`PLAY0268`); see [Operations and external systems](operations.md). A phase source or wrapper is not an admitted executable implementation role.

> [Event sources, source-owned streams and command stream routes](event-sources.md) are authoring-only. These constructs are not admitted by any supported executable model (ESM) version yet; binding reports `PLAY0268`. Per-event overrides, observer filters, new concurrency flags, occurrence time and constraint scopes are not part of this increment.

The Screenplay syntax reference in EBNF. `INDENT`/`DEDENT` represent indented bodies: parsers read lines at greater indentation until the body ends. PDL and CDL have their own [sub-grammars](sub-languages.md). The C# compiler validates the full language; the TypeScript compiler models a subset and recognizes the remaining shipped constructs as opaque bodies.

Declarations and body directives can appear in any order unless a rule below states otherwise. A repeated group such as { A | B } means its members may appear in any order; it does not allow repeating singleton directives such as description, for or where. References may name declarations later in the document; scope and semantic checks still apply.

```ebnf
(* ============================================================ *)
(* Screenplay DSL — Full EBNF                                    *)
(* ============================================================ *)

Document       = [ DomainDecl ], { Import | ConceptDecl | TypeDecl | PolicyDecl
               | PersonaDecl | AuthenticationDecl | TriggerDecl | ThemeDecl
               | LayoutDecl | UiProfileDecl | BehaviorDecl | SystemDecl | EventSourceDecl | ExampleDecl | Module | SeedDecl } ;

(* At most one domain and authentication block. Put domain first; the compiler
   reports PLAY0004 when it follows another application declaration. *)

(* A document a FileImport placed in a module or feature holds, besides the
   declarations above, the body of that module or feature at its top level - see
   Module and Feature below, and imports.md.                                   *)

(* -------------------------------------------------------------- *)
(* Domain                                                          *)
(* -------------------------------------------------------------- *)

DomainDecl     = "domain", QualifiedName, NL ;

(* -------------------------------------------------------------- *)
(* Imports                                                         *)
(* -------------------------------------------------------------- *)

Import         = "import", QualifiedName, NL
               | FileImport ;
QualifiedName  = Ident, { ".", Ident } ;

FileImport     = "import", '"', ImportPattern, '"', NL ;
ImportPattern  = ? a path or glob relative to the importing file's folder -
                   "**" any number of folders, "*" within one, "?" one character, ".." climbs ? ;

(* A quoted operand imports .play files; an unquoted one keeps naming a contract
   from another bounded context. At the top level a file import brings in whole
   documents. Inside a module or feature it places each imported file there: the
   file's top level is that module's or feature's body, and also holds whatever
   belongs to the application as a whole. A file matched by several imports is
   imported once, at the deepest placement - see imports.md.                  *)

(* -------------------------------------------------------------- *)
(* External systems — syntax-only                                  *)
(* -------------------------------------------------------------- *)

SystemDecl     = "system", Ident, NL,
                 [ INDENT, { DescriptionDecl }, DEDENT ] ;

(* Systems are application-scoped, including in placed files. They name external
   systems without provider types or abilities. Systems, operations and their
   specification steps are authoring-only: binding rejects them with PLAY0268;
   these constructs are not admitted by any supported ESM version yet. *)

(* -------------------------------------------------------------- *)
(* Event sources and streams — syntax-only                         *)
(* -------------------------------------------------------------- *)

EventSourceDecl = "eventsource", Ident, NL,
                  [ INDENT, { EventDescriptionDecl | EventIdDecl
                            | SourceIdentifierDecl | EventStreamDecl }, DEDENT ] ;
SourceIdentifierDecl = "identifier", QualifiedName, NL ;
EventStreamDecl = "stream", Ident, NL,
                  [ INDENT, { EventDescriptionDecl | EventIdDecl | StreamIdentifierDecl }, DEDENT ] ;
StreamIdentifierDecl = "streamId", QualifiedName, NL ;

(* Sources belong to the application; streams belong to their physical parent.
   A duplicate parent makes its children's ownership ambiguous. Identifier and
   stream-id types are nonoptional scalars. Known stream-id types are limited to
   text and UUID values and their nominal concepts, plus integer-backed concepts;
   bare Int is rejected and unavailable imported shapes remain unresolved.
   No formatter is executed. Description, rename-only id and identifier/streamId
   directives appear at most once per declaration.
   Pins retain old stored names only, not semantic ids. *)

(* -------------------------------------------------------------- *)
(* Concepts                                                        *)
(* -------------------------------------------------------------- *)

ConceptDecl    = "concept", Ident, ":", PrimitiveType, { Attribute }, NL,
                   [ INDENT, { FileDirective | AttributeReason | ConceptValidate }, DEDENT ]
               | "concept", Ident, ":", "Enum", { Attribute }, NL,
                   INDENT, { FileDirective | AttributeReason | [ "@" ], LowerIdent, NL | ConceptValidate }, DEDENT ;

AttributeReason = AttributeName, "reason", StringLiteral, NL ;

ConceptValidate = "validate", NL,
                   INDENT, ( { ConceptRule } | InlineBlock ), DEDENT ;

ConceptRule    = RuleOp, [ "severity", ValidationSeverity ], [ "message", LocalizableString ], NL,
                   [ INDENT, RuleImplementation, DEDENT ] ;

PrimitiveType  = "Uuid" | "String" | "Int" | "Decimal" | "Bool"
               | "Date" | "DateTime" ;

Attribute      = "@", AttributeName ;
AttributeName  = "pii" | "sensitive" ;

(* -------------------------------------------------------------- *)
(* Composite value types                                           *)
(* -------------------------------------------------------------- *)

TypeDecl       = "type", Ident, NL,
                 INDENT, { DescriptionDecl | FileDirective | PropertyLine }, DEDENT ;
(* A type must have at least one property. *)

(* -------------------------------------------------------------- *)
(* Policies                                                        *)
(* -------------------------------------------------------------- *)

PolicyDecl     = "policy", Ident, NL,
                 INDENT, PolicyBody, DEDENT ;

PolicyBody     = PolicyExpr
               | InlineBlock
               | FileDirective ;

(* An InlineBlock or FileDirective policy body implements the same bool answer
   against PolicyContext as the PolicyExpr it stands in for -
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
                   { DescriptionDecl | "policy", Ident, NL },
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
(* Themes and ui profiles                                          *)
(* -------------------------------------------------------------- *)

ThemeDecl      = "theme", Ident, NL,
                 INDENT, { CompatibleWithDecl }, DEDENT ;

CompatibleWithDecl = "compatible", "with", PackageName, NL ;

(* A theme names a look, and which component packages it is built for. A package
   may be declared compatible once; a second says nothing the first did not.  *)

UiProfileDecl  = "ui", "profile", Ident, NL,
                 INDENT,
                   { TargetPlatformDecl | TargetSizeDecl | PackagesBlock
                   | ProfileLayoutDecl | ProfileThemeDecl },
                 DEDENT ;

TargetPlatformDecl = "target", "platform", PlatformName, { ",", PlatformName }, NL ;

PlatformName   = Ident ;

TargetSizeDecl = "target", "size", ProfileSizeClass, NL ;

ProfileSizeClass = "compact" | "regular" | "expanded" ;

PackagesBlock  = "packages", NL,
                 INDENT, PackageName, { PackageName }, DEDENT ;

ProfileLayoutDecl = "layout", Ident, NL ;

ProfileThemeDecl = "theme", Ident, NL ;

PackageName    = Ident, { ".", Ident } ;

(* Each of the five is optional and may appear at most once, in any order - the
   body is read line by line rather than positionally. The size class names a
   class rather than a pixel breakpoint, because a narrow browser window and a
   compact phone are the same class and a breakpoint means nothing natively.
   A profile's "theme" and "layout" each select one declared elsewhere in the
   document - the layout being the navigational shell the build renders inside.
   The width x height matrix an arrangement resolves against is a separate
   concern.                                                                  *)

(* -------------------------------------------------------------- *)
(* Module                                                          *)
(* -------------------------------------------------------------- *)

Module         = "module", Ident, NL,
                 INDENT,
                   { DescriptionDecl
                   | AuthorizeDecl
                   | FileImport
                   | ScreenTemplateDecl
                   | DialogTemplateDecl
                   | FormDecl
                   | ContributionDecl
                   | InteractionBinding
                   | UsesBehaviorDecl
                   | ExampleDecl
                   | Feature },
                 DEDENT ;

(* A module or feature may attach an inline interaction with "on" or a
   named behavior with "uses". These bindings reach its descendant screens. *)

(* -------------------------------------------------------------- *)
(* Interactions                                                    *)
(* -------------------------------------------------------------- *)

BehaviorDecl   = "behavior", Ident, NL,
                 [ INDENT, { DescriptionDecl | FileDirective
                   | "parameter", Ident, [ RequiredTypeRef ], NL
                   | "order", SignedInteger, NL | InteractionBinding }, DEDENT ] ;

InteractionBinding = "on", InteractionTrigger, NL,
                 INDENT, { "where", BindingText, NL | InteractionAction }, DEDENT ;

UsesBehaviorDecl = "uses", Ident, NL,
                 [ INDENT, { Ident, BehaviorArgument, NL }, DEDENT ] ;
BehaviorArgument = ? nonempty argument text (literal, binding or parameter) ? ;
SignedInteger  = [ "-" ], Integer ;

InteractionTrigger = "click" | "double", "click" | "select" | "submit"
                   | "change" | "load" | "unload" | "enter" | "leave"
                   | "event", QualifiedName
                   | "interval", Integer, IntervalUnit
                   | Ident ; (* declared application trigger; starts uppercase *)

InteractionAction = InteractionActionHeader, NL,
                    [ INDENT, { InteractionArgument | InteractionContinuation }, DEDENT ] ;
InteractionActionHeader = "execute", QualifiedName
                        | "navigate", "to", QualifiedName | "navigate", "back"
                        | "open", "dialog", QualifiedName | "close", "dialog"
                        | "refresh", QualifiedName
                        | "set", Path, "to", BindingText
                        | "notify", ( "info" | "warning" | "error" ), InteractionMessage
                        | "confirm", InteractionMessage
                        | "raise", QualifiedName ;
InteractionArgument = "with", Ident, "from", BindingText, NL ;
InteractionMessage = LocalizableString | Path ;
InteractionContinuation = "on", ( "success" | "failure" | "result" ), NL,
                          INDENT, { InteractionAction }, DEDENT ;
BindingText    = ? nonempty remainder of the line, stored verbatim ? ;

(* A binding needs at least one action and at most one where guard. Guards,
   set values and argument bindings are opaque text, not Condition operands.
   on success/failure are allowed only on execute, refresh, confirm, open dialog
   and raise; on result only on open dialog. Continuations nest up to 16 action
   levels. An interval below 5 seconds warns. See interactions.md. *)

(* -------------------------------------------------------------- *)
(* Forms and contributions                                         *)
(* -------------------------------------------------------------- *)

FormDecl       = "form", Ident, "for", QualifiedName, NL,
                 INDENT, { FormDirective }, DEDENT ;

FormDirective  = FormPopulateDecl
               | FormFieldDecl
               | FormSubmitDecl | InteractionBinding | UsesBehaviorDecl ;

FormPopulateDecl = "populate", "via", "query", QualifiedName, [ "by", Ident ], NL
                 | "populate", "from", "item", NL ;

FormFieldDecl  = "field", Path,
                 [ "from", Path | "compose", "using", Ident ],
                 [ "label", LocalizableString ], NL ;

FormSubmitDecl = "on", "submit", NavigateDecl ;

ContributionDecl = "contribute", "to", Ident, NL,
                 [ INDENT, { ContributionDirective }, DEDENT ] ;

ContributionDirective = NavigateDecl
                      | "label", LocalizableString, NL
                      | "order", Integer, NL ;

(* Forms describe command input and optional query-backed population. A
   contribution adds one navigable item to a named contribution point. Forms
   are module-scoped; contributions may be declared on modules or features. *)

(* -------------------------------------------------------------- *)
(* Layout, screen template and dialog template                     *)
(* -------------------------------------------------------------- *)

(* A "layout" is the application's base navigational look and is selected by a
   "ui profile". A "screen template" is a reusable shape inside that shell and
   names the slot of its parent it fills. A "dialog template" opens over the
   application, so it fills no slot. The body is otherwise identical.        *)

LayoutDecl     = "layout", Ident, NL,
                 INDENT, StructureBody, DEDENT ;

ScreenTemplateDecl = "screen", "template", Ident, NL,
                 INDENT, { FitsSlotDecl | SlotDecl | ArrangementDecl
                         | InteractionBinding | UsesBehaviorDecl }, DEDENT ;

DialogTemplateDecl = "dialog", "template", Ident, NL,
                 INDENT, StructureBody, DEDENT ;

FitsSlotDecl   = "fits", "slot", Ident, NL ;

StructureBody  = { SlotDecl | ArrangementDecl | InteractionBinding | UsesBehaviorDecl } ;
(* At most one arrangement; a screen template has at most one fits slot. *)

SlotDecl       = Ident, [ "contributes", Ident ], NL ;

ArrangementDecl = "arrangement", "flow", NL,
                 INDENT, { ArrangementNode }, { WhenDecl }, DEDENT
                | "arrangement", "freeform", NL,
                 INDENT, { VariantDecl }, DEDENT ;

(* The tree hangs directly off "arrangement" - there is no separate block for
   it, so "template" means a screen or dialog template and nothing else. An
   arrangement is optional: a body of plain slot names is a complete
   declaration on its own.                                                   *)

ArrangementNode = ContainerDecl | ArrangementSlot ;

ContainerDecl  = ( "row" | "column" | "grid" ), [ "gap", Number ], NL,
                 INDENT, { ArrangementNode }, DEDENT ;

ArrangementSlot = Ident,
                 [ "width", Number ],
                 [ "height", Number ],
                 [ "grow" ],
                 [ "span", Number ],
                 NL ;

WhenDecl       = "when",
                 ( "width", ArrangementSizeClass, [ ",", "height", ArrangementSizeClass ]
                 | "height", ArrangementSizeClass ), NL,
                 INDENT, { ArrangementNode }, DEDENT ;

VariantDecl    = "variant", "width", ArrangementSizeClass, ",", "height", ArrangementSizeClass, NL,
                 INDENT, { PlaceDecl }, DEDENT ;

PlaceDecl      = "place", Ident,
                 ( "hidden"
                 | "at", Number, ",", Number, "size", SizeValue, ",", SizeValue ),
                 NL ;

SizeValue      = "fill" | Number ;

ArrangementSizeClass = "compact" | "regular" ;

(* -------------------------------------------------------------- *)
(* Features                                                        *)
(* -------------------------------------------------------------- *)

Feature        = "feature", Ident, NL,
                 INDENT,
                   { DescriptionDecl
                   | AuthorizeDecl
                   | FileImport
                   | Feature
                   | SliceDecl
                   | ExampleDecl
                   | ContributionDecl
                   | InteractionBinding
                   | UsesBehaviorDecl },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Slices                                                          *)
(* -------------------------------------------------------------- *)

SliceDecl      = "slice", SliceType, Ident, NL,
                 INDENT, { DescriptionDecl | FileDirective | SliceBody }, DEDENT ;

SliceType      = "StateChange" | "StateView" | "Automation" | "Translate" ;

SliceBody      = EventDecl
               | OperationDecl
               | CommandDecl
               | QueryDecl
               | ReadModelDecl
               | ProjectionDecl
               | ReducerDecl
               | CaptureDecl
               | SpecificationDecl
               | ExampleDecl
               | ReactionDecl
               | ScreenDecl
               | ConstraintDecl ;

ReadModelDecl  = "readmodel", Ident, NL,
                 INDENT, { DescriptionDecl | FileDirective | PropertyLine }, DEDENT ;

ReducerDecl    = "reducer", Ident, "=>", Ident, NL,
                 INDENT, { DescriptionDecl | ReducerRule }, DEDENT ;

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

EventDecl      = "event", Ident, [ "generation", PositiveUInt32 ], NL,
                 INDENT, { FileDirective | EventMetadata | TagDecl | PropertyLine }, DEDENT ;

EventMetadata  = EventDescriptionDecl | DocumentationDecl | EventIdDecl ;

EventDescriptionDecl = "description", ( StringLiteral | EventFencedText ), NL ;
EventIdDecl    = "id", StringLiteral, NL ;        (* nonempty old persisted name; at most one *)
DocumentationDecl = "documentation", NL, INDENT,
                    "```markdown", NL, { AnyLine }, "```", NL, DEDENT ;

(* Without a marker the generation is 1. Declarations of the same event name in
   the same slice are complete, distinct revisions and must be numbered from 1
   consecutively. A different slice owns a different event contract.          *)

PositiveUInt32 = Digit, { Digit } ;              (* value 1..4294967294; 4294967295 is Chronicle's unspecified sentinel *)

TagDecl        = "tag", TagValue, NL ;

TagValue       = Ident
               | StringLiteral
               | "$context.", Path
               | "$env.", Ident ;

Path           = Ident, { ".", Ident } ;

PropertyLine   = [ "@" ], Ident, TypeRef, [ "generated" ], [ "identifier" ], NL ;

(* "generated" is command-only and requires a required scalar Uuid-backed concept.
   Generated values and responses select ESM v7. Pre-generation references report
   PLAY0273; generated concepts with validation rules report PLAY0268. *)

(* "identifier" is only accepted on a command property, and on at most one of
   them - it marks the property a runtime resolves the event source id from.  *)

TypeRef        = QualifiedName, [ "[]" ], [ "optional" | "?" ] ;
RequiredTypeRef = QualifiedName, [ "[]" ] ;

(* "optional" follows the complete type, including any collection marker.
   It is case-sensitive and contextual, not a reserved name. The attached ?
   suffix still parses, with information diagnostic PLAY0479; prefer optional. Modifiers cannot
   be repeated. Optional reads are not yet supported. *)

(* -------------------------------------------------------------- *)
(* Commands                                                        *)
(* -------------------------------------------------------------- *)

CommandDecl    = "command", Ident, NL,
                 INDENT,
                   { DescriptionDecl | PropertyLine | ReadsDecl | AuthorizeDecl
                   | ValidateDecl | ProducesDecl | HandlerDecl | ConcurrencyDecl | CommandResponse | CommandStreamDecl },
                 DEDENT ;

(* A command cannot have both produces and handler. At most one concurrency
   block; repeated authorize lines combine with and in authored order. *)

CommandStreamDecl = "stream", Ident, ".", Ident, NL,
                    [ INDENT, "streamId", "=", MappingSource, NL, DEDENT ] ;

(* At most one authored command route, including on a handler command. It selects
   classification, not the identity destination supplied by for. Resolve exact
   headers once against the complete immutable compilation input and authoritative
   import placements: @stream Qualified.Type and modified property forms remain
   properties; production stream mappings remain payload. A uniquely owned route
   and a viable imported value type together produce blocking PLAY0505 with both
   candidates retained. A known source with missing/duplicate stream ownership
   produces PLAY0504. Neither resolved interpretation keeps legacy property syntax,
   including deeper legacy members and unknown-type evidence. A nested streamId
   does not force route interpretation; typo sources are not distinguishable from
   unresolved qualified property types by spelling alone. Every ambiguous or duplicate
   header is retained structurally in streamCandidates; stream holds at most one
   unambiguous route. Invalid candidate drafts support syntax JSON transport, but
   printing and folder expansion refuse them with InvalidSyntaxJson. *)

CommandResponse = "returns", [ "@" ], Ident, NL
                | "returns", NL, INDENT, ResponseField, { ResponseField }, DEDENT ;
ResponseField  = [ "@" ], LowerIdent, [ TypeRef ], "=", [ "@" ], LowerIdent, NL ;
(* At most one unconditional response. A two-token returns line refers to a source
   only when that source is another property of the same command. Otherwise it is
   a property declaration. @returns Type forces a property; returns @name forces
   a response. Types are inferred or must exactly match the direct source. *)

ReadsDecl      = "reads", Ident, [ "as", LowerIdent ], [ "by", LowerIdent ], NL ;

(* The read model a command consults before it decides. Declaring it puts the
   read model in scope for the rest of the command body, so a produces mapping
   can be fed from state - "consultantId = EngagementScope.consultantId" - and
   a validation rule can be stated against it. "by" names the command property
   the read model is looked up by, and is absent for a read model that is not
   looked up by a key. An alias is required for every instance when the same
   read model is read twice by one command. Aliases must be unique in that
   command and must not match a command property or the keywords "as", "by",
   or "reads". An unambiguous view name and an alias may qualify paths in a
   require condition.                                                       *)

ConcurrencyDecl = "concurrency", NL,
                 INDENT, { ConcurrencyDim }, DEDENT ;

ConcurrencyDim = "eventSource", NL
               | "sourceType", Ident, NL
               | "streamType", Ident, NL
               | "streamId", Ident, NL
               | "events", Ident, { ",", Ident }, NL ;

AuthorizeDecl  = "authorize", PolicyRequirement, NL ;

PolicyRequirement = PolicyAll, { "or", PolicyAll } ;

PolicyAll      = PolicyRequirementOperand, { [ "and" ], PolicyRequirementOperand } ;

PolicyRequirementOperand = PolicyRef
               | "(", PolicyRequirement, ")" ;

PolicyRef      = Ident ;

(* Two policies written next to each other mean both, which is what "authorize
   A B" has always meant, and "and" says the same thing out loud. Combining is
   the language's one condition rule - "and" binds tighter than "or", both are
   left associative, parentheses override - so "A or B and C" groups here
   exactly as it groups in a policy. A requirement may continue on the next
   line at deeper indentation.                                              *)

ValidateDecl   = "validate", NL,
                   INDENT, ( { ValidationRule | RequireRule } | InlineBlock ), DEDENT ;

ValidationRule = Path, RuleOp, [ "severity", ValidationSeverity ], [ "message", LocalizableString ], NL,
                   [ INDENT, RuleImplementation, DEDENT ] ;

RequireRule    = "require", Condition, NL,
                   [ INDENT, { "severity", ValidationSeverity, NL
                             | "message", LocalizableString, NL }, DEDENT ] ;
(* At most one severity directive per require. *)

ValidationSeverity = "information" | "warning" | "error" ;

(* Severity defaults to error and affects presentation, not whether a failure rejects.
   A validation rule puts severity before the end-of-line message; require keeps
   both directives in its body so a condition is never confused with metadata. *)

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

(* "email" is the one defined named match pattern. StringLiteral here holds an
   ECMAScript regular expression, not another named pattern; see Commands for
   its definition and substring-matching semantics.                         *)

(* RuleImplementation is only meaningful after "rule", Ident - the other RuleOp
   forms are already fully declarative and take no implementation body. *)
RuleImplementation = FileDirective
                    | InlineBlock
                    | CommandRuleImplementation ;
CommandRuleImplementation = "implementation", NL,
                    [ INDENT, { ImplementationHint | FileDirective | InlineBlock }, DEDENT ] ;
(* CommandRuleImplementation is accepted only on command property named predicates.
   Hints are ordered, nonblank quoted strings. Zero or one source: file OR tagged fence.
   Duplicate wrappers and wrapped/direct mixtures are errors. Pending is authoring-valid
   but fails executable binding with PLAY0268; attached wrappers keep the existing
   RulePredicate contract and do not change canonical ESM bytes. Concept predicates,
   builtins and whole-command validation do not accept the wrapper. *)

(* A RuleImplementation and a "validate" InlineBlock both compile against
   RuleContext. The rule implementation answers with a bool; the "validate csharp"
   block yields the message of every rule the artifact breaks -
   see Documentation/screenplay/context.md.                                  *)

Value          = Number | StringLiteral | "today" | "true" | "false" | "null" | Path ;

(* "max" and "min" take their meaning from the property type: a text length or
   a number's value. In a ValidationRule a Path operand names a member of an
   enum concept - see Documentation/screenplay/commands.md for the rules the
   executable model admits.                                                  *)

(* -------------------------------------------------------------- *)
(* Produces                                                        *)
(* -------------------------------------------------------------- *)

ProducesDecl   = "produces", ProductionReference, NL,
                   [ INDENT, { ForDecl | TagDecl | PropertyMapping }, DEDENT ]
               | "produces", "when", Condition, NL,
                   INDENT, ProductionReference, NL,
                   [ INDENT, { ForDecl | TagDecl | PropertyMapping }, DEDENT ],
                   DEDENT
               | InlineEventProduction
               | InlineOperationProduction ;

ProductionReference = Ident | QualifiedOperationReference ;
QualifiedOperationReference = Ident, ".", Ident, { ".", Ident } ;
(* A qualified production must resolve to an explicit operation declaration.
   Event productions retain their existing bare-name grammar and binding rules.
   Operations have no for destination or event metadata and do not participate
   in event destination defaults. Plain references never declare a target. *)

InlineEventProduction = "produces", "event", Ident, NL,
                        [ INDENT, { ForDecl | TagDecl | EventMetadata | TypedMapping }, DEDENT ] ;
TypedMapping   = [ "@" ], Ident, TypeRef, "=", MappingSource, NL ;
ForDecl        = "for", MappingSource, NL ;

(* InlineEventProduction is allowed only inside commands. It declares a slice-owned
   generation-1 event; origin and generation are forbidden. Tags are event-type tags.
   At most one for, id, description and documentation directive is allowed.
   An inline omission means the command identifier only when no production names
   another source and no plain production omits for. Otherwise every destination
   must be explicit. Typed payload property names must be unique (PLAY0168).
   Plain omission retains legacy allocation semantics. ESM v7 commands with a generated
   identifier may route explicitly to another required scalar command property;
   named event sources and streams remain unadmitted.
   Unescaped namespace, sequence, correlation, causation, causedBy and occurred
   are reserved system-assigned metadata in both production forms. *)

(* At most one for directive in each production. A conditional production has
   one event child, not a list of alternative events. Conditions combine as
   policy conditions do - see the note under Policies. *)

Condition      = ConditionAnd, { "or", ConditionAnd } ;

ConditionAnd   = ConditionOperand, { "and", ConditionOperand } ;

ConditionOperand = Path, CompOp, Value
               | "(", Condition, ")" ;

CompOp         = "==" | "!=" | ">" | ">=" | "<" | "<="
               | "contains" | "starts", "with" ;

(* The word operators compare text: "contains" for a substring anywhere,
   "starts with" for one at the beginning. "starts with" is two words because
   that is the phrase, so an operator is not always a single token.          *)

PropertyMapping = [ "@" ], Ident, "=", MappingSource, NL ;

MappingSource  = Ident                         (* command property   *)
               | ContextPath
               | "$env.", Ident
               | "$strings.", Path
               | StringLiteral
               | Number
               | "true" | "false" | "null"
               | StructuredValue
               | Expression ;

StructuredValue = "[", [ JSONValue, { ",", JSONValue } ], "]"
                | "{", [ JSONString, ":", JSONValue,
                         { ",", JSONString, ":", JSONValue } ], "}" ;
JSONValue       = StructuredValue | JSONString | Number | "true" | "false" | "null" ;
JSONString      = (* double-quoted JSON string, including escaped characters *) ;

(* Structured values are single-line JSON-shaped data with quoted object keys.
   They are typed syntax in mapping sources; projection expressions keep their
   existing expression grammar.                                        *)

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
(* Operations — syntax-only                                        *)
(* -------------------------------------------------------------- *)

OperationDecl  = "operation", Ident, NL,
                 INDENT, { DescriptionDecl | UsesSystem | OperationInput | OperationPhase }, DEDENT ;
InlineOperationProduction = "produces", "operation", Ident, NL,
                 INDENT, { DescriptionDecl | UsesSystem | TypedMapping | OperationPhase }, DEDENT ;
UsesSystem     = "uses", Ident, NL ;
OperationInput = [ "@" ], Ident, TypeRef, NL ;
OperationPhase = ( "execute" | "compensate" ), NL,
                 [ INDENT, { DescriptionDecl | FileDirective | InlineBlock | OperationImplementation }, DEDENT ] ;
OperationImplementation = "implementation", NL,
                 [ INDENT, { ImplementationHint | FileDirective | InlineBlock }, DEDENT ] ;

(* Operations are slice-owned and command-only; event and operation names share
   the slice namespace. Exactly one uses must resolve to a declared system.
   Each phase occurs at most once and owns at most one file OR tagged fence;
   wrapped and direct sources cannot mix. Hints are ordered and nonblank.
   Inputs cannot be identifier or generated properties. Code and phases are
   optional: intent-only and description-only forms remain valid authoring.
   New words are contextual, not globally reserved property names. Within an
   operation, @uses escapes an input named uses; event metadata input names
   also use @. Typed inputs named execute or compensate are not phase headers.
   Execution, failure fixtures and compensation are not admitted by any supported ESM version yet. *)

(* -------------------------------------------------------------- *)
(* Handler                                                         *)
(* -------------------------------------------------------------- *)

HandlerDecl    = "handler", NL,
                 INDENT, ( FileDirective | InlineBlock | HandlerImplementation ), DEDENT ;
HandlerImplementation = "implementation", NL,
                        [ INDENT, { ImplementationHint | FileDirective | InlineBlock }, DEDENT ] ;
ImplementationHint = "hint", StringLiteral, NL ;
(* The handler wrapper retains its existing contract: hints are ordered, nonblank quoted strings.
   At most one payload (file OR tagged fence); direct and wrapped sources cannot mix.
   A bare or hints-only implementation is pending, not executable.
   Operation phases and command property named rules also support a wrapper;
   forms on other owners are deferred. *)

(* -------------------------------------------------------------- *)
(* Queries                                                         *)
(* -------------------------------------------------------------- *)

QueryDecl      = "query", Ident, "=>", [ "observable" ], TypeRef, NL,
                 [ INDENT,
                     { DescriptionDecl | ByClause | FilterClause | ScopeDecl
                     | AuthorizeDecl | PerformerDecl },
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

ProjectionDecl = "projection", QualifiedName, [ "=>", QualifiedName ], NL,
                 INDENT, PDLBody, DEDENT ;

(* Variant groups omit the target: each variant names its read model.
   A non-variant projection in a Screenplay application needs a target to bind
   its read model. Standalone PDL also accepts a targetless header. *)

PDLBody        = (* Projection Declaration Language grammar - covers the projection
                    directives (automap, sequence, file, key), the from/every/join/
                    children/nested blocks, property mapping, expressions and removal
                    - see Documentation/screenplay/projections/grammar.md *) ;

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

ExampleDecl    = "example", Ident, ":", QualifiedName, NL,
                 [ INDENT, { DescriptionDecl | SpecificationEventSource | PropertyMapping | GeneratedFixture }, DEDENT ] ;

(* One typed fixture, never a caller, clock or sequence of steps. Its underlying
   declaration is an event, command or read model, not another example. Examples
   may be declared at document, module, feature or slice scope, and alongside
   specifications in a standalone specification document. *)

InlineFixtureAssignment = Path, "=", ConcreteValue ;

SpecificationDecl = "specification", Ident, NL,
                 INDENT, [ FileDirective ], { SpecificationGiven | SpecificationWhen | SpecificationThen }, DEDENT ;

SpecificationGiven = OperationFailureFixture
               | "given", "caller", NL,
                 [ INDENT, { "authenticated", NL | "role", StringLiteral, NL | "claim", StringLiteral, "=", StringLiteral, NL }, DEDENT ]
               | "given", "readmodel", QualifiedName, [ InlineFixtureAssignment ], NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "given", "clock", StringLiteral, NL
               | "given", "capture", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "given", QualifiedName, [ InlineFixtureAssignment ], NL,
                 [ INDENT, { SpecificationEventSource | PropertyMapping }, DEDENT ] ;

(* "given clock" states the ISO 8601 instant the scenario happens at - the
   occurrence time of everything it does. "given capture" states an earlier record
   of a capture's source, so a value transition has something to transition from. *)

SpecificationWhen = "when", QualifiedName, [ InlineFixtureAssignment ], NL,
                 [ INDENT, { SpecificationEventSource | PropertyMapping | GeneratedFixture }, DEDENT ]
               | "when", "append", QualifiedName, [ InlineFixtureAssignment ], NL,
                 [ INDENT, { SpecificationEventSource | PropertyMapping }, DEDENT ]
               | "when", "redelivered", QualifiedName, "to", QualifiedName, NL,
                 [ INDENT, { SpecificationEventSource | PropertyMapping }, DEDENT ]
               | "when", "clock", StringLiteral, NL
               | "when", "trigger", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "when", "capture", Ident, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "when", "query", QualifiedName, NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

(* One action per specification. "when clock" lets the clock reach an instant, so
   a reaction scheduled with "every" or "at" is due. "when trigger" fires an
   application trigger with the values it carries. "when capture" hands a capture
   one record of its source. "when query" performs a query with its arguments;
   its outcome is "then result", "then no result" or "then denied".           *)

GeneratedFixture = "generated", LowerIdent, "=", ConcreteValue, NL ;
ReturnExpectation = "then", "returns", ConcreteValue, NL
                  | "then", "returns", NL, INDENT, ReturnField, { ReturnField }, DEDENT ;
ReturnField    = LowerIdent, "=", ConcreteValue, NL ;
ConcreteValue  = ? a completely consumed literal, list or object, without raw expressions ? ;
(* Generated fixtures and return expectations select ESM v7.
   Reached generation without a fixture is Unsupported(IdentityAllocation).
   Generated identifiers use SpecificationEventSource, not GeneratedFixture.
   Return expectations require a command and cannot accompany errors or denial. *)

OperationFailureFixture = "given", "operation", QualifiedName, "fails", NL ;
OperationExpectation = "then", "operation", QualifiedName, NL,
                 [ INDENT, { OperationValue }, DEDENT ] ;
OperationValue = Path, "=", ConcreteValue, NL ;
CompensationExpectation = "then", "compensated", QualifiedName, NL ;
(* Failure and compensation lines are leaves. Operation assertions may be partial
   but require compatible concrete values. All three require an operation-kind
   reference and a command action; compensation must be declared. These forms
   are syntax-only, not admitted by any supported ESM version yet. *)

SpecificationThen = ReturnExpectation
               | OperationExpectation
               | CompensationExpectation
               | "then", "readmodel", QualifiedName, [ "exactly" ], [ InlineFixtureAssignment ], NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "then", "no", "readmodel", Ident, "for", Expression, NL
               | "then", "query", QualifiedName, [ "exactly" ], NL,
                 [ INDENT, { SpecificationQueryDirective }, DEDENT ]
               | "then", "result", [ "exactly" ], NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "then", "no", "result", NL
               | "then", "no", "events", NL
               | "then", "error", [ StringLiteral ], NL
               | "then", "denied", NL
               | "then", "events", "in", "any", "order", NL
               | "then", QualifiedName, [ InlineFixtureAssignment ], NL,
                 [ INDENT, { SpecificationEventSource | PropertyMapping }, DEDENT ] ;

SpecificationEventSource = "for", Expression, NL ;

SpecificationQueryDirective = "arguments", NL,
                 [ INDENT, { PropertyMapping }, DEDENT ]
               | "result", NL,
                 [ INDENT, { PropertyMapping }, DEDENT ] ;

(* By default read-model and query-result property comparison is subset;
   "exactly" requires all properties. Event assertions are ordered and exact
   by default; "then events in any order" retains exact count and payload
   comparison while ignoring their order. Append actions are event occurrences,
   not commands; optional "for" asserts the typed event source (ESM v2). *)

(* "then no readmodel" requires a concrete key of the view identifier's type;
   it has no "exactly" qualifier or child mappings. It selects ESM v5, and
   contradicts "then readmodel" for the same view and key. *)

(* Repeat "result" to assert several results in authored comparison order. A
   "then query" with no result blocks asserts an empty result. The query
   declaration supplies the result read-model type, so it is not repeated. *)

(* A bare "then error" states a rejection whose reason the specification does
   not name; the quoted form names it. Both match regardless of validation severity. *)

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
(* Extension boundary                                              *)
(* -------------------------------------------------------------- *)

(* Construct keywords are closed and every accepted construct appears in this
   grammar. Inline language tags are open: the compiler carries a registered
   block as opaque text, but registration does not add a new host-language
   construct. Editor-only sub-language registrations are not valid Screenplay
   syntax until the compiler itself gains that construct. See
   sub-languages.md.                                                *)

(* -------------------------------------------------------------- *)
(* Constraints                                                     *)
(* -------------------------------------------------------------- *)

ConstraintDecl = "constraint", Ident, NL,
                 INDENT, ConstraintBody, DEDENT ;

ConstraintBody = { ConstraintOption }, UniquePropertyRule,
                   { UniquePropertyRule | ConstraintOption }
               | { ConstraintOption }, UniqueEventRule,
                   { UniqueEventRule | ConstraintOption }
               | FileDirective ;  (* file <Path>: Chronicle IConstraint, uniqueness only;
                                     PLAY0396 warns; ESM rejects with PLAY0268 *)
UniquePropertyRule = "unique", Ident, { ",", Ident }, "on", Ident, NL ;
UniqueEventRule = "unique", "event", Ident, NL ;
ConstraintOption = "released", "by", Ident, NL
                 | "message", String, NL
                 | "ignore", "casing", NL ;  (* property rules only *)

(* -------------------------------------------------------------- *)
(* Reactions and triggers                                          *)
(* -------------------------------------------------------------- *)

ReactionDecl   = "reaction", Ident, NL,
                 INDENT,
                   { DescriptionDecl | TriggerClause | WhereDecl },
                 DEDENT ;

(* A reaction needs at least one trigger and at most one where condition.
   A trigger with no body is a complete statement of intent - the reaction runs
   when that happens. The file reference and the inline block are optional
   realization metadata.                                                      *)

TriggerClause  = TriggerSource, NL,
                 [ INDENT,
                     { DescriptionDecl | TriggerValue | ReadsDecl | ProducesDecl
                     | InvokesDecl | FileDirective | InlineBlock },
                   DEDENT ] ;

TriggerSource  = "when", Ident                      (* event, declared or registered trigger *)
               | "every", Integer, IntervalUnit     (* every 15 minutes                      *)
               | "at", Time, [ "on", ( Weekday | "day", Integer ) ] ;

IntervalUnit   = "second"  | "seconds"
               | "minute"  | "minutes"
               | "hour"    | "hours"
               | "day"     | "days" ;

Weekday        = "Monday" | "Tuesday" | "Wednesday" | "Thursday"
               | "Friday" | "Saturday" | "Sunday" ;

Time           = Digit, Digit, ":", Digit, Digit ;   (* 24 hour, HH:mm *)

(* A bare name selects a value the occurrence carries, so it is written without a
   type - the shape belongs to the event or the trigger declaration.          *)

TriggerValue   = [ "@" ], Ident, [ TypeRef ], NL ;

(* Under a reaction trigger, ReadsDecl's "by" names one of its selected
   TriggerValues. Clock sources take no values, so they cannot use "by".
   Repeated views require distinct aliases, which must not match trigger values.
   "@reads" selects a trigger value
   named reads; "for each <View>" is reserved, not admitted here. Reactions
   do not yet bind in the executable semantic model. *)

WhereDecl      = "where", Condition, NL ;

(* The trigger declaration. It says the name exists and what an occurrence hands
   the reaction - never what makes one occur, which belongs to whatever provides
   it. That boundary is what lets the set of triggers be open.                *)

TriggerDecl    = "trigger", Ident, NL,
                 INDENT, { DescriptionDecl | FileDirective | TriggerValue }, DEDENT ;

InvokesDecl    = "invokes", Ident, NL,
                 [ INDENT, { PropertyMapping | RefusalBranch }, DEDENT ] ;
RefusalBranch  = "on", "refused",
                 [ "by", ( "validation" | "authorization" | "constraint", [ QualifiedName ] ) ], NL,
                 INDENT, ( "acknowledge", NL | ProducesDecl, { ProducesDecl } ), DEDENT ;
RefusalValue   = "$refusal.", ( "reason" | "constraint" | "message" ) ;

(* Refusal branches and values, "when redelivered" and "then no events" are syntax-only:
   binding refuses them with PLAY0268. No executable admission is claimed.
   Branches are ordered; bare refusal excludes authorization. RefusalValue is
   a String source only in branch event mappings; constraint requires a
   "by constraint" selector. Branches cannot produce operations or inline events.
   Redelivery names one compatible event-trigger reaction and locates exactly
   one given event by optional "for" and all stated values, without appending it.
   "then no events" is a leaf for non-append actions awaiting admission; it cannot
   accompany events, event-order, error or denial expectations. *)

(* What the reaction sets off. Plain "produces" has the same form as on a command;
   InlineEventProduction is not allowed inside reactions.
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
               | TemplateRef
               | InteractionBinding
               | UsesBehaviorDecl
               | InlineBlock ;

(* A screen, a section and a filled slot attach interactions the same way a
   module or feature does - see interactions.md.                              *)

DataDecl       = "data", RequiredTypeRef, "via", "query", QualifiedName,
                 [ "by", Ident ], NL ;

ActionDecl     = "action", QualifiedName, NL,
                 [ INDENT, { ActionOption }, DEDENT ]
               | "action", LocalizableString, NL,
                 INDENT, { ActionAlternative | ActionOtherwise | NavigateDecl }, DEDENT ;

ActionAlternative = "when", Condition, "execute", QualifiedName, NL,
                    [ INDENT, { InteractionArgument }, DEDENT ] ;
ActionOtherwise   = "otherwise", "hidden", NL
                  | "otherwise", "execute", QualifiedName, NL,
                    [ INDENT, { InteractionArgument }, DEDENT ] ;
(* A guarded action requires at least one alternative; otherwise is optional,
   occurs once after all alternatives, and hidden has no arguments. At most one
   navigate is allowed. Conditions compare item.<field>[.<field>...] to literals;
   ordering requires numbers, contains / starts with require strings. *)

ActionOption   = NavigateDecl
               | "label", LocalizableString, NL ;

NavigateDecl   = "navigate", "to", QualifiedName, [ "by", Ident ], NL ;

TemplateRef    = "template", Ident, NL,
                 INDENT, { FilledSlot }, DEDENT ;

FilledSlot     = Ident, NL,
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

FencedText     = NL, INDENT, ( "```text" | "```" ), NL, { AnyLine }, "```", DEDENT ;
EventFencedText = NL, INDENT, ( "```text" | "```markdown" | "```" ), NL, { AnyLine }, "```", DEDENT ;
(* Bare description fences are accepted for compatibility with warning PLAY0397.
   Only event descriptions accept the markdown tag; documentation requires it. *)

LocalizableString = StringLiteral
               | "$strings.", Path ;

FileDirective  = "file", FilePath, NL ;
FilePath       = (* repository relative path, never absolute *) ;

(* One keyword, and the construct it sits on says which of the language's two
   file relationships is meant. On a construct that HAS an implementation - a
   handler, a performer, a reducer rule, a reaction trigger, a rule predicate,
   a constraint, a screen - it stands in for the inline body: the implementation
   lives there. A constraint file names a Chronicle IConstraint class, which can
   declare only uniqueness; prefer the portable unique forms instead. On a pure
   declaration - concept, type, event, readmodel,
   projection, slice, specification, trigger - there is no body to delegate, so
   it can only say which file realizes the declaration. Those are different
   relationships, but the construct already decides which one, so a second
   keyword would say nothing the reader does not already know and would be one
   more word to learn.

   A path is repository relative, so it means the same thing on every machine,
   and it is never resolved by the compiler - a document is read in a designer,
   in a build and where the tree is absent, so a path that has gone stale must
   not be what makes a valid document invalid. An absolute one is reported as a
   warning, because it is wrong without looking anything up.

   In a block that also reads property lines - event, readmodel, type - the
   directive is told from a property named "file" by shape: a type reference is
   a bare identifier, so a value carrying a separator or an extension is a path
   and nothing else, and the property wins the tie. This is the rule
   "description" already follows in the same blocks. A trigger body reserves the
   word outright, as it always has, so a trigger value named after it is written
   "@file".                                                                  *)

InlineBlock    = "```", LanguageTag, NL, { AnyLine }, "```", NL ;

(* The fence's info string is the one place a block names its language. The
   earlier forms - "validate csharp" on the keyword line, and a language tag on
   its own line above a bare fence - still parse, with a deprecation warning
   (PLAY0397), and print back in the form above.                            *)
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
Number         = [ "-" ], Digit, { Digit }, [ ".", Digit, { Digit } ] ;
Integer        = Digit, { Digit } ;
Ident          = Letter, { Letter | Digit | "_" } ;
LowerIdent     = ( "a".."z" | "_" ), { Letter | Digit | "_" } ;
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
| `concept` / `type` | primitive or properties, attributes, `validate` | a ```` ```csharp ```` block under `validate` |
| `command` | `produces` with mappings and conditions | `handler` |
| `query` | `=>` return type with optional `observable`, `by`/`filter`, `description` | `performer` |
| `policy` | `require` conditions | inline `csharp` |
| `reaction` | `description` on the reaction and on each trigger, plus trigger `reads`, `produces` / `invokes` / `where` | `file` / inline block |
| `screen` | title, sections, tables, `data`, `action`, `navigate`, `template` | `file` |
| `constraint` | `unique …` forms | `file` |
| `projection` / `capture` | fully declarative (PDL / CDL) | — |

## The other `file` — where a declaration is realized

The table above is `file` standing in for an implementation. A **declaration** has no implementation to stand in for, so on `concept`, `type`, `event`, `readmodel`, `projection`, `slice`, `specification` and `trigger` the same keyword says something else: **which file realizes this declaration**.

```screenplay
concept InvoiceId : Uuid
  file Invoicing/InvoiceId.cs
```

```screenplay
slice StateChange RegisterInvoice
  file Invoicing/RegisterInvoice/RegisterInvoice.cs

  event InvoiceRegistered
    file Invoicing/RegisterInvoice/RegisterInvoice.cs
    invoiceId InvoiceId
```

One keyword covers both, because the construct it sits on already decides which is meant — a second word would carry no information a reader does not already have, and would be one more thing to learn. It changes nothing about the guarantee above: a declaration still says everything it says without a `file`, and adding one never replaces any part of it. A `projection` still needs its blocks, an `event` still needs its properties.

Two rules follow from a path being data rather than a claim about a machine:

- **Repository relative, never absolute** — the same path means the same thing wherever the document is read. An absolute one is a warning ([`PLAY0264`](diagnostics.md)), not an error.
- **Never resolved** — the compiler does not look for the file. A document is read in a designer, in a build and on a machine where the tree is absent, so a path that has gone stale is not what makes a valid document invalid. Whatever *can* resolve paths decides for itself what an unresolvable one means.

In `event`, `readmodel` and `type` — the bodies that also read property lines — `file` is told from a property named `file` by shape. A type reference is a bare identifier, so a value carrying a separator or an extension is a path and nothing else, and a property wins the tie:

```screenplay
type Upload
  file Attachment
  size Int
```

That is a property named `file` of type `Attachment`, exactly as it was before the directive existed. A `trigger` body reserves the word outright, as it always has, so a trigger value named after it is written `@file`.

So this is a complete, valid statement of intent for a reaction nobody has written yet:

```screenplay
reaction AcceptedInvitationProvisioner
  description "Provisions the account when an invitation to join is accepted"
  when InvitationAccepted
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
| `command` body | `authorize`, `produces`, `reads` (`description`, `validate`, `handler` and `concurrency` resolve by shape) |
| `event` body | `tag` (`id`, `description`, and `documentation` resolve by shape) |
| inline `produces event` body | `tag`, `for`, `generation`, `origin`, `namespace`, `sequence`, `correlation`, `causation`, `causedBy`, `occurred`; metadata names resolve by shape |
| reaction trigger body | `description`, `file`, `produces`, `invokes`, `reads` (use `@reads` for a value named `reads`) |
| plain `produces` body | `tag`, `for`, `namespace`, `sequence`, `correlation`, `causation`, `causedBy`, `occurred` |
| other mapping blocks | `tag`; the printer also escapes the production metadata names above |
| projection `from` block | `key`, `parent` |
| projection `clear` mapping target | `with` |
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
