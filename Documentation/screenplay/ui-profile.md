# UI profile

Screenplay's screen and layout constructs are platform-agnostic on purpose - the same `screen` should work on the web, on a phone, and on desktop. Something still has to say which platforms an application targets and which component packages (PrimeReact, an internal widget library, ...) a build resolves widget names against. That is `ui profile`.

## Syntax

```screenplay
ui profile <Name>
  target platform <Platform>[, <Platform>...]
  target size <SizeClass>

  packages
    <Package>
    ...
```

- `ui profile <Name>` - top level, alongside authentication, modules and everything else. A document can declare more than one; each name must be unique.
- `target platform` - the platform(s) this profile targets, e.g. `web`, `ios`, `android`. At most one per profile.
- `target size` - the size class assumed by default: `compact`, `regular` or `expanded` (Apple/Material-style named classes, not raw pixel breakpoints - a narrow browser window and a compact phone are "the same" class, and a raw breakpoint means nothing on native). At most one per profile. The two-axis width x height matrix a `layout` resolves against is a separate, more specific concern.
- `packages` - the component packages this profile draws from, one per line, in **override-priority order**: a later package's `Button` shadows an earlier one's on a name collision. `core`, the built-in vocabulary (`button`, `table`, `form`, `title`, ...), is always the final fallback regardless of what a profile lists here.
- `theme` - the visual theme this profile applies. Optional, at most one per profile. A theme is only meaningful relative to a specific set of packages - see [Theme](theme.md) for how that compatibility is declared and checked.

## Example

```screenplay
ui profile Desktop
  target platform web
  target size expanded

  packages
    core
    PrimeReact
    Internal.Widgets

ui profile Mobile
  target platform ios, android
  target size compact
```

## A screen never declares its profile

Profile selection is a build/Stage concern, not something a `screen` states about itself. The same screen resolves against different package chains per build - that is what keeps a Level 1/2 screen genuinely portable across web, mobile and desktop rather than tied to one target.

## Component name resolution

A profile's `packages` list is resolved the same way as any other bare name in Screenplay (see [Visitors and traversal](visitors.md) for the general inside-out walk this reuses): a bare widget name checks the active packages in declaration order, and a name matching two packages equally well is reported the same way an ambiguous query reference is - named candidates, not a silent pick. A widget already in `core` (a button, say) always resolves without qualification; one that only exists in a package further down the list, like an internal chart widget, has to be qualified by that package's name to be found at all.

Screenplay's own compiler has no visibility into what widgets a package like PrimeReact or an internal library actually defines - that catalog is external to the document. Resolving a bare widget name against it, and reporting the resulting ambiguity, is therefore a build-time concern owned by Stage and the package resolution engine, not something the Screenplay compiler itself checks. What the compiler does validate at compile time is the profile declaration itself: a profile name, or a package within one profile's list, is never allowed to be declared twice.
