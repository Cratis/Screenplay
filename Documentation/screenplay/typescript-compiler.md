# TypeScript compiler

The C# compiler is the authority on Screenplay, but not everything that reads a `.play` document runs .NET. An editor extension, a web page or a build script written in TypeScript needs the same syntax tree without starting a process. `@cratis/screenplay-compiler` parses `.play` documents in TypeScript into the syntax tree the C# compiler produces, for the constructs that describe an event model. The [VS Code extension](vscode.md) uses it to draw the event model board.

It parses; it does not validate. Reference resolution and the semantic checks the C# compiler runs after parsing are not part of it. A document the C# compiler rejects for a semantic reason is one this compiler reads without complaint, so keep the C# compiler, or the [CLI](tool.md), as the gate.

## Parse a document

`parse` turns the text of one document into an `ApplicationSyntax`, with the diagnostics found on the way:

```typescript
import { parse } from '@cratis/screenplay-compiler';

const result = parse(source, 'Projects/Projects.play');

for (const diagnostic of result.diagnostics) {
    console.log(`${diagnostic.location.path}(${diagnostic.location.line}): ${diagnostic.code} ${diagnostic.message}`);
}

const modules = result.value.modules.map(module => module.name);
```

A tree is always produced. A document with errors still yields everything that could be read, and `result.success` says whether there were errors. Diagnostics carry the C# compiler's `PLAY` codes, so a code means the same thing in either language. The path is optional: give one when the document is part of a folder, and every location in the tree carries it.

## Compile a folder as one application

A [folder of `.play` files](folders.md) is one application. `parseFolder` merges the documents the way `CompileFolder` does: in ordinal order of their paths, with modules and features of the same name combined, and a concept, type or slice declared in two files reported:

```typescript
import { parseFolder } from '@cratis/screenplay-compiler';

const result = parseFolder([
    { path: 'application.play', source: applicationText },
    { path: 'Projects/Projects.play', source: projectsText },
]);
```

The compiler reads no files itself. You pass each document's path, relative to the folder, and its text, so the same call works in Node, in a browser and in an editor whose open files have unsaved changes.

## Turn the tree into your own artifacts

The [visitors and the walker](visitors.md) have TypeScript counterparts with the same names and the same order of traversal. A visitor is handed the whole tree:

```typescript
import { ApplicationSyntax, ApplicationSyntaxVisitor, compile } from '@cratis/screenplay-compiler';

class SliceNames implements ApplicationSyntaxVisitor<string[]> {
    visit(syntax: ApplicationSyntax): string[] {
        return syntax.modules.flatMap(module => module.features.flatMap(feature => feature.slices.map(slice => slice.name)));
    }
}

const names = compile(source, new SliceNames()).value;
```

`ScreenplaySyntaxWalker` visits every node it models, depth first, and you override the kinds you care about. Features nest, so walk with the walker rather than with loops of your own:

```typescript
import { EventSyntax, ScreenplaySyntaxWalker, parse } from '@cratis/screenplay-compiler';

class EventCollector extends ScreenplaySyntaxWalker {
    readonly events: EventSyntax[] = [];

    override visitEvent(syntax: EventSyntax): void {
        this.events.push(syntax);
        super.visitEvent(syntax);
    }
}

const collector = new EventCollector();
collector.visitApplication(parse(source).value);
```

Unlike the C# overload, `compile` runs the visitor even when the document has errors, over everything that could be read. An editor wants to show a document while someone is typing it.

`@cratis/screenplay-event-models` is one such visitor. It turns an application into the document the Cratis event model board (`@cratis/event-models`) draws:

```typescript
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '@cratis/screenplay-event-models';

const board = toEventModelDocument(parse(source).value, 'Invoicing');
```

The ids in that document are derived from where each element sits in the model. Compiling the same model again gives the same ids, so a board keeps what it remembers about each element, such as what is collapsed, across edits.

## What it reads

The compiler reads what an event model is made of:

- the domain, imports, concepts and types
- modules, features (nested too) and slices
- events, read models and descriptions
- commands with their properties and declarative `validate` rules
- queries with their parameters
- the events each projection block consumes
- reaction triggers (`when`, `every`, `at`)
- unique and file constraints
- specifications with the values they state

Everything else is recognized and skipped whole, without a diagnostic. That covers:

- screens, captures, reducers, forms and layouts
- policies, personas, authentication, seeds and themes
- what a command produces and its handler
- projection keys and mappings
- what a trigger does

Each node carries the members of its C# record that the compiler reads, under the same camelCase names `SyntaxJson` writes. A member it does not read is absent rather than empty, because an empty list would claim the document declared nothing there.

## How it is kept in step with the C# compiler

Both compilers are held to the same golden files. For a shared corpus of `.play` documents, `Source/Screenplay/Compiler/Conformance` holds the syntax the TypeScript compiler reads, in the [canonical JSON form](ast-compatibility.md) narrowed to the members it reads. A TypeScript spec fails when the compiler no longer produces those files. A C# spec fails when the C# compiler's `SyntaxJson` disagrees with any member in them. Neither compiler can change how it reads a construct without one of the two failing.

When a change to the TypeScript compiler is intended, rewrite the golden files and review the diff:

```shell
SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1 yarn workspace @cratis/screenplay-compiler test
```

To hold a new construct to the C# compiler, add it to `Conformance/constructs.play`, or add a document to `Conformance/manifest.json`.
