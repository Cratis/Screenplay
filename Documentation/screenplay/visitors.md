---
title: "Screenplay visitors: traverse a syntax tree"
description: "Use ScreenplaySyntaxWalker to inspect selected nodes, or a root visitor to transform a parsed Screenplay document into your own representation."
---

# Visitors and traversal

Use `ScreenplaySyntaxWalker` to inspect selected nodes in a parsed Screenplay document without writing the traversal yourself. A root visitor serves a different task: it receives the whole tree and returns your representation, such as a diagram or a documentation index.

Start with the walker when you need particular declarations. Use a root visitor when you already own the whole transformation. The [TypeScript compiler](typescript-compiler.md) provides corresponding APIs for the syntax it models.

Both APIs work with written structure, not resolved executable meaning. If you need to generate target code from semantic identities and admitted capabilities, use the [rendering contract](interoperability.md#level-3-screenplay-to-artifact-rendering). To write a changed syntax tree back to `.play`, use the [printer](printing.md).

## The problem the walker solves

A loop over modules, features and slices can miss a nested feature. Projection `children` blocks introduce another nesting level, and inline events need visiting too. Maintaining those loops in every consumer makes each consumer responsible for new language constructs.

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

The collector records each event as the walker reaches it, including events in nested features and command-inline declarations. Calling `base.VisitEvent` keeps traversal going through the event's children; the collector does not need to know which containers led to that event.

## Overriding one kind leaves the rest alone

A method you do not override still visits its children. When the library adds a node kind, it adds a `Visit` method and a call from the parent traversal. Your subclass can keep using the inherited traversal without adding an override for that kind.

This protects consumers from having to enumerate every node kind themselves. See [Syntax tree compatibility](ast-compatibility.md) for the API's versioning contract.

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

Use `IApplicationSyntaxVisitor<T>`, `IProjectionSyntaxVisitor<T>`, `ISpecificationSyntaxVisitor<T>` or `ICaptureSyntaxVisitor<T>` when the result comes from a transformation of an entire root. The compiler can invoke the visitor for you:

```csharp
var result = compiler.Compile(source, myApplicationVisitor);
```

They are a one-call handoff: the compiler gives your visitor the root and your visitor produces a value. That is the right shape when your consumer really is a single transformation of the whole document - [the printer](printing.md) implements all four, and `Compile(source, visitor)` gives you the result and the diagnostics in one `CompilationResult`.

A root visitor does not dispatch to child visitors automatically. `IModuleSyntaxVisitor<T>`, `IFeatureSyntaxVisitor<T>`, `ISliceSyntaxVisitor<T>` and `IConstraintSyntaxVisitor<T>` help you compose a manual transformation: a feature visitor can call a slice visitor, for example. Use the walker instead when you want inherited child traversal.

In C#, the compiler's visitor overload runs only when compilation succeeds. For the same boundary with a walker, compile first and check the result before walking:

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
- [Build a renderer target](https://github.com/Cratis/Stage/blob/main/Documentation/guides/build-renderer-target.md) - turn the executable semantic model into deterministic framework code through Stage artifact plans.
