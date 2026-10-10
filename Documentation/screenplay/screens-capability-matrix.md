# Screens capability matrix

This page traces each language-level screens release criterion to the Screenplay syntax that expresses it, how the
compiler treats it, the diagnostics that guard it, and anything the language cannot yet express. It covers the
authoring language only. Rendering and runtime proof live in the
[screens release acceptance matrix](screens-release-acceptance.md).

"Executable model" means the backend executable semantic model (ESM). UI constructs are explicitly deferred from it
with `PLAY0269`; they are authored, validated, printed and exported, and Stage plans them from the syntax tree.

## Screens, components and bindings

| Criterion | Syntax | Semantics | Diagnostics | Not expressible |
| --- | --- | --- | --- | --- |
| Screen identity (536.2) | `screen <Name>` in a slice; `description` | Authoring; `PLAY0269` in the ESM | `PLAY0103` malformed directive | None |
| Package components and exact stable ids (536.2) | `component <Package.Component> <name>`, `id "<stable-id>"` | Authoring; the stable id is never normalized | `PLAY0103`, `PLAY0632` package not in a ui profile | None |
| Typed bindings (536.2) | `from data`, `from query`, `from component`, `from literal`, `mode`, `null`, `expected` | Typed `UiBindingSyntax`; invalid text kept as `rawText` | `PLAY0103` | None |
| Typed literals (536.2) | `property <path> = <string, number, bool, null, array or object>` | Typed `ExpressionSyntax` | `PLAY0103` | None |
| Exposed values (536.2) | `exposes <name> from <binding>` on components; `exposes <name> <Type>` on templates | Authoring | `PLAY0103`, `PLAY0026` | None |
| Toolbars and presentation (536.2) | `toolbar`, `item`, `presentation`, `icon` | Authoring | `PLAY0103` | None |

## Forms

| Criterion | Syntax | Semantics | Diagnostics | Not expressible |
| --- | --- | --- | --- | --- |
| Command forms (536.2) | `form <Name> for <Command>`, `field`, `populate`, `on submit` | Authoring | `PLAY0208`–`PLAY0216` | None |
| Generation mode and geometry, Scene 4.12 (536.2) | `generation auto\|manual`, `layout` with `column`, `place`, `columnGap`, `rowGap` | `CommandFormLayoutSyntax` maps 1:1 to Scene `CommandFormLayout` | `PLAY0210` malformed generation or layout lines | None |

## Templates, layouts and composition

| Criterion | Syntax | Semantics | Diagnostics | Not expressible |
| --- | --- | --- | --- | --- |
| Template categories and assignment (536.2, 536.3) | `layout`, `screen template`, `dialog template`, `category`, `type`, `template <Name>` at module, feature or slice | Hierarchical; the nearest assignment wins | `PLAY0026`, `PLAY0631` scope mismatch | None |
| Template picker metadata | `display`, `description`, `scopes` | Scene `DisplayName`, `Description`, `Metadata.Scopes` | `PLAY0026`, `PLAY0631` | None |
| Template content | `content <slot>` with screen directives | Scene template `Content` | `PLAY0629` unknown slot, `PLAY0649` nesting cycle | None |
| Arrangements | `arrangement flow\|freeform`, `row`, `column`, `grid columns rows`, `grow`, `span`, `when`, `variant`, `place` | Scene `FlowArrangement`, `FreeformArrangement` | `PLAY0235`, `PLAY0236` | None |
| Exposures | `exposure for <Owner>`, `property <component>.<path>` with `label`, `operations`, `fields`, `reexposes` | Scene `ExposureDeclaration` | `PLAY0621`–`PLAY0624` | None |
| Instance values | `instance <X>` with `set` and `items` | Scene `InstanceContribution` | `PLAY0625`–`PLAY0628` | None |
| Screen contributions | `contribute to <Point> [order <n>]` inside a screen | Scene `Screen.Contributions` | `PLAY0648` unknown point | None |
| Recursive outlets (536.3) | `outlet <name>` on layouts, templates and components | Authoring | `PLAY0630` unknown outlet | None |

## Navigation

| Criterion | Syntax | Semantics | Diagnostics | Not expressible |
| --- | --- | --- | --- | --- |
| Navigation targets and URL overrides (536.3) | `navigate to <Screen> [by <param>]`, `route`, `outlet`, `parameter` | Scene `DestinationReference` | `PLAY0107`, `PLAY0197` unknown screen, `PLAY0630` | None |
| Navigation items | `contribute to <Point>` with `navigate`, `label`, `order`, `id`, `icon`, `presentation`, `group`, `destination outlet\|dialog\|external` | Scene `NavigationItem` and `DestinationKind` | `PLAY0217`–`PLAY0224`, `PLAY0647` unknown destination | None |
| Dialog placement (536.3) | `open dialog <DialogTemplate>`, `destination dialog <DialogTemplate>` | Authoring | `PLAY0647` | None |

## Packages and design-time output

| Criterion | Syntax | Semantics | Diagnostics | Not expressible |
| --- | --- | --- | --- | --- |
| Packages and icons (536.2) | `packages` and `icons` in a `ui profile` | Authoring | `PLAY0632` component from an undeclared package | None |
| Design-time output (536.6) | A design-time action's result is ordinary syntax: `instance` values, `form` fields and `layout` | Package callbacks stay in the package; only their output is source | The diagnostics of the constructs it produces | Package code and callbacks, by design |

## Preservation and tooling

| Criterion | Proof |
| --- | --- |
| Parser-printer-parser preservation (536.4) | `when_printing_screen_composition`, `when_printing_screen_release_ui_contract` |
| References and cycles (536.4) | Corpus rejection vectors for template nesting cycles, re-exposure cycles, out-of-scope screen references, missing outlets and destinations |
| .NET and TypeScript parity (536.5) | Shared transport schema, golden syntax vectors and `Conformance/diagnostics.json` |
| Language service (536.5) | `when_surfacing_screen_composition_diagnostics` (validation, keywords, highlighting, native diagnostic markers) |
| Authoring separate from executable readiness (536.5) | UI constructs are authored and validated; the ESM reports them as `PLAY0269` deferred |
| MCP remove and move (537.1) | `when_moving_and_removing_screen_nodes` |
| MCP missing package (537.3) | `when_moving_and_removing_screen_nodes`: removing a package a component still uses is refused with `PLAY0632` |

The TypeScript compiler parses and preserves every construct above. The reference checks (owners, instances, cycles,
outlets, destinations, scopes, contribution points, packages) run in the C# compiler; the language service shows them
when a Monaco host supplies the native compiler's diagnostics.
