# Visitors and traversal

Screenplay's job is to make a `.play` document compilable to anything - C#, TypeScript, a diagram, a database schema, a checklist for an auditor. The compiler gets you a syntax tree. Turning that tree into your own representation is what this page is about.

There are two surfaces, and they answer different questions. The **root visitors** answer "hand me the tree". The **walker** answers "call me for the parts I care about". Reach for the walker first - it is the one that keeps working when the language grows.

## The problem the walker solves

Without a traversal layer, consuming the tree means writing the walk yourself: loop the modules, loop the features, loop the slices, loop the commands, remember that a feature can nest inside a feature, remember that a `children` block in a projection contains more blocks. Every consumer writes that code, every consumer writes it slightly differently, and every consumer has to revisit it when the language grows a construct.

`ScreenplaySyntaxWalker` writes it once. It has one `Visit<Kind>` method per node kind, and the default implementation of each one visits that node's children. You derive from it and override only the kinds you care about:

```csharp
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

public class EventCollector : ScreenplaySyntaxWalker
{
    public List<EventSyntax> Events { get; } = [];

    public override void VisitEvent(EventSyntax syntax)
    {
        Events.Add(syntax);
        base.VisitEvent(syntax);
    }
}

var compiler = new ScreenplayCompiler();
var result = compiler.Compile(source);

var collector = new EventCollector();
collector.VisitApplication(result.Value!);
```

That is the whole consumer. It never mentions modules, features or slices, yet it finds every event in the document - including events inside a feature nested three levels deep, because the base class already knows how to get there.

## Overriding one kind leaves the rest alone

The default implementations are the point. A method you do not override still visits its children, so a walker that overrides `VisitEvent` is unaffected by commands, screens, projections, captures and specifications - and stays unaffected when the language grows a construct it has never heard of. A new node kind arrives as a new `Visit` method plus a call to it from the walk of its parent; your subclass compiles unchanged and keeps walking.

That is the compatibility guarantee this API exists to give. See [Syntax tree compatibility](ast-compatibility.md) for what else the tree promises.

## Calling `base` continues; not calling it prunes

An override that calls its `base` implementation descends into the node's children. One that does not stops there:

```csharp
public class ModuleShapeOnly : ScreenplaySyntaxWalker
{
    public List<string> Slices { get; } = [];

    // No base call - the walk stops at the slice and never looks inside it.
    public override void VisitSlice(SliceSyntax syntax) => Slices.Add(syntax.Name);
}
```

Both are supported and both are useful. Pruning is how you skip a branch you have no use for - a code generator that only needs the module and feature shape should not pay for walking every screen directive.

## Seeing every node at once

`VisitNode` runs for every node in the tree, whatever its kind, before that node's own method visits its children. Override it alone and you see everything:

```csharp
public class NodeCounter : ScreenplaySyntaxWalker
{
    public Dictionary<string, int> Counts { get; } = [];

    public override void VisitNode(SyntaxNode node)
    {
        var kind = node.GetType().Name;
        Counts[kind] = Counts.GetValueOrDefault(kind) + 1;
    }
}
```

This is the hook to use for anything cross-cutting - collecting every `SourceLocation`, building an index, measuring a document. It also sees node kinds that did not exist when you wrote the code, which a set of kind-specific overrides cannot.

The walk is pre-order, and children are visited in the order the construct is written in a document rather than the order the record declares its parameters - the same order [the printer](printing.md) writes them in.

## The four roots

The walker has four entry points, matching the four things the compiler can produce:

| Method | Starts from | Compiled by |
|---|---|---|
| `VisitApplication(ApplicationSyntax)` | a whole `.play` document | `Compile(string)` |
| `VisitProjection(ProjectionSyntax)` | a standalone projection | `CompileProjection(string)` |
| `VisitSpecification(SpecificationSyntax)` | a standalone specification | `CompileSpecification(string)` |
| `VisitCapture(CaptureSyntax)` | a standalone capture | `CompileCapture(string)` |

Sub-language fragments compile and walk on their own exactly as they always have - a tool that only understands projections never has to construct an application around one.

Every other `Visit` method is public too, so a walk can start part way down. Handing a single `SliceSyntax` to `VisitSlice` walks that slice and nothing else, which is what an incremental generator wants when one slice changed.

## Method naming

The name of a method is the node type's name without the `Syntax` suffix, prefixed with `Visit`: `CommandSyntax` becomes `VisitCommand`, `ScreenTableSyntax` becomes `VisitScreenTable`, `CaptureAppendSyntax` becomes `VisitCaptureAppend`. There is no overload set - each kind has its own name - so adding a kind can never change which method an existing call resolves to.

Where a node kind is an abstract base with several concrete forms - an expression, a policy condition, a projection block, a screen directive, a mapping, a key - the base method dispatches to the concrete one. Override the base method to treat every form alike, or a concrete method to single one out:

```csharp
public class EnvironmentUsage : ScreenplaySyntaxWalker
{
    public List<string> Names { get; } = [];

    // Only $env references; every other expression form walks as usual.
    public override void VisitEnvironmentExpression(EnvironmentExpressionSyntax syntax) => Names.Add(syntax.Name);
}
```

A form the walker does not recognize - one from a newer version of the language, or one a sub-language contributes - reaches `VisitNode` and is then left alone rather than raising an error.

## The root visitor interfaces

The `IApplicationSyntaxVisitor<T>`, `IProjectionSyntaxVisitor<T>`, `ISpecificationSyntaxVisitor<T>` and `ICaptureSyntaxVisitor<T>` interfaces are still there, and the compiler still drives them:

```csharp
var result = compiler.Compile(source, myApplicationVisitor);
```

They are a one-call handoff: the compiler gives your visitor the root and your visitor produces a value. That is the right shape when your consumer really is a single transformation of the whole document - [the printer](printing.md) implements all four, and `Compile(source, visitor)` gives you the result and the diagnostics in one `CompilationResult`.

It is the wrong shape when your consumer cares about parts of the tree, because nothing dispatches for you. `IModuleSyntaxVisitor<T>`, `IFeatureSyntaxVisitor<T>`, `ISliceSyntaxVisitor<T>` and `IConstraintSyntaxVisitor<T>` are composition helpers for that style - a slice visitor a feature visitor calls - not a traversal engine. Use them to structure a transformation you are writing by hand; use the walker when you want the walk given to you.

The two compose. Compile once, then walk:

```csharp
var result = compiler.Compile(source);
if (result.Success)
{
    myWalker.VisitApplication(result.Value!);
}
```

## See also

- [Syntax tree compatibility](ast-compatibility.md) - what the tree guarantees and what changes break you.
- [Printing and generating](printing.md) - the other direction, tree to text.
- [Compiler and CLI](tool.md) - text to tree.
- [Sub-language pluggability](sub-languages.md) - how projections and captures layer into the grammar.
