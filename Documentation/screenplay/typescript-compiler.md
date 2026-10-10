# TypeScript compiler

Quoted and fenced descriptions on concepts, policies, constraints, projection headers and screens are retained in the parsed tree. Form bodies remain outside the narrow source-to-syntax projection; their descriptions are validated with the same grammar and are available in full typed syntax JSON from the native compiler. The full transport schema preserves descriptions on all six kinds. This does not promise TypeScript executable binding.

The C# compiler is the authority on Screenplay, but not everything that reads a `.play` document runs .NET. An editor extension, a web page or a build script written in TypeScript needs the same syntax tree without starting a process. `@cratis/screenplay-compiler` parses `.play` documents in TypeScript into the syntax tree the C# compiler produces, for the constructs that describe an event model. The [VS Code extension](vscode.md) uses it to draw the event model board.

It parses and checks inline-event declaration collisions and destination consistency; it does not bind an executable model. Public/private event boundaries are checked against scoped declarations in the assembled application, including imported contracts and seed appends. General reference resolution and the remaining semantic checks the C# compiler runs after parsing are not part of it. A document the C# compiler rejects for a semantic reason is one this compiler reads without complaint, so keep the C# compiler, or the [CLI](tool.md), as the gate. [Editor diagnostic support](editor-diagnostics.md) describes the validation boundary and which checks each surface runs.

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

For an assembled tree authored programmatically or decoded from AST JSON, `publicEventDiagnostics(application)` returns public metadata and usage diagnostics with the same codes, messages, severities and locations. It checks operational edges, not specification fixtures. Projection `all` subscribes to every declared event and public import; `every` only maps existing inputs. Unknown and ambiguous references are not classified as private. Event-target projections and reducers (`=>` resolving to an event) and `source events` captures need no AST member: the collector reads the projection target and the capture source's `from` settings (`consumedEvents`), so `PLAY0618`–`PLAY0620` and the input/output checks apply to them exactly as in the C# compiler. Validation does not change semantic admission.

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
- application-owned event sources, nested streams and command-level stream/key authoring, with complete-input ambiguity checks (syntax-only)
- systems, operations and their phase/specification intent (syntax-only)
- generated command values, response contracts, fixtures and return expectations (syntax shared with C# ESM v7 admission)
- modules, features (nested too) and slices
- fenced Markdown documentation on modules, features, slices, commands, read models and reactions, and quoted or fenced-text specification descriptions (report-only authoring metadata)
- guarded screen actions with nearest-subject field paths, command input properties and collection cardinality, and provable alternative shadowing (PLAY0345–PLAY0348)
- standalone and inline events with their tags, descriptions, documentation, and rename pins
- standalone public-event visibility, opaque event/import origins, and optional inbound/outbound Translate direction (syntax-only)
- commands with their properties, declarative `validate` rules, and productions (typed mappings, destinations and `produces when` conditions included)
- queries with their parameters
- the events each projection block consumes
- reaction triggers (`when`, `every`, `at`), invocation refusal branches and their scoped selector/value checks
- redelivery specifications, including observer resolution and unique given-occurrence matching (syntax-only)
- captures, with their sources, `when` conditions, mappings and appended events
- personas, policies, seeds and declared triggers
- projection keys and mappings, including variants
- unique and file constraints
- specifications with the values they state, structured values included
- screens with their data, actions, navigation, titles, tables, summaries, sections, template slots and inline code

Systems and operations remain syntax-only and unadmitted. Event sources, streams, command and specification routes, and composite stream ids are admitted by ESM v8 in the C# binder and reference runner. Generated values and responses are admitted as ESM v7. The TypeScript compiler has no ESM binder or executor; its syntax validation is not proof of semantic admission, fixture completeness or execution. Pre-generation references (`PLAY0273`) and generated concepts with rules (`PLAY0268`) are C# semantic checks, not additional TypeScript syntax rules.

Everything else is recognized and skipped whole, without a diagnostic. That covers:

- reducers (a slice keeps only each reducer's name, read model and events, for the dependency graph), forms, layouts, behaviors and screen templates
- the interaction a screen binds with `on` and `uses`, which it keeps only as a marker in the screen's directives
- authentication, themes and UI profiles
- command handler execution; handler implementation intent retains its typed hints and selected file/code
- trigger implementation code

For the syntax and authoring checks it supports, it uses the C# diagnostic codes, lines and order. [Editor diagnostic support](editor-diagnostics.md) lists the shared checks, C#-only scoped/semantic/completeness checks and the deliberate legacy absence-assertion exception.

Public-event metadata uses the C# AST members: `EventSyntax.visibility` and `ImportSyntax.visibility` (`Private` by default), nullable `origin`, and nullable `SliceSyntax.direction` (`Inbound` or `Outbound`, only on Translate slices). Origins are nonblank opaque strings, never filesystem imports or declaration references. An origin requires public visibility; a public contract import requires an origin. Default metadata is omitted from structural JSON so legacy bytes stay unchanged. Typed authoring rejects inconsistent metadata before writing it. The board document retains nondefault metadata on declared events and slices; it does not infer delivery behavior. The C# binder explicitly refuses nondefault metadata with `PLAY0268`; TypeScript has no executable binder or canonical `.play` printer.

The top-level [identity block](identity.md) is modeled, including claim, keyed-query, code and file sources. Its cross-reference checks run against the assembled application, and the walker visits its typed source children. `$identity` path warnings are deferred until declared details are known; `$context.identity` stays built-ins only. This is authoring support, not executable admission (#600).

`ProducesSyntax.inlineEvent` preserves inline authoring structure. Use `eventDeclarations(slice)` to enumerate both standalone and command-inline events; the walker visits both, and the event model board draws them with the same slice-owned identity.

Each node carries the members of its C# record that the compiler reads, under the same camelCase names `SyntaxJson` writes. A member it does not read is absent rather than empty, because an empty list would claim the document declared nothing there.

## Read exact numbers

A document that starts with the `numbers exact` preamble is read with exact numeric literals. `parse` records the mode on the tree as `sourceOptions`, which is `{ numericMode: 'exact' }` for such a document and `{ numericMode: 'legacy' }` for a document without a numeric preamble. A malformed preamble gives `numericMode: 'invalid'`. Projections and specifications carry the same member.

In exact mode a number literal is not a JavaScript number. It becomes an `ExactNumber`, `{ literalType: 'ExactNumber', value: '12.5' }`, whose `value` is canonical fixed-point text, so no digit is rounded. Trailing fractional zeros normalize away: `parseExactNumber('12.50')` returns `'12.5'`, and `SyntaxJson` rejects non-canonical forms. Exponents are accepted. A literal that does not fit the bounded Decimal domain is reported as `PLAY0511` instead of being rounded. Without the preamble, literals stay ordinary numbers. A preamble with another spelling, a second preamble, one after the domain, imports or declarations, or a `numbers` line inside a declaration is reported with `PLAY0508`, `PLAY0509` or `PLAY0510`. The codes are listed in the [diagnostics](diagnostics.md).

The preamble is syntax only. Neither compiler binds an exact-mode document yet: the C# compiler reports `PLAY0268` when one is bound.

## Query dependencies

`DependencyGraph.for(application)` computes how modules, features and slices depend on each other from the references a tree states explicitly. The [VS Code board's dependency map](vscode.md#dependency-map) is drawn from it. It needs no semantic binding, so it works on a tree that has errors.

```typescript
import { DependencyGraph, parse } from '@cratis/screenplay-compiler';

const graph = DependencyGraph.for(parse(source).value);

for (const dependency of graph.implied('feature', 'feature')) {
    console.log(`${dependency.source.address} -> ${dependency.target.address}: ${dependency.references} references`);
}
```

A reference becomes an edge between the slice that makes it (the consumer) and the slice that declares what it names (the producer). An edge to a read model points to the projection or reducer that builds the read model, which is not necessarily the slice that declares it. Each edge has one of these kinds: `usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks`, `shows`, `verifiedWith` and `outsideTheModel`, the last for an event owned by an imported bounded context. The graph exposes:

| Member | Returns |
| --- | --- |
| `nodes`, `edges` | The modules, features, slices and imported contexts, and the slice-to-slice edges with the evidence for each |
| `unresolved` | References whose target no slice declares, and `unusedImports` the imports nothing uses |
| `implied(from, to, kinds?, includeTestOnly?, evidenceLimit?)` | The edges aggregated between two levels (`slice`, `feature` or `module` as the source, plus `context` as the target), counting slice pairs and references and keeping up to `evidenceLimit` (3) sources of evidence. `verifiedWith` evidence, which comes from specifications, is left out unless `includeTestOnly` is set |
| `cycles(level, kinds?)` | Groups of mutually dependent nodes at one level, considering only the ordering kinds `usesFactsFrom`, `reactsTo` and `decidesFrom` |
| `siblingGroups(kinds?)` | The same mutual dependencies among the children of each container |
| `suggestedOrder(kinds?)` | An order for each container with producers first, keeping the authored order where nothing constrains it. It is a suggestion and is never applied |
| `traverse(address, direction, kinds?, includeTestOnly?)` | The nodes reachable from a node following `incoming` or `outgoing` edges |

A query with an unknown level, direction or dependency kind, or a negative evidence limit, throws `InvalidDependencyQuery`.

## How it is kept in step with the C# compiler

Both compilers are held to the same golden files. For a shared corpus of `.play` documents, `Source/Screenplay/Compiler/Conformance` holds the syntax the TypeScript compiler reads, in the [canonical JSON form](ast-compatibility.md) narrowed to the members it reads. A TypeScript spec fails when the compiler no longer produces those files. A C# spec fails when the C# compiler's `SyntaxJson` disagrees with any member in them. Neither compiler can change how it reads a construct without one of the two failing.

When a change to the TypeScript compiler is intended, rewrite the golden files and review the diff:

```shell
SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1 yarn workspace @cratis/screenplay-compiler test
```

To hold a new construct to the C# compiler, add it to `Conformance/constructs.play`, or add a document to `Conformance/manifest.json`.

Invalid documents are held to the C# compiler the same way. `Conformance/diagnostics.json` lists invalid documents together with the diagnostics they must produce: the code, the line and the order. Specs on both sides hold their compiler to that list. When the TypeScript compiler starts reporting something new, add a case for it there.

The compiler's specs also enforce coverage. `yarn workspace @cratis/screenplay-compiler test` fails when the share of code they exercise drops below the thresholds in its `vitest.config.mts`, so new code arrives with its specs.
