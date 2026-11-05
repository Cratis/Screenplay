---
title: "Cratis Screenplay theme compatibility"
description: "Declare which component packages a Cratis Screenplay theme supports, select it in a UI profile, and understand what the compiler checks."
---

# Theme

A Cratis Screenplay `theme` declares which component packages a visual theme supports. Select it in a [UI profile](ui-profile.md) to have the C# compiler warn when a listed package has no declared compatibility. This helps you catch a configuration gap before inspecting the rendered screen.

The declaration does not contain styles or install a theme. The consuming host and component packages supply the visual implementation; the compiler checks the relationship you declare, not the rendered result.

## Syntax

```screenplay
theme <Name>
  compatible with <Package>
  ...
```

Declare `theme <Name>` at the top level, alongside `ui profile`, using a unique identifier. List each supported component package on its own `compatible with <Package>` line, at most once per theme.

Then select the theme in the profile that uses those packages:

```screenplay
ui profile Desktop
  packages
    core
    PrimeReact
    Internal.Widgets

  theme Aurora
```

## Example

```screenplay
theme Aurora
  compatible with core
  compatible with PrimeReact
  compatible with Internal.Widgets

theme Midnight
  compatible with core
```

`Aurora` covers all three packages listed by the desktop profile above. `Midnight` covers only `core`, the built-in component vocabulary. Selecting `Midnight` in that desktop profile therefore produces warnings for `PrimeReact` and `Internal.Widgets`. Either use a theme that supports those libraries or supply the missing styling before declaring compatibility.

## Compatibility is checked

For each package explicitly listed in a profile, the C# compiler checks the selected theme's `compatible with` declarations. A missing declaration produces warning `PLAY0232`:

```text
Theme 'Midnight' is not declared compatible with package 'PrimeReact' -
components from that package may not receive Midnight's styling
```

The warning identifies a package to check. It does not prove that the components are unstyled, and adding a compatibility line does not prove they render correctly. Inspect the pairing in the consuming UI host.

An identifier theme name that the document does not declare produces warning `PLAY0231`, naming the unknown theme.

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
