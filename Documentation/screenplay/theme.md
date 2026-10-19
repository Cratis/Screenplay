# Theme

A [ui profile](ui-profile.md)'s `packages` list says which component packages a build draws widgets from - but a theme is only meaningful relative to a specific set of those packages. Pairing an arbitrary theme with an arbitrary package can silently produce unstyled or broken components. `theme` makes that relationship a declared, checked one instead of an assumed one.

## Syntax

```screenplay
theme <Name>
  compatible with <Package>
  ...
```

- `theme <Name>` - top level, alongside `ui profile`. A document can declare more than one; each name must be unique.
- `compatible with <Package>` - one of the component packages this theme is meaningful against. A theme lists as many as it actually supports; each package can be listed at most once per theme.

A `ui profile` selects a theme with its own `theme <Name>` line:

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

`Midnight` declares compatibility with only `core` - a reasonable minimal choice for a theme that only styles the built-in vocabulary. Since `core` is always present in every profile's package list, `Midnight` is valid to select from any profile; it just does not claim to style anything a vendor package like `PrimeReact` adds.

## Compatibility is checked

A `ui profile` selecting a theme not declared compatible with one of the profile's own packages gets a compile-time warning, the same class of diagnostic an ambiguous or unknown name already gets:

```text
Theme 'Midnight' is not declared compatible with package 'PrimeReact' -
components from that package may not receive Midnight's styling
```

This is a warning, not an error - the pairing might still work by coincidence, since Screenplay's compiler has no visibility into what a package's widgets actually look like. What the warning buys is visibility: a mismatched theme/package pairing is a compile-time signal instead of a runtime surprise a developer has to notice by eye.

A `ui profile` that selects a theme nothing in the document declares gets the same treatment as any other unresolved name - a warning naming the unknown theme.

### Naming a package-provided theme

The themes a component package ships have display names with spaces, such as `Scene Default Light`. A `theme <Name>` declaration needs an identifier, so a document cannot declare one. A `ui profile` can still select it by its real name, written as a quoted string:

```screenplay
ui profile Desktop
  theme "Scene Default Light"
```

The quoted text, with its escapes resolved, is the theme name wherever the profile carries it, and the printer writes it back quoted. An identifier name such as `theme Aurora` is unchanged and prints bare. A name that is not an identifier cannot be declared in a document, so it refers to a theme a package provides. The compiler cannot see package themes, so such a name is not checked against the document's declarations and gets no `PLAY0231`. A quoted name that does match a theme the document declares resolves to it, and the compatibility check applies as for an identifier. An empty, blank or unterminated quoted name is `PLAY0229`. Standalone `theme <Name>` declarations keep identifier-only names.

## Out of scope

A packaged "pick-one-to-start-from" UI kit - a bundle of packages, a theme, and a sample gallery - is a different, tooling-level concept from `theme`. It is not a `.play` file construct at all; see the Scene UI starters work instead.
