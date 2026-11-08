---
title: "Cratis Screenplay design system"
description: "Declare an application's design contract in Cratis Screenplay - tokens, components, words and contrast rules - and let themes implement it with values."
---

# Design system

A Cratis Screenplay `design system` is the design contract of an application: which modes it is drawn in, which design tokens exist and what each is for, how components are made of those tokens, the words the product uses, and the rules a finished design must satisfy. It holds no concrete values. A [theme](theme.md) implements the design system by giving every token a value, and a [UI profile](ui-profile.md) picks the theme it is drawn in.

Use it to state a design once, check it at compile time, and give every renderer the same answer to "what does a primary button look like in dark mode?".

## Design system and theme

| | Design system | Theme |
|---|---|---|
| Role | The contract: what exists and what it is for | An implementation: the values |
| How many | Exactly one per application | Several, one is the default |
| Holds | Modes, tokens, components, words, rules | Palette, token values, per-mode values |
| Components | Declares them | Never redefines them |

A component is described once, in terms of tokens. Each theme then assigns values to the tokens, so every theme draws the same components with its own look.

## Syntax

```screenplay
design system <Name>
  modes <mode>, <mode>
  default theme <ThemeName>

  tokens
    <category>
      <name> "<what it is for>"

  components
    <Component>
      parts <part>, <part>
      variant <name>
        <part> = <category>.<name>
        state <state>
          <part> = <category>.<name>

  words
    voice
      <key> = "<text>"
    naming
      <key> = "<text>"

  rules
    contrast <part> on <part> at least <ratio>
```

Declare `design system <Name>` at the top level, once. The sections may come in any order, each at most once. `default theme` is required, because a profile without a theme line resolves to it ([`PLAY0927`](diagnostics.md#design-systems-and-themes)).

## Example

```screenplay
design system ShopDesign
  modes light, dark
  default theme Daylight

  tokens
    color
      surface    "page background"
      text       "body text"
      primary    "primary action"
      on-primary "text on a primary action"
    space
      md         "default gap between elements"
    radius
      control    "inputs and buttons"
    font
      sans       "interface text"

  components
    Button
      parts text, background, radius
      variant primary
        text       = color.on-primary
        background = color.primary
        radius     = radius.control
        state disabled
          text       = color.text
          background = color.surface

  words
    voice
      tone = "Plain and exact"
    naming
      customer = "Client"

  rules
    contrast text on background at least 4.5

theme Daylight
  implements ShopDesign

  palette
    indigo = "#4338CA"
    white  = "#FFFFFF"
    ink    = "#111827"

  values
    space.md       = "12px"
    radius.control = "8px"
    font.sans      = "inter"

  light
    color.surface    = palette.white
    color.text       = palette.ink
    color.primary    = palette.indigo
    color.on-primary = palette.white

  dark
    color.surface    = palette.ink
    color.text       = palette.white
    color.primary    = palette.indigo
    color.on-primary = palette.white
```

## Modes

`modes light, dark` lists the modes the design is drawn in, such as light and dark. Modes belong to the design system, not to a theme: every theme implements the same modes. A `ui profile`'s `color scheme` chooses among them; see [Which theme and mode a profile gets](#which-theme-and-mode-a-profile-gets). A mode written twice is an error ([`PLAY0919`](diagnostics.md#design-systems-and-themes)).

## Tokens

Tokens are grouped by category. The list of categories is closed:

| Category | Holds |
|---|---|
| `color` | Colors, which usually differ per mode |
| `space` | Gaps and padding |
| `size` | Heights, widths and other dimensions |
| `radius` | Corner rounding |
| `border` | Border widths and styles |
| `shadow` | Elevation shadows |
| `font` | Font families |
| `type` | Type styles |
| `motion` | Durations and easing |
| `opacity` | Transparency levels |
| `z` | Stacking order |

Write each token as a name with an optional quoted description of what it is for. The description is the contract: a theme author reads it to choose a value. A token is addressed as `<category>.<name>`, such as `color.primary` or `space.md`. Names are unique within a category. An unknown category is an error ([`PLAY0916`](diagnostics.md#design-systems-and-themes)).

A `type` token is a single value in this version. A type style that combines family, size and weight is not a token yet; express those parts with `font` and `size` tokens.

## Components

A component names the parts it is made of and the variants it comes in. The list of parts is closed, and each part accepts only some token categories:

| Part | Takes a token of category |
|---|---|
| `text` | `color` |
| `background` | `color` |
| `border` | `color` or `border` |
| `radius` | `radius` |
| `height` | `size` or `space` |
| `padding` | `space` or `size` |
| `shadow` | `shadow` |
| `font` | `font` or `type` |

`parts` lists the parts the component uses. Each `variant` assigns one token to each part with `<part> = <category>.<name>`. A variant can only assign a part the component declares ([`PLAY0916`](diagnostics.md#design-systems-and-themes)), only to a token the design system declares ([`PLAY0922`](diagnostics.md#design-systems-and-themes)), and only to a token of a category the part takes: `background = space.md` is [`PLAY0924`](diagnostics.md#design-systems-and-themes).

### States

Inside a variant, a `state` overrides some of its parts. The states are `default`, `hover`, `focus`, `active`, `disabled` and `selected`. The variant's own parts are the `default` state; a state names only what changes and every other part keeps the variant's token.

## Words

`words` records how the product speaks. `voice` holds the tone of its text; `naming` holds the terms the product uses instead of the model's own names, such as showing "Client" where the model says `customer`. Each is a set of `<key> = "<text>"` lines, and each section appears once. Words travel with the design system, so every theme speaks the same way.

## Rules

A rule states a property every theme must satisfy. One form exists in this version:

```text
contrast <part> on <part> at least <ratio>
```

`contrast text on background at least 4.5` says that, wherever a component has both parts, the text must reach a 4.5 WCAG contrast ratio against the background.

The compiler evaluates each rule for every theme, in every mode, for every variant and every state except `disabled` (a disabled control is exempt from contrast requirements). A pair that falls short produces warning [`PLAY0928`](diagnostics.md#design-systems-and-themes) naming the theme, mode, component, colors and measured ratio.

The ratio is measured only on colors written as hex (`#rgb`, `#rrggbb`, `#rrggbbaa`) or `rgb(...)` literals, after the compiler follows palette and token references. A color the compiler cannot measure, such as a named color, is not checked.

## How a theme implements it

A theme that says `implements <DesignSystem>` assigns values and nothing else:

```text
theme <Name>
  implements <DesignSystem>
  palette
    <name> = "<value>"
  values
    <category>.<name> = "<value>" | palette.<name> | <category>.<name>
  <mode>
    <category>.<name> = "<value>" | palette.<name> | <category>.<name>
```

- `palette` names raw values so the rest of the theme can refer to them as `palette.<name>`.
- `values` holds token values that are the same in every mode.
- A block named for a mode (`light`, `dark`) holds the values that differ per mode.
- **Every `color` token needs a value in every mode**, either in `values` or in that mode's block. A missing one is [`PLAY0921`](diagnostics.md#design-systems-and-themes).
- A theme assigns only tokens the design system declares ([`PLAY0920`](diagnostics.md#design-systems-and-themes)) and has blocks only for declared modes ([`PLAY0918`](diagnostics.md#design-systems-and-themes)).
- A theme never redefines components. Their parts come from the design system, so one change to a component reaches every theme.

[Theme](theme.md) describes the theme declaration in full, including the `compatible with` lines.

## Which theme and mode a profile gets

A `ui profile` with no `theme` line is drawn in the design system's `default theme`. A `theme <Name>` line overrides it, and the named theme must implement the design system ([`PLAY0926`](diagnostics.md#design-systems-and-themes)).

The mode comes from the profile's `appearance`: `color scheme dark` selects the `dark` mode. When the profile gives no color scheme, or the scheme is not one of the design system's modes (such as `system`), the first mode is used.

`appearance` keeps only `color scheme`. Its `accent`, `corners` and `font` members are deprecated ([`PLAY0929`](diagnostics.md#design-systems-and-themes)); put those values in the theme. [UI profile](ui-profile.md#appearance-and-theme) shows the move.

## Resolving a theme in a host

A host that draws the application, such as Stage, Scene or Direct, reads the resolved design instead of walking the syntax. `Cratis.Screenplay.Design.DesignSystemResolver` offers three functions:

| Function | Returns |
|---|---|
| `SelectTheme(application, profile)` | The theme a profile selects: its `theme` line, else the default theme |
| `SelectMode(application, profile)` | The mode its `color scheme` selects, else the first mode |
| `Resolve(application, theme, mode)` | A `ResolvedDesignTheme`; `Resolve(application, profile)` does both selections first |

`ResolvedDesignTheme` carries the design system and theme names, the mode, a flat map from `<category>.<name>` to the resolved value (palette and token references already followed), the components with each variant's parts and each state's parts merged over the variant's, and `Problems` for anything that could not be resolved. `Resolve` returns `null` when the application has no design system, the theme does not implement it, or the mode is not declared.

## Compiler and tools

- The compiler checks the whole construct: the diagnostics are listed under [Design systems and themes](diagnostics.md#design-systems-and-themes), `PLAY0916` to `PLAY0930`.
- The [executable semantic model](diagnostics.md) defers the design system as UI configuration. Binding reports it as deferred (`PLAY0269`) rather than failing; a host reads the design from the syntax tree or the resolver.
- The MCP `ui-profiles` read tool returns the design system, every theme and the resolved values per mode, plus the theme and mode each profile resolves to. See [MCP reference](mcp/reference.md) and [authoring tools](mcp/authoring-tools.md#the-profiles-shell).

## Out of scope

This version has one design system per application, no theme inheritance, no per-module design systems, and no generation of CSS or component-package mappings. Those belong to the tools that render a design.
