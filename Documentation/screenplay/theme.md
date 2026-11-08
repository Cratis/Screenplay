---
title: "Cratis Screenplay theme"
description: "Declare a Cratis Screenplay theme as an implementation of a design system - palette, token values and per-mode values - and optionally which component packages it supports."
---

# Theme

A Cratis Screenplay `theme` is an implementation of the application's [design system](design-system.md). The design system says which tokens exist and how components use them; the theme gives each token a value. An application has one design system and can have several themes, one of them the design system's default. A [UI profile](ui-profile.md) selects the theme it is drawn in, or falls back to the default.

A theme can also list the component packages it supports with `compatible with`. A theme with only those lines, and no `implements`, is a *compatibility-only theme*. It keeps working as before: the C# compiler warns when a profile lists a package the theme does not declare. See [Compatibility is checked](#compatibility-is-checked).

A theme does not install styles. The consuming host and component packages supply the visual implementation; the compiler checks the relationships you declare, not the rendered result.

## Syntax

```screenplay
theme <Name>
  implements <DesignSystem>
  compatible with <Package>

  palette
    <name> = "<value>"

  values
    <category>.<name> = "<value>" | palette.<name> | <category>.<name>

  <mode>
    <category>.<name> = "<value>" | palette.<name> | <category>.<name>
```

Declare `theme <Name>` at the top level, alongside `ui profile`, using a unique identifier. Each line is optional; the order is free.

| Line or block | Meaning |
|---|---|
| `implements <DesignSystem>` | The design system this theme gives values to. Once per theme, and the design system must be declared (`PLAY0925`). |
| `compatible with <Package>` | A supported component package, at most once per package. |
| `palette` | Named raw values, referred to as `palette.<name>`. |
| `values` | Token values that are the same in every mode. |
| A mode block (`light`, `dark`, ...) | Token values for that mode. The mode must be one the design system declares (`PLAY0918`), once per theme. |

A value is a quoted literal, a `palette.<name>` reference, or the address of another token. References are followed until a literal is reached; a loop is `PLAY0923`, and a name that is not declared is `PLAY0922`.

## Example

```screenplay
design system ShopDesign
  modes light, dark
  default theme Daylight

  tokens
    color
      surface "page background"
      text    "body text"
    radius
      control "inputs and buttons"

theme Daylight
  implements ShopDesign
  compatible with core

  palette
    white = "#FFFFFF"
    ink   = "#111827"

  values
    radius.control = "8px"

  light
    color.surface = palette.white
    color.text    = palette.ink

  dark
    color.surface = palette.ink
    color.text    = palette.white

theme Square
  implements ShopDesign

  palette
    white = "#FFFFFF"
    ink   = "#111827"

  values
    radius.control = "0px"

  light
    color.surface = palette.white
    color.text    = palette.ink

  dark
    color.surface = palette.ink
    color.text    = palette.white
```

`Daylight` and `Square` draw the same components with different corners. Both give every `color` token a value in both modes.

## What a theme may and may not do

- It assigns values only. Components, their parts and states, words and rules belong to the design system, so a theme never redefines them.
- Every `color` token needs a value in every mode, from `values` or from that mode's block. A missing one is `PLAY0921`. Another category needs a value where a component uses the token; a used token with no value is `PLAY0922`.
- It assigns only tokens the design system declares (`PLAY0920`).
- Each name is assigned once in a block (`PLAY0919`).
- A malformed or repeated `implements`, `palette`, `values` or mode block is `PLAY0930`.
- The design system's `contrast` rules are evaluated against each theme and mode; a pair below a rule is warning `PLAY0928`.

See [Design systems and themes](diagnostics.md#design-systems-and-themes) for every code.

## Selecting a theme

A profile without a `theme` line is drawn in the design system's `default theme`. A `theme` line selects another one:

```screenplay
ui profile Desktop
  theme Aurora
```

The selected theme must implement the application's design system (`PLAY0926`). A compatibility-only theme is accepted in a profile when the application declares no design system.

```screenplay
ui profile Desktop
  packages
    core
    PrimeReact
    Internal.Widgets

  theme Aurora
```

## Compatibility is checked

For each package explicitly listed in a profile, the C# compiler checks the selected theme's `compatible with` declarations. A missing declaration produces warning `PLAY0232`:

```text
Theme 'Midnight' is not declared compatible with package 'PrimeReact' -
components from that package may not receive Midnight's styling
```

The warning identifies a package to check. It does not prove that the components are unstyled, and adding a compatibility line does not prove they render correctly. Inspect the pairing in the consuming UI host.

A theme name that the document does not declare produces warning `PLAY0231`, naming the unknown theme.

### Naming a package-provided theme

A component package may supply a theme with spaces in its display name, such as `Scene Default Light`. Top-level theme declarations accept identifiers only, but a profile can select a package-provided display name by quoting it:

```screenplay
ui profile Desktop
  theme "Scene Default Light"
```

The compiler resolves escapes in the quoted text and preserves the resulting name. The printer quotes names that are not identifiers; an identifier such as `Aurora` prints bare.

A name containing spaces cannot match a top-level declaration, so the compiler treats it as package-provided and does not report `PLAY0231`. The consumer must still find that theme in its packages. Quoting an identifier does not bypass validation: `theme "Aurora"` resolves to the document's `Aurora` declaration, or warns if none exists. Empty, blank or unterminated quoted names produce `PLAY0229`.

## Out of scope

A UI starter that bundles packages, a theme and a sample gallery is a tooling choice, not a `theme` construct in a `.play` file. After choosing the visual configuration, add [sample data](sample-data.md) to inspect the design with representative rows.
