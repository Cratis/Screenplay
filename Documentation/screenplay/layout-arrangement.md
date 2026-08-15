# Layout arrangement

A [layout template](screens.md#layout-templates) says a screen has a `sidebar` and a `main` - but not how those two slots share the available space, or what should happen when the available space changes shape. `arrangement` closes that gap: it says whether a layout reflows responsively (`flow`) or is placed at pixel-precise coordinates (`freeform`), and it gives each mode its own way of varying by size.

## Size classes: width x height, not "orientation"

Both arrangement modes vary by the same two-dimensional vocabulary: `width` and `height`, each either `compact` or `regular`. This is deliberately a matrix rather than an `orientation` keyword - phone portrait, phone landscape, tablet, and desktop narrow/wide all fall out of the width/height combination for free, without inventing a separate primitive for orientation.

| | `height compact` | `height regular` |
|---|---|---|
| **`width compact`** | phone landscape | phone portrait |
| **`width regular`** | tablet/desktop, short | tablet/desktop, tall |

## `flow` - one template, reflow via overrides

`flow` is the default - a bare `layout` with no `arrangement` line, and a `template` of plain slot names, is exactly the `flow` grammar that predates arrangement entirely:

```screenplay
layout MasterDetail
  template
    sidebar
    main
```

To actually arrange those slots, nest them under `row`, `column` or `grid`:

```screenplay
layout MasterDetail
  arrangement flow

  template
    row gap 16
      sidebar width 280
      main grow

    when width compact
      column
        main
        sidebar
```

- `row` / `column` / `grid` are containers - children are arranged horizontally, vertically, or in a two-dimensional grid. A container may declare `gap <number>`, the spacing between its children.
- A slot leaf can carry sizing: `width <n>` / `height <n>` (a fixed size), `grow` (fills the remaining space), or `span <n>` (grid tracks).
- `when width <class>[, height <class>]` (or `when height <class>` alone) replaces the whole template tree for that size-class condition. `MasterDetail` above renders `sidebar` beside `main` at regular width, and stacks `main` above `sidebar` at compact width.

These primitives are deliberately neutral, not CSS-flavored - they map to CSS flex/grid on web and to native stack/grid equivalents elsewhere, the same platform-agnostic stance the rest of the language takes.

## `freeform` - one variant per size-class combination

`freeform` places slots at pixel-precise coordinates instead of reflowing them - the right fit for dashboards and canvases where positions matter more than responsive flow:

```screenplay
layout DashboardCanvas
  arrangement freeform

  variant width regular, height regular
    place header  at 0,0    size fill,64
    place sidebar at 0,64   size 240,fill
    place main    at 240,64 size fill,fill

  variant width compact, height regular
    place header at 0,0  size fill,48
    place main   at 0,48 size fill,fill
    place sidebar hidden
```

- `variant width <class>, height <class>` declares one point in the size-class matrix. A layout can declare up to four - one per combination.
- `place <Slot> at <x>,<y> size <w>,<h>` positions a slot; either size dimension can be `fill` instead of a pixel count.
- `place <Slot> hidden` removes a slot from that variant entirely rather than positioning it.

Critically: **one `screen`, one data/action/form contract, N `variant`s purely for placement.** Freeform needing "a layout per size" only touches where things sit - the binding contract a screen declares against its layout never changes per variant. Duplicating the contract per size would repeat the exact mistake [forms](forms.md) were designed to avoid for command properties.

A `variant` that omits a slot another variant of the same layout places (or explicitly hides) gets a compile-time warning - the slot's presence for that size class is otherwise undefined:

```
Layout 'DashboardCanvas' variant for width compact, height regular does not
mention slot 'x' - place it or declare it 'hidden'
```

## `flow` vs. `freeform` - pick one per layout

A layout is either `flow` or `freeform` - the whole layout, not per slot. Mixing responsive flow for most of a layout with one pixel-precise region (a chart embedded in an otherwise-responsive dashboard) is a real, common need that this version does not yet support; it needs slot-level arrangement, which is a larger grammar change tracked separately. Until then, split a mixed layout into two: a `flow` layout for the responsive shell, with one of its slots occupied by a screen whose own layout is `freeform`.

Declaring `template` on a `freeform` layout, or `variant` on a `flow` layout, is a compile-time error - a layout's body must match its arrangement.

## See also

- [Screens](screens.md) - where a `layout` is referenced and its slots are filled.
- [Forms](forms.md) - the same "one contract, many presentations" principle applied to command properties.
- [UI profile](ui-profile.md) - where a build's default size class and target platforms are declared.
