# Layout arrangement

A [layout or template](templates.md) says a structure has a `sidebar` and a `main` — but not how those two slots share the available space, or what should happen when the available space changes shape. `arrangement` closes that gap: it says whether the slots reflow responsively (`flow`) or sit at pixel-precise coordinates (`freeform`), and it gives each mode its own way of varying by size.

`arrangement` works identically on all three slot-bearing declarations — the application's `layout`, a `screen template` and a `dialog template`.

## Size classes: width x height, not "orientation"

Both arrangement modes vary by the same two-dimensional vocabulary: `width` and `height`, each either `compact` or `regular`. This is deliberately a matrix rather than an `orientation` keyword - phone portrait, phone landscape, tablet, and desktop narrow/wide all fall out of the width/height combination for free, without inventing a separate primitive for orientation.

| | `height compact` | `height regular` |
|---|---|---|
| **`width compact`** | phone landscape | phone portrait |
| **`width regular`** | tablet/desktop, short | tablet/desktop, tall |

## Declaring slots comes first

A slot is declared once, by name, in the body — the arrangement then positions it:

```screenplay
module Invoicing
  screen template MasterDetail
    sidebar
    main
```

That is a complete declaration. `arrangement` is optional, and a structure that only names its slots leaves the placement to whatever renders it.

Because a slot is declared once, the arrangement never repeats what a slot *is*: `contributes` belongs on the declaration, and sizing belongs in the arrangement. A slot named only by an arrangement is still declared by it, so an arrangement on its own remains complete too.

## `flow` — one tree, reflow via overrides

`arrangement flow` holds the tree directly. Nest slots under `row`, `column` or `grid`:

```screenplay
module Invoicing
  screen template MasterDetail
    sidebar
    main

    arrangement flow
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
- `when width <class>[, height <class>]` (or `when height <class>` alone) replaces the whole tree for that size-class condition. `MasterDetail` above renders `sidebar` beside `main` at regular width, and stacks `main` above `sidebar` at compact width.

These primitives are deliberately neutral, not CSS-flavored - they map to CSS flex/grid on web and to native stack/grid equivalents elsewhere, the same platform-agnostic stance the rest of the language takes.

## `freeform` — one variant per size-class combination

`arrangement freeform` places slots at pixel-precise coordinates instead of reflowing them - the right fit for dashboards and canvases where positions matter more than responsive flow:

```screenplay
module Dashboards
  screen template DashboardCanvas
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

- `variant width <class>, height <class>` declares one point in the size-class matrix. An arrangement can declare up to four - one per combination.
- `place <Slot> at <x>,<y> size <w>,<h>` positions a slot; either size dimension can be `fill` instead of a pixel count.
- `place <Slot> hidden` removes a slot from that variant entirely rather than positioning it.

Critically: **one `screen`, one data/action/form contract, N `variant`s purely for placement.** Freeform needing "a layout per size" only touches where things sit - the binding contract a screen declares against its template never changes per variant. Duplicating the contract per size would repeat the exact mistake [forms](forms.md) were designed to avoid for command properties.

A `variant` that omits a slot another variant of the same arrangement places (or explicitly hides) gets a compile-time warning - the slot's presence for that size class is otherwise undefined:

```
The screen template 'DashboardCanvas' has a variant for width compact, height regular
that does not mention slot 'x' - place it or declare it 'hidden'
```

## `flow` vs. `freeform` — pick one per arrangement

An arrangement is either `flow` or `freeform` - the whole arrangement, not per slot. Mixing responsive flow for most of a structure with one pixel-precise region (a chart embedded in an otherwise-responsive dashboard) is a real, common need that this version does not yet support; it needs slot-level arrangement, which is a larger grammar change tracked separately. Until then, split a mixed structure into two: a `flow` structure for the responsive shell, with one of its slots filled by a screen whose own template is `freeform`.

Declaring a `variant` under `arrangement flow`, or a `row`/`column`/`grid` under `arrangement freeform`, is a compile-time error - the body must match the mode on the line that opened it.

## See also

- [Layouts and templates](templates.md) - the constructs `arrangement` belongs to, and how they nest.
- [Screens](screens.md) - where a template is filled with content.
- [Forms](forms.md) - the same "one contract, many presentations" principle applied to command properties.
- [UI profile](ui-profile.md) - where a build's default size class and target platforms are declared.
