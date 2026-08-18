# Syntax tree compatibility

The syntax tree is a public API. Every consumer that compiles Screenplay to something - a code generator, a designer, a documentation tool - is written against these types, so what the tree promises across versions decides how much work a Screenplay upgrade costs them.

This page states those promises. It is deliberately narrow: a guarantee you cannot rely on is worse than one that was never made.

## What the tree is

Every node is a positional `record` deriving from `SyntaxNode`, which carries a `SourceLocation`. Nodes are immutable, compare by value, and support `with` expressions. A construct with several forms - an expression, a policy condition, a projection block, a screen directive, a projection mapping, a projection key, a validation block, a constraint - is an abstract base record with a concrete record per form.

That shape is stable. Nodes will stay records, stay immutable, and keep deriving from `SyntaxNode`.

## What is guaranteed

**A node kind can be added.** New constructs arrive as new record types. If the new type derives from an existing abstract base, it starts appearing in collections you already read - see the caveat below. If you consume the tree through [`ScreenplaySyntaxWalker`](visitors.md), a new kind arrives as a new `Visit` method with a default implementation that walks its children, so your subclass compiles unchanged and keeps walking. That is the property the walker exists to give you.

**An unknown form never throws.** The walker dispatches abstract bases with a fallback: a concrete form it does not recognize is passed to `VisitNode` and then left alone. A tool compiled against an older Screenplay does not fault on a document using a newer construct - it silently does not act on it, which is the recoverable failure.

**Enum members keep their numeric values.** `SliceType`, `ValidationRuleKind`, `ComparisonOperator`, `LogicalOperator`, `AutoMapMode` and `CaptureWhenKind` gain members at the end and never renumber existing ones. This one matters more than it looks: the C# compiler inlines an enum constant into the consuming assembly, so renumbering a member changes behavior in an already-compiled consumer with no error anywhere - the value it was built with silently means something else. New members are therefore always appended.

**`public const` values never change.** The [diagnostic codes](diagnostics.md) are `public const string`, and a `const` inlines into consumers exactly the way an enum member does. A code's value is fixed once published; a diagnostic that changes meaning gets a new code rather than a new value on the old one. The same rule applies to every other published constant, such as `QuerySyntax.ObservableModifier` and `PropertySyntax.IdentifierModifier`.

**Optional record parameters are appended, never inserted.** A node gaining an optional value gains it as a new trailing parameter with a default, so existing positional construction and `with` expressions keep compiling.

## What is not guaranteed

**Positional parameter order.** Inserting a parameter, or promoting an optional one to required, is a breaking change - source and binary. It is allowed in a major release and is enumerated in the release notes when it happens. If you construct nodes yourself, prefer named arguments; if you consume them, prefer property access over deconstruction.

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
