# UI profile

Screenplay's screen and template constructs are platform-agnostic on purpose - the same `screen` should work on the web, on a phone, and on desktop. Something still has to say which platforms an application targets, which shell it renders inside, and which component packages (PrimeReact, an internal widget library, ...) a build resolves widget names against. That is `ui profile`.

## Syntax

```screenplay
ui profile <Name>
  target platform <Platform>[, <Platform>...]
  target size <SizeClass>
  layout <LayoutName>
  theme <ThemeName>

  root template [blueprint <Blueprint>] <TemplateName> [version "<range>"]
    option <name> <value>
    ...

  appearance
    color scheme light|dark|system
    accent "<#hex>"

  new screens use template [blueprint <Blueprint>] <TemplateName> [version "<range>"]

  packages
    <Package>
    ...

  icons
    <IconSetOrPackage>
    ...

  sample data
    active <Pack>
    shape <Shape> maps to <ReadModel>
    bind <ReadModel> to <Pack>
```

- `ui profile <Name>` - top level, alongside authentication, modules and everything else. A document can declare more than one; each name must be unique.
- `target platform` - the platform(s) this profile targets, e.g. `web`, `ios`, `android`. At most one per profile.
- `target size` - the size class assumed by default: `compact`, `regular` or `expanded` (Apple/Material-style named classes, not raw pixel breakpoints - a narrow browser window and a compact phone are "the same" class, and a raw breakpoint means nothing on native). At most one per profile. The two-axis width x height matrix a `layout` resolves against is a separate, more specific concern.
- `packages` - the component packages this profile draws from, one per line, in **override-priority order**: a later package's `Button` shadows an earlier one's on a name collision. A package that is a blueprint can also be named by a `template blueprint <Blueprint> <Name>` reference (see [Layouts and templates](templates.md#own-and-blueprint-templates)); a reference to a blueprint no profile lists is `PLAY0722`. `core`, the built-in vocabulary (`button`, `table`, `form`, `title`, ...), is always the final fallback regardless of what a profile lists here.
- `icons` - the icon sets or packages this profile makes available to `icon <name>` references on components, toolbar items and actions. The compiler preserves the list; Stage checks whether the selected packages can resolve each icon.
- `theme` - the visual theme this profile applies. Optional, at most one per profile. A theme is only meaningful relative to a specific set of packages - see [Theme](theme.md) for how that compatibility is declared and checked.
- `sample data` - which sample data the whole application previews with: the active pack, chosen **once here** and inherited by every screen, slice and component, plus the rare shape-to-read-model mappings and per-read-model bindings. Optional, at most one per profile. It is design and preview time only and never part of a production build. See [Sample data](sample-data.md).
- `root template` - the template the whole application is drawn in: every screen's template chain is placed in its primary outlet. The reference is the one a `template` assignment uses - an own layout or application-scope screen template by name, or a template a blueprint ships, `blueprint <Blueprint> <Name>`, with an optional `version "<range>"` pin (a version range only applies to a blueprint template; `PLAY0788` otherwise). Optional, at most one per profile (`PLAY0798`). The template has to exist (`PLAY0790`), be usable as the shell - a layout, or a screen template that declares `scopes application` and fits no slot (`PLAY0794`) - and have a `content` slot (`PLAY0795`); a blueprint it comes from has to be in the profile's `packages` (`PLAY0792`). See [Layouts and templates](templates.md#own-and-blueprint-templates).
  - `option <name> <value>` - one of the shell's own configurable items, for example `option navigationMode slim`. The value is a bare word, a quoted string, a number, `true` or `false`, kept as written. Each name is set once. The grammar only reads the line. The validator then checks the name: an own shell exposes the properties of its `exposure for <Shell>` (and a shell that exposes nothing has no options), and an option it does not expose is `PLAY0796`. A blueprint shell's options are checked against the installed catalog (see below).
- `appearance` - how the application looks regardless of the shell: `color scheme` (`light`, `dark` or `system`) and `accent "<#hex>"` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`; anything else is `PLAY0797`). Optional, at most one block per profile, each line at most once (`PLAY0789`).
- `new screens use template` - the template a new screen starts from when it is created in a designer. It is an authoring default; it changes no existing screen. Optional, at most one per profile (`PLAY0788`). It has to name a template that can start a screen in a feature - not a layout or an application-scope root, and one whose `scopes` leave out `feature` is refused too (`PLAY0799`).
- `layout` - the application's base navigational shell this profile renders inside. This is the older way to name the shell and stays accepted; it is reported as `PLAY0791` (a warning) with a quick fix that rewrites it to `root template <Name>`. When a `root template` is present as well, the `layout` line is ignored. Optional, at most one per profile. Naming a layout the document does not declare is reported as a warning, the same way an unknown theme is. See [Layouts and templates](templates.md).

## Example

```screenplay
layout AppShell
  topbar
  navigation contributes Navigation
  content

theme Aurora
  compatible with core
  compatible with PrimeReact
  compatible with Internal.Widgets

ui profile Desktop
  target platform web
  target size expanded
  theme Aurora

  root template AppShell
    option navigationMode slim

  appearance
    color scheme dark
    accent "#4f46e5"

  packages
    core
    PrimeReact
    Internal.Widgets

  icons
    PrimeIcons
    Internal.Icons

ui profile Mobile
  target platform ios, android
  target size compact
```

## A screen never declares its profile

Profile selection is a build/Stage concern, not something a `screen` states about itself. The same screen resolves against different package chains per build - that is what keeps a Level 1/2 screen genuinely portable across web, mobile and desktop rather than tied to one target.

## Component name resolution

A profile's `packages` list is resolved the same way as any other bare name in Screenplay (see [Visitors and traversal](visitors.md) for the general inside-out walk this reuses): a bare widget name checks the active packages in declaration order, and a name matching two packages equally well is reported the same way an ambiguous query reference is - named candidates, not a silent pick. A widget already in `core` (a button, say) always resolves without qualification; one that only exists in a package further down the list, like an internal chart widget, has to be qualified by that package's name to be found at all.

Screenplay's own compiler has no visibility into what widgets a package like PrimeReact or an internal library actually defines - that catalog is external to the document. Resolving a bare widget name against it, and reporting the resulting ambiguity, is therefore a build-time concern owned by Stage and the package resolution engine, not something the Screenplay compiler itself checks. What the compiler does validate at compile time is the profile declaration itself: a profile name, or a package within one profile's list, is never allowed to be declared twice.

## Checking a shell against the installed blueprints

The compiler decides everything the document alone decides. What a blueprint ships - its templates, their kind, slots and options, and the
version installed - is only known where the blueprint is installed. A host that knows (Stage, the CLI, the MCP server) passes its catalog to
`UiProfileShellBinder.Bind(application, blueprints)`, which reports the rest with the same codes and messages: `PLAY0790` for a template the
blueprint does not ship, `PLAY0792` for a blueprint that is not installed, `PLAY0793` for a `version` pin the installed version does not
satisfy, `PLAY0794` and `PLAY0795` for a template that cannot be the shell or has no `content` outlet, `PLAY0796` for an option the
shell does not expose, and `PLAY0799` for a `new screens use template` that cannot start a screen in a feature. The rules are the same
ones the compiler runs - there is no second rule set - and they restate Scene's `ShellResolver` and `TemplateApplicability`.

## Conversion and conformance corpus

A `ui profile` is a `UiProfileSyntax` in the syntax tree and in syntax JSON (`root`, `options`, `appearance` and `newScreenTemplate` are all modelled, so a profile round trips through print, parse and JSON without loss). It is deferred from the backend semantic model (`PLAY0269`): hosts that turn a document into a running application read the profile from the syntax tree. The mapping to Scene's `UiProfile` lives in Stage's `UiProfileConverter`, not here: `root template` becomes `root` (a template reference with its options and version pin), `appearance` becomes `appearance`, `new screens use template` becomes `defaults.newScreenTemplate`, and a legacy `layout` line converts to `root` when the layout resolves.

The canonical `screen-composition/v1` corpus pins the contract in `Cratis.Screenplay.CanonicalCorpus`:

| Vector | What it holds |
| --- | --- |
| `ui-profile-root-positive` | An own layout, an own application-scope screen template and a blueprint template with a version pin as the root template, shell options, appearance and the new screens default. |
| `ui-profile-root-legacy-layout` | The legacy `layout` line, alone and beside a `root template` (`PLAY0791`, a warning). |
| `ui-profile-root-unknown` | `PLAY0790`, `PLAY0792` and `PLAY0798`. |
| `ui-profile-root-not-a-shell` | `PLAY0794`, `PLAY0795` and `PLAY0799`. |
| `ui-profile-root-invalid` | `PLAY0796` and `PLAY0797`. |

The same shell rules are held to Scene's shared `effective-shell-fixtures.json` and `template-applicability-fixtures.json` vectors; the TypeScript compiler reports the legacy `layout` line (`PLAY0791`) with the same code and line as the C# compiler (`Conformance/diagnostics.json`) and offers the quick fix.
