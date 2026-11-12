---
title: "Cratis Screenplay UI profile configuration"
description: "A Cratis Screenplay UI profile selects target platforms, an application shell, component packages and appearance without tying screens to one renderer."
---

# UI profile

A Cratis Screenplay `ui profile` declares where an application's screens are intended to appear and which shell, component packages and visual settings a consumer should use. Keep these choices in the profile so a screen can describe its task without naming a target platform.

A tool such as Stage reads these settings from the model; the profile itself does not supply a renderer or make a component library available on every platform. Before choosing packages, check that the consuming host supports the target and can load those packages. The platform names in this page's examples are declarations, not a list of implemented targets.

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

  identity
    name "<display name>"
    logo initials "<AB>" | icon <icon> | image "<data:image/...;base64,...>"
    show name true|false

  appearance
    color scheme light|dark|system

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

Declare `ui profile <Name>` at the top level. A document can contain several profiles, each with a unique name.

### Targets and packages

| Setting | Meaning and limits |
| --- | --- |
| `target platform` | The intended platforms, such as `web`, `ios` or `android`, on one comma-separated line. At most one line per profile. |
| `target size` | The default size class: `compact`, `regular` or `expanded`. These are named classes, not pixel breakpoints. At most one per profile. A layout's width-by-height arrangement matrix is a separate, more specific choice. |
| `packages` | Component packages, one per line, in override-priority order. Later component libraries can shadow earlier ones; see [Component name resolution](#component-name-resolution). `core` supplies the built-in vocabulary as a fallback. |
| `icons` | Icon sets or packages for `icon <name>` references on components, toolbar items and actions. The compiler preserves the list; Stage checks icon resolution against the selected packages. |
| `theme` | An optional theme name, once per profile. Without it the profile is drawn in the [design system's](design-system.md) default theme; with it, the named theme overrides the default. Use an identifier or a quoted display name such as `theme "Scene Default Light"`. See [Theme](theme.md) for declaration checks and package-provided names. |
| `sample data` | An optional application-wide pack selection, shape mappings and per-read-model bindings. These apply only during design and UI previews, never in production builds. See [Sample data](sample-data.md). |

A package that supplies reusable templates is a blueprint. Reference one with `template blueprint <Blueprint> <Name>`; a blueprint that no profile lists produces `PLAY0722`. See [Layouts and templates](templates.md#own-and-blueprint-templates).

### Application shell and new screens

`root template` selects the shell surrounding every screen's template chain. It can name a layout or an application-scope screen template from the document, or a blueprint template using `blueprint <Blueprint> <Name>`. An optional `version "<range>"` pin applies only to blueprint templates; using it on an own template produces `PLAY0788`.

A profile can have one root template (`PLAY0798` for a duplicate). The template must exist (`PLAY0790`), be a layout or an application-scope screen template that fits no slot (`PLAY0794`), and provide a `content` slot (`PLAY0795`). A blueprint supplying the root must appear in the profile's `packages` (`PLAY0792`).

Under the root, `option <name> <value>` configures an item the shell exposes. Values can be bare words, quoted strings, numbers, `true` or `false`, and each option name is set once. For an own shell, the validator checks options against the properties in `exposure for <Shell>`; a shell without an exposure has no options. An unknown option produces `PLAY0796`. Blueprint options require the installed catalog described below.

`new screens use template` sets a designer's starting template without changing existing screens. It is optional and can appear once (`PLAY0788` for a duplicate). The template must be able to start a screen in a feature. Layouts, application-scope roots and templates whose `scopes` exclude `feature` are refused with `PLAY0799`.

The older `layout <Name>` selection remains accepted, once per profile. It produces warning `PLAY0791` and a quick fix to `root template <Name>`. If both are present, `root template` takes precedence. An undeclared layout produces a warning. See [Layouts and templates](templates.md).

### Identity

Use `identity` for the application's displayed name and logo. The block is optional; each setting can appear once. Duplicate blocks or settings produce `PLAY0789`.

| Identity setting | Accepted values and defaults |
| --- | --- |
| `name "<text>"` | Display name; defaults to the application's name. |
| `logo initials "<AB>"` | One or two characters. |
| `logo icon <icon>` | `box`, `layers`, `home`, `users`, `receipt`, `package`, `database` or `palette`. |
| `logo image "<data URI>"` | PNG, JPG, SVG or WebP, as a base64 data URI of at most 400 KB. |
| `show name true\|false` | Whether to show the name beside the logo; shown by default. |

### Appearance and theme

Use `appearance` for the shell-independent choice of color scheme. It is optional, with one block and one line (`PLAY0789` for duplicates).

| Appearance setting | Accepted values |
| --- | --- |
| `color scheme` | `light`, `dark` or `system`. With a [design system](design-system.md), it selects the mode of that name; when the scheme is not one of the design system's modes, the first mode is used. |

Identity and appearance values outside Scene's rules produce `PLAY0797`.

How the application looks beyond the color scheme belongs in the theme. A profile with no `theme` line uses the design system's default theme. A `theme <Name>` line selects another theme, which must implement the design system (`PLAY0926`). See [Which theme and mode a profile gets](design-system.md#which-theme-and-mode-a-profile-gets).

`accent`, `corners` and `font` in `appearance` are deprecated. They still read, but each produces warning `PLAY0929`. Move the value into the theme the profile uses: the accent becomes a `color` token value, the corners a `radius` token value and the font a `font` token value.

Before:

```text
ui profile Desktop
  appearance
    color scheme dark
    accent "#4f46e5"
    corners round
    font inter
```

After:

```screenplay
design system ShopDesign
  modes light, dark
  default theme Nordic

  tokens
    color
      primary "primary action"
    radius
      control "inputs and buttons"
    font
      sans "interface text"

theme Nordic
  implements ShopDesign

  values
    radius.control = "12px"
    font.sans      = "inter"

  light
    color.primary = "#4F46E5"

  dark
    color.primary = "#4F46E5"

ui profile Desktop
  appearance
    color scheme dark
```

## Example

The desktop profile below selects an application shell, dark appearance and a package chain. The mobile profile declares a different target and size without changing any screen.

This is a configuration fragment: the shell option assumes an `exposure for AppShell` declaring `navigationMode`, which is not shown. Without that exposure, the option produces `PLAY0796`.

```screenplay
layout AppShell
  topbar
  navigation panel
  content page routed primary
  outlet Navigation of navigation in navigation

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

  identity
    name "Contoso CRM"
    logo initials "CC"

  appearance
    color scheme dark

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

The consuming build or host selects the profile; a `screen` does not select one for itself. This keeps screen declarations reusable across package chains. Whether a screen can render on a particular target still depends on the host's renderer and the components that target supports.

## Component name resolution

Scene's package resolver searches a bare component name from highest to lowest package priority. The last matching package wins, and the result records the packages it shadows. A name available in only one active package needs no qualification, even if that package appears late in the list.

Use a qualified name such as `Internal.Widgets.TrendChart` when you want that package's component explicitly. Qualification bypasses the priority search. In the host package chain, `core` is the fallback, component libraries follow in their declared order, and blueprints have priority over those libraries.

These rules use the consumer's component catalog. Screenplay's compiler cannot inspect the widgets that PrimeReact or an internal library implements. It checks the declaration, including duplicate profile names and duplicate packages within a profile; package resolution belongs to the consuming host and Scene engine.

## Extension components

A [component an extension provides](screens.md#extension-components) is placed as `component <System>.<Component>`, so a profile does not list the extension as a package: the system names it. The component's contract says which platforms it supports. A `target platform` of the profile that the component does not list is reported as `PLAY1118`, a warning, at each placement, so a screen that must run on `ios` does not quietly place a web-only component. A component whose contract declares no `platform` line is not checked.

## Checking a shell against the installed blueprints

For an own shell, the compiler can inspect the document's template and exposure declarations. A blueprint shell needs an additional check against what is installed: its template kinds, slots, options and version.

A host with that catalog passes it to `UiProfileShellBinder.Bind(application, blueprints)`. The binder applies the shell rules used by the compiler, corresponding to Scene's `ShellResolver` and `TemplateApplicability`:

| Diagnostic | Reported when |
| --- | --- |
| `PLAY0790` | The blueprint does not ship the named template. |
| `PLAY0792` | The blueprint is not installed. |
| `PLAY0793` | The installed version does not satisfy the profile's `version` pin. |
| `PLAY0794` | The template cannot serve as the application shell. |
| `PLAY0795` | The shell has no `content` slot. |
| `PLAY0796` | The shell does not expose the named option. |
| `PLAY0799` | The new-screen default cannot start a screen in a feature. |

A profile that parses successfully has not necessarily passed these installed-package checks.

## Conversion and conformance corpus

The C# compiler represents a profile as `UiProfileSyntax`, preserving `root`, `options`, `identity`, `appearance` and `newScreenTemplate` through printing, parsing and syntax JSON. The backend executable semantic model defers UI profiles (`PLAY0269`); a consumer that needs UI configuration reads the syntax tree. See [Visitors and traversal](visitors.md) for working with that tree.

Stage's `UiProfileConverter` maps the syntax to Scene's `UiProfile`: `root template` becomes `root`, including options and the version pin; `appearance` becomes `appearance`; and `new screens use template` becomes `defaults.newScreenTemplate`. A legacy `layout` line becomes the root when the layout resolves. This conversion carries the configuration; it does not establish renderer support for every declared platform.

The canonical `screen-composition/v1` corpus pins the contract in `Cratis.Screenplay.CanonicalCorpus`:

| Vector | What it holds |
| --- | --- |
| `ui-profile-root-positive` | An own layout, an own application-scope screen template and a blueprint template with a version pin as the root template, shell options, appearance and the new screens default. |
| `ui-profile-root-legacy-layout` | The legacy `layout` line, alone and beside a `root template` (`PLAY0791`, a warning). |
| `ui-profile-root-unknown` | `PLAY0790`, `PLAY0792` and `PLAY0798`. |
| `ui-profile-root-not-a-shell` | `PLAY0794`, `PLAY0795` and `PLAY0799`. |
| `ui-profile-root-invalid` | `PLAY0796` and `PLAY0797`. |

The shell rules also have vectors in Scene's shared `effective-shell-fixtures.json` and `template-applicability-fixtures.json`. The TypeScript compiler otherwise skips UI profiles, but it reports legacy `layout` selection with `PLAY0791` and offers the quick fix. `Conformance/diagnostics.json` covers that diagnostic's code and line; it does not establish full profile-validation parity.

After selecting packages and a shell, declare [theme compatibility](theme.md) and add [sample data](sample-data.md) to inspect the design with representative rows.
