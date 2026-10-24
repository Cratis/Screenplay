---
title: "Syntax tree compatibility"
description: "What the Screenplay syntax tree promises across versions, so code generators and designers know which guarantees to build on."
---

# Syntax tree compatibility

The syntax tree is a public API. Every consumer that compiles Screenplay to something - a code generator, a designer, a documentation tool - is written against these types, so what the tree promises across versions decides how much work a Screenplay upgrade costs them.

This page states those promises. It is deliberately narrow: a guarantee you cannot rely on is worse than one that was never made.

## Clock operand authoring additions

Decisions 0106/0107 add optional `ApplicationSyntax.TimeZone`, `CommandSyntax.Calendar`,
`SpecificationSyntax.GivenTimeZone` and `ComparisonConditionSyntax.LeftExpression` members.
They are omitted from JSON when absent, so legacy syntax vectors retain their bytes.
The comparison's existing string `Left` remains unchanged for paths; for a clock operand
it contains canonical text and `LeftExpression` holds the structured expression.
`ClockOffsetExpressionSyntax` and `DateOfExpressionSyntax` may appear on either side.
A bare `today` or `now` is still a path, not a globally reserved keyword.

These additions are syntax-only. Binding them reports `PLAY0268` naming decisions
0106/0107; ESM versions and executable payloads are unchanged. See
[commands](commands.md#calendar-and-clock-operands--syntax-only).

## What the tree is

Every node is a positional `record` deriving from `SyntaxNode`, which carries a `SourceLocation`. Nodes are immutable, compare by value, and support `with` expressions. A construct with several forms - an expression, a policy condition, a projection block, a screen directive, a projection mapping, a projection key, a validation block, a constraint - is an abstract base record with a concrete record per form.

That shape is stable. Nodes will stay records, stay immutable, and keep deriving from `SyntaxNode`.

## What is guaranteed

**A node kind can be added.** New constructs arrive as new record types. If the new type derives from an existing abstract base, it starts appearing in collections you already read - see the caveat below. If you consume the tree through [`ScreenplaySyntaxWalker`](visitors.md), a new kind arrives as a new `Visit` method with a default implementation that walks its children, so your subclass compiles unchanged and keeps walking. That is the property the walker exists to give you.

**An unknown form never throws.** The walker dispatches abstract bases with a fallback: a concrete form it does not recognize is passed to `VisitNode` and then left alone. A tool compiled against an older Screenplay does not fault on a document using a newer construct - it silently does not act on it, which is the recoverable failure.

**Enum members keep their numeric values.** `SliceType`, `ValidationRuleKind`, `ComparisonOperator`, `LogicalOperator`, `AutoMapMode` and `CaptureWhenKind` gain members at the end and never renumber existing ones. This one matters more than it looks: the C# compiler inlines an enum constant into the consuming assembly, so renumbering a member changes behavior in an already-compiled consumer with no error anywhere - the value it was built with silently means something else. New members are therefore always appended.

**`public const` values never change.** The [diagnostic codes](diagnostics.md) are `public const string`, and a `const` inlines into consumers exactly the way an enum member does. A code's value is fixed once published; a diagnostic that changes meaning gets a new code rather than a new value on the old one. The same rule applies to every other published constant, such as `QuerySyntax.ObservableModifier` and `PropertySyntax.IdentifierModifier`.

**A node grows by init-only property, not by a new parameter.** A node gaining an optional value gains it as an `init` property on the record body, so the primary constructor and `Deconstruct` keep the exact signature they were published with. Positional construction you already wrote keeps compiling, `with` expressions reach the new value like any other, and it participates in equality and `ToString` the same way a parameter would.

Appending a trailing optional parameter instead looks equivalent and is not. It is source compatible and *binary* breaking: the compiler emits a different primary constructor and a different `Deconstruct`, so an assembly built against the previous version calls a member that no longer exists and fails at run time with a missing method - with no error at compile time anywhere. It has shipped once, `Cratis.Arc.Screenplay` built against 1.5.2 breaking against 1.7.0 exactly this way. Package validation now fails the build that would repeat it, so the guarantee is enforced rather than intended.

Authoring metadata follows this rule: `SpecificationSyntax.Description` and the `Documentation` members on modules, features, slices, commands, read models and reactions are nullable init-only properties. They appear in the syntax transport schema, not in the executable semantic model. Preserve them when rewriting a declaration.

`SpecificationSyntax.GivenCallerPersona` is an additive init-only reference with a name and location, distinct from `GivenCaller`. It is omitted from syntax JSON when absent, preserving untouched documents' bytes. A reader that predates the persona member must reject the new member rather than silently interpret an unauthenticated caller. Effective syntax expands the reference and carries `SpecificationValueOrigin.Persona` with persona and policy provenance; it adds no ESM member or version.
## Atomic reaction production mode

`ApplicationSyntax.ReactionProductionMode` is a nullable init-only `ReactionProductionMode` enum. The only admitted value is `Atomic`, authored as application-root `reaction batches atomic`; absence preserves legacy behavior and is omitted from syntax JSON. The scalar is visible to `VisitApplication`, not a new child node. Folder layout keeps it only in the root document, and imported reactions inherit it. Strict transport readers reject unknown values. ESM v22 writes the distinct optional final application member `reactionProductionMode: "atomic"`; consumers must preserve the choice during source recovery or refuse it.

## Protected command read mode

`ApplicationSyntax.CommandReadMode` is a nullable init-only `CommandReadMode` enum, with the sole value `Protected`. Application-root `command reads protected` selects it; syntax JSON writes `commandReadMode: "Protected"` only when present. The scalar is visible to `VisitApplication`, not a child node, and duplicates or conflicting/non-root declarations report `PLAY0780`. Folder layout keeps it in the application root; imported commands inherit it. Preserve it during rewrites and source recovery.

ESM v28 admits Uuid `==` and `!=` command conditions without changing AST kinds or adding ESM members or value variants. A literal stays a text constant, typed from the other condition operand only at v28 or later and checked as a canonical Uuid. Older readers and validators reject the shape. This does not change equality in specification expectations, keys or constraints. See [Uuid equality](commands.md#uuid-equality-in-command-conditions).

ESM v26 admits chronological Date/DateTime command comparisons without changing AST kinds or adding ESM members or value variants. A literal stays a text constant, typed from the other condition operand or validation subject only at v26 or later. Canonical date text is `yyyy-MM-dd`; canonical DateTime text is UTC with seven fractional digits and `Z`. Older readers and validators reject the shape rather than interpreting it as text comparison. This does not change ordinal equality in specification expectations, keys or constraints. See [command date comparisons](commands.md#chronological-command-date-comparisons).

ESM v25 writes the distinct optional final application member `commandReadMode: "protected"`, plus each command's admitted `reads` entries (`name`, `readModel`, `key`, `presence: "required"`). Read-path expressions record `kind: "readPath"`, `read` and the view `property` id; condition property operands carry the read name. Earlier ESM readers and programmatic validators reject these members instead of silently losing protection. Omission preserves earlier bytes, version and legacy refusals. Consumers must explicitly admit the version or refuse it; reference execution and target realization are separate verdicts.

## Generation rule bodies and checksum validation

`GenerationSyntax.File` and `.Code` are nullable init-only members; old omissions remain absent in transport. They belong to `generate rule Name`, independently of a concept's provenance file. Preserve and walk them when rewriting concepts. Exactly one direct source is required for executable admission. `ValidationRuleKind.Checksum` and the semantic counterpart are appended enum members; earlier numeric values are unchanged. Checksum uses the existing rule operand and severity/message envelope.

ESM v21 generation has only authored pattern/checksum, exact Int64 min/max, or implementation requirement id members, according to strategy. No current year, support counts, proof status or body bytes serialize. A ValueGeneration requirement and its Now/Tenant typed-context descriptor belong to the same compilation and attachment manifest; an ESM-only reader cannot invent them. Consumers explicitly admit v21 or reject it. Earlier ESM and corpus bytes are frozen.

## Navigation regions

`LayoutSyntax.Navigation` and `ModuleSyntax.Navigation` are additive init-only collections of `NavigationRegionSyntax`; the JSON omits them when empty, so documents without navigation regions keep their bytes. `NavigationRegionKind` (`Topbar`, `Sidebar`) and `NavigationRegionMode` (`Unspecified`, `Inherit`, `Extend`, `Override`) are appended-only enums: a layout's regions are `Unspecified`, a module's carry their mode. `NavigationItemSyntax` nests sub-items in `Children`, and `NavigationTargetSyntax` holds the module and the dotted feature path. See [Navigation regions](navigation.md).

`ScreenToolbarSyntax.Buttons` is an additive init-only collection of `ToolbarButtonSyntax` (`Id`, `Title`, optional `Slice`, `Emphasis`, `Icon`). The JSON omits `buttons` when empty and the optional members when null. `ToolbarEmphasis` (`Primary`, `Secondary`, `Quiet`) is append-only. See [Toolbar buttons](screens.md#toolbar-buttons).

## Enum member numbers

`ConceptSyntax.ValueNumbers` is a nullable init-only collection of int32, not a positional constructor parameter. Absent numbers stay null and the syntax JSON omits `valueNumbers`, keeping existing documents' bytes. Populated numbers parallel `values` in authored order; cardinality must match, numbers must be unique, and the concept must be an enum. Preserve the distinction between absent and populated numbers when rewriting. Positional pins retain the original executable bytes; other numbers select ESM v12 and become the optional canonical `numbers` member, parallel to ESM `values`. Consumers must explicitly admit that contract.

## Key and routing members

`PropertySyntax.IsKey`, `QuerySyntax.ByParts`, `ReadsSyntax.ByParts`, `ProducesSyntax.Stream`, `ReactionSyntax.From` and `ReducerSyntax.From` are additive init-only properties. Constructors and Deconstruct signatures stay unchanged. Missing members default to false, empty collections or null. Writers omit those defaults. Preserve authored key-part and mapping order when rewriting nodes.

A pending `HandlerSyntax.Implementation` may coexist with unconditional `CommandSyntax.Produces`. This selects ESM v23 without a new syntax node: the command's final optional `handler` member carries `requirementId` and `resultVersion: 1`, while ordered hints and Pending attachment state stay in the sidecar. Required event payload properties may be unmapped because `produces` is the result contract, not its implementation. Older ESM readers must refuse v23 rather than discard the described body. File/inline handlers remain unadmitted; commands without a described handler retain their prior bytes. See [Handler intent edits](ast-authoring.md#handler-intent-edits).

`ObserverFilterSyntax` carries an event source name, optional stream name and location. Routes and filters select ESM v10. Explicit single read-model keys preserve ESM bytes. Flat composite read-model identities mark several properties as identifiers, use `keyParts` for single-instance query arguments and `kind: "parts"` for projection keys, and select their own executable contract. Earlier readers reject these shapes. See [AST authoring](ast-authoring.md#keys-production-routes-and-observer-filters).

`RemoveWithSyntax.PartsKey` is a nullable init-only `CompositeKeySyntax`, beside the existing scalar `Key`. It holds the untyped `key` child block, defaults to null and is omitted from syntax JSON when absent. The walker visits its parts and expressions. Preserve it during source recovery; an earlier transport reader rejects the new member. A removal with both scalar and parts keys is invalid.

## Reaction command identity member

`ReactionSyntax.RunsAs` is a nullable init-only `ReactionIdentitySyntax`, not a new constructor or Deconstruct parameter. `ReactionIdentitySyntax(Kind, Roles, Location)` is an additive node; its CLR `Kind` uses `syntaxKind` in typed syntax JSON to avoid the node discriminator. The walker gains `VisitReactionIdentity`, and generated transport and collection schemas describe the roles. Preserve `RunsAs` when rewriting a reaction. Old syntax JSON may omit the member; its default is null. A declared identity binds to the reaction's `runsAs` member and selects ESM v10; omission preserves existing ESM bytes and revisions.

## Public event source-authoring members

`EventSyntax` and `ImportSyntax` gain init-only `Visibility` (`EventVisibility.Private` by default)
and nullable `Origin`. An origin-bearing declaration/import must have `Public` visibility.
`SliceSyntax` gains nullable init-only `Direction`; its computed `EffectiveDirection` interprets
an omitted Translate direction as inbound and is not serialized. Constructors and Deconstruct
signatures are unchanged. Missing old JSON members default to Private/null/null, and writers omit
these defaults, preserving legacy syntax bytes. Explicit Public, origin and direction values
participate in structural equality and survive typed JSON and canonical printing.

C# `SyntaxSchema` owns the new optional fields; the generated transport/TypeScript mirrors are
regenerated through the owning schema specification, not edited by hand, and the TypeScript parser reads
and writes the same members. Any nondefault public-event metadata or explicit
direction refuses semantic compilation with `PLAY0268` (#481/#480); source authoring acceptance
must not be confused with executable readiness. Preserve these fields when rewriting nodes.

Event-target projections and reducers (`projection X => PublicEvent`, #482) and `source events` captures
(#483) add no AST member. A projection's or reducer's `ReadModel` text and a capture source's `Kind`
(`events`) and `from` settings carry them: whether `=>` names an event or a read model is decided by
resolution against the assembled model, never by the parser. `CaptureEventsSource` (C#) and
`consumedEvents` (TypeScript) read the consumed events. Both refuse semantic compilation with `PLAY0268`.

## Compliance marker members

`ConceptAttributeSyntax.Name` keeps the wire values `pii` and `sensitive`, even though source uses canonical `pii`/`secret` and accepts the `personal` alias. `Scope`, `SpecialCategory` and `Criminal` are additive init-only properties. Old JSON omissions default to null/null/false; writers omit those defaults to preserve existing syntax bytes. Preserve explicitly declared values when rewriting a concept. Compliance markers select ESM v13 and bind to optional `SemanticConcept.Compliance`; reasons remain report-only. Personal takes precedence over secret, and no secret scope is stored next to personal. Identity and enum compliance, and protected events with an explicit `subject` mark, are refused; see [concepts](concepts.md).

## Composite stream id members

`EventStreamIdPartSyntax(Name, Type, Location)` is additive. `EventStreamSyntax.StreamIdParts` contains declarations in identity-bearing declaration order. `CommandStreamSyntax.StreamIdParts` and `SpecificationStreamSyntax.StreamIdParts` contain `PropertyMappingSyntax` in authored mapping order. These init-only collections default to empty, including old JSON omissions; existing positional constructors and scalar members are unchanged. A nonempty parts list is mutually exclusive with `StreamId`. Part names are not workspace references; part types are ordinary type references. The walker visits parts, their types and route mappings. Composite stream ids are admitted by ESM v8. Models using them select that version; models without sources or routes retain their existing ESM version and bytes.

## Caller expression node

`IdentityExpressionSyntax(Path, Location)` is an additive expression kind for `$identity.<path>`.
`Path` contains only the text after `$identity.`. `VisitIdentityExpression` is an additive virtual
walker method. `$context.identity.<path>` keeps its existing `ContextExpressionSyntax` kind and path;
the printer preserves both spellings. The two forms bind to the same executable expressions and add
no ESM member or version. Syntax transport consumers must support the new kind or reject it explicitly.

## Caller detail metadata

`ApplicationSyntax.Identity` is a nullable init-only property, never a new positional parameter.
`IdentitySyntax` carries typed `IdentityDetailSyntax` entries with claim, query, inline code or file source
nodes. Absent identity metadata is omitted from syntax JSON, preserving existing documents' bytes.
The walker visits all source children; syntax schema and TypeScript transport mirrors include the new
kinds. Preserve the property during rewrites. The block adds no ESM member or version; executable reads
of declared details refuse with `PLAY0268` until admitted (#600).

## What is not guaranteed

**The positional parameter list.** Inserting a parameter, removing one, promoting an optional one to required, or appending a new one is a breaking change. The first three break source and binary; appending breaks binary only, which is the worse of the two to discover. Any of them is allowed in a major release and is enumerated in the release notes when it happens. If you construct nodes yourself, prefer named arguments and object initializers; if you consume them, prefer property access over deconstruction.

**Exhaustive `switch` over an abstract base.** A `switch` on `ExpressionSyntax` or `ProjectionBlockSyntax` with an arm per known form compiles today and still compiles tomorrow, but a form added later falls through. C# cannot check exhaustiveness over a type hierarchy, so this is a silent behavior change, not a compile error. Always write a `default` arm, or override the walker's base-kind method (`VisitExpression`, `VisitProjectionBlock`, …) so the fallback is handled for you.

**A collection's element types.** `IEnumerable<ProjectionBlockSyntax>` can start yielding a form your code has never seen. Code that casts elements to a concrete type rather than pattern-matching them will fault.

**Hand-written traversal.** Nothing protects a consumer that walks the tree with its own loops. A node gaining a child collection is invisible to it - the tree grew and the consumer silently stopped covering the document. This is the concrete reason to prefer the walker: a hand-written walk has no place for the language to tell you it changed.

## The safe way to consume the tree

1. Derive from `ScreenplaySyntaxWalker` rather than writing the loops.
2. Override the narrowest methods that answer your question, and call `base` unless you mean to prune.
3. Pattern-match node forms; never cast, and always have a fallback.
4. Treat an unrecognized form as "not my concern", not as an error.
5. Read diagnostics by [`Code`](diagnostics.md), never by message text.

A consumer that does these five things is affected by a Screenplay release only when a construct it actively handles changes shape.

## See also

- [Visitors and traversal](visitors.md) - the walker and the root visitor interfaces.
- [Diagnostics](diagnostics.md) - the diagnostic code catalogue and its stability rules.
- [Printing and generating](printing.md) - constructing nodes yourself and rendering them back to text.
