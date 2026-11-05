---
title: "Screenplay layout arrangement: position template slots"
description: "Use flow to reflow Screenplay slots as space changes, or freeform to position them at coordinates. Width and height classes select the arrangement."
---

# Layout arrangement

A Screenplay `arrangement` positions a layout or template's slots. Choose `flow` when content should reflow as space changes, or `freeform` when a dashboard or canvas needs explicit coordinates. Both let you change placement by available width and height without duplicating the screen's data and actions.

Declare it on the application's `layout`, a `screen template` or a `dialog template`. If you have not named the regions to place, start with [layouts and templates](templates.md).

## Size classes: width x height, not "orientation"

Both modes use `width` and `height`, each either `compact` or `regular`. Choose placement based on those available dimensions rather than a device name or orientation. A narrow desktop window can need the same arrangement as another compact-width surface.

| | `height compact` | `height regular` |
|---|---|---|
| **`width compact`** | Narrow, short space | Narrow, tall space |
| **`width regular`** | Wide, short space | Wide, tall space |

## Declaring slots comes first

Name the regions in the layout or template body before deciding where to place them:

```screenplay
module Invoicing
  screen template MasterDetail
    sidebar
    main
```

This declares `sidebar` and `main` without fixing their placement. `arrangement` is optional; omit it when the renderer should decide where the slots go.

Keep contribution metadata on the slot declaration and sizing in the arrangement. An arrangement can also introduce a slot that has no separate declaration, so you do not have to repeat its name just to make it valid.

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

The declaration expresses rows, columns and grids without embedding CSS or a native UI API. The rendering target is responsible for realizing those placement rules.

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

The variants change placement only. In the compact variant above, `main` fills the width and `sidebar` is hidden; the screen still declares its data, actions and forms once. You do not need a separate screen contract for each size.

A `variant` that omits a slot another variant of the same arrangement places (or explicitly hides) gets a compile-time warning - the slot's presence for that size class is otherwise undefined:

```text
The screen template 'DashboardCanvas' has a variant for width compact, height regular
that does not mention slot 'x' - place it or declare it 'hidden'
```

## `flow` vs. `freeform` — pick one per arrangement

An arrangement uses one mode for the whole structure, not a different mode per slot. If a responsive shell needs a coordinate-positioned region, split it into two structures: use `flow` for the shell and fill one slot with a screen whose template uses `freeform`. Mixing the two modes inside one arrangement is not supported.

Declaring a `variant` under `arrangement flow`, or a `row`/`column`/`grid` under `arrangement freeform`, is a compile-time error - the body must match the mode on the line that opened it.

After choosing placement, [fill the template from a screen](screens.md#level-2-structure). The screen's bindings stay the same across the size variants you declared.

## See also

- [Layouts and templates](templates.md) - the constructs `arrangement` belongs to, and how they nest.
- [Screens](screens.md) - where a template is filled with content.
- [Forms](forms.md) - the same "one contract, many presentations" principle applied to command properties.
- [UI profile](ui-profile.md) - where a build's default size class and target platforms are declared.
