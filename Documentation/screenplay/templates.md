---
title: "Screenplay layouts and templates: compose shared UI"
description: "Use a Screenplay layout for the application shell, screen templates for reusable content regions, and dialog templates for content above the application."
---

# Layouts and templates

A Screenplay layout defines the application shell; templates give screens reusable regions to fill. Use a screen template for content inside the shell and a dialog template for content that opens over it. This keeps shared navigation and framing separate from each screen's data and actions.

| Word | What it is | Where it belongs |
| --- | --- | --- |
| **Layout** | The application shell, with regions such as a top bar, navigation, content and footer. | Selected by a UI profile. |
| **Screen template** | A reusable structure inside the shell. | Declared on a module. |
| **Dialog template** | A reusable structure for content that opens over the application. | Declared on a module. |
| **Screen** | The content that fills a template's named regions. | Declared inside a slice. |

Layouts and templates declare slots: named regions where content belongs. An optional [arrangement](layout-arrangement.md) positions those slots. Start by naming the regions you need; add placement rules when the structure needs them.

## `layout` — the application's shell

A layout is a top level declaration, alongside `ui profile` and `theme`:

```screenplay
layout AppShell
  topbar
  navigation contributes Navigation
  content
  footer

  arrangement flow
    column
      topbar height 56
      row
        navigation width 240
        content grow
      footer height 32
```

- Each plain line in the body **declares a slot**. `contributes <ContributionPoint>` opens it up to contributors declared anywhere in the document — see [Contributions](contributions.md). The application shell is where an application-wide contribution point such as `Navigation` belongs.
- `category <name>` and `type <name>` provide catalog metadata for designers and renderers. Every layout and template needs a `category` (`PLAY0720`).
- `exposes <name> [<Type>]` names a value the structure makes available to descendants or package tooling.
- `outlet <name>` declares a recursive fill point for package components and design-time tools.
- `navigation topbar` and `navigation sidebar` blocks hold the application's own navigation: a logo and items with targets, slots and sub-items. A module inherits, extends or overrides them - see [Navigation regions](navigation.md).
- `arrangement` says how those slots share the space. It is optional: a layout that only names its slots is a complete declaration.

An application selects its layout from a [ui profile](ui-profile.md), the same way it selects its theme:

```screenplay
layout AppShell
  category application
  type masterDetail
  exposes selectedItem String
  outlet details
  content

ui profile Desktop
  target platform web
  root template AppShell
```

A document may declare more than one layout, so that different profiles can select different shells — but each profile selects exactly one, and a profile naming a layout the document does not declare is reported.

## `screen template` — a shape inside the shell

A screen template is declared inside a `module`, and says which slot of its parent it fills:

```screenplay
module Invoicing
  screen template MasterDetail
    fits slot content

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

`fits slot content` places `MasterDetail` in a parent's slot named `content`. Matching uses the slot name, not the template's declaration location. The parent can be the application layout or another template, so nested structures use the same placement rule.

Declare templates on a `module`; features and slices use them rather than declaring their own.

You can omit `fits slot`. The template remains valid, and the renderer decides where to place it.

## `dialog template` — a shape over the application

A dialog template declares slots for content that opens over the application. It does not declare `fits slot`, because it does not occupy a region of the structure beneath it:

```screenplay
module Invoicing
  dialog template RegisterInvoiceDialog
    body
    actions
```

Writing `fits slot` on a dialog template — or on a layout — is a compile-time error.

## `screen` — filling a template

A screen names the structure it fills with `template <Name>`, and provides the content of each slot:

```screenplay
module Invoicing
  screen template MasterDetail
    sidebar
    main

  feature InvoiceManagement
    slice StateView InvoiceDetails
      query GetInvoice => InvoiceDetailsReadModel

      screen InvoiceDetails
        template MasterDetail
          sidebar
            data InvoiceDetailsReadModel via query GetInvoice
          main
            section lineItems
              table lineItems
                column lineNumber
```

To fill a dialog, name its dialog template with the same `template` directive. Here the screen supplies a title in `body` and a command action in `actions`:

```screenplay
module Invoicing
  dialog template RegisterInvoiceDialog
    body
    actions

  feature InvoiceManagement
    slice StateChange RegisterInvoice
      command RegisterInvoice
        invoiceId Uuid

      screen RegisterInvoiceScreen
        template RegisterInvoiceDialog
          body
            title "Register invoice"
          actions
            action RegisterInvoice
```

A screen names its content template, not the application's `layout`. A `ui profile` selects the shell once per build, so individual screens do not choose the application-wide frame.

## Own and blueprint templates

Use an own template when you declare the structure in the application. To reuse a packaged structure, reference a blueprint: a package listed under `packages` in a `ui profile` that supplies layouts, screen templates or dialog templates. The reference identifies which source to use:

```screenplay
template MasterDetail
template blueprint Crm CustomerDetail
template blueprint Crm CustomerDetail version "^1.4"
```

- `template <Name>` names an own template.
- `template blueprint <Blueprint> <Name> [version "<range>"]` names the template `<Name>` of the blueprint package `<Blueprint>`. The optional `version` limits the accepted package versions using Scene's supported semantic-version ranges: `*`, an exact version, or a `^`, `~`, `>=`, `>`, `<=` or `<` range over `major[.minor[.patch]]`. A range Scene cannot read is `PLAY0721`. Leaving it out accepts any version.
- Both forms are valid wherever `template` is: as an assignment at application, module, feature or slice scope, and inside a screen. Scene resolves a reference with `resolveTemplateReference` (C# `TemplateReferenceResolver.Resolve`): `UnknownBlueprint`, `VersionNotSatisfied` (the installed blueprint has drifted outside the range) and `UnknownTemplate` are reported where the blueprint is installed, because the compiler has no visibility into a package's catalog.
- When at least one `ui profile` lists packages, a blueprint reference whose blueprint no profile lists is `PLAY0722`, the same rule `PLAY0632` applies to components.
- The same reference names the application's shell inside a `ui profile`: `root template <reference>` (with its `option` lines), and the template a new screen starts from, `new screens use template <reference>`. See [UI profile](ui-profile.md).
- A blueprint reference is not checked against the document's own `scopes` or nesting - the blueprint's templates carry their own metadata, which Scene checks with `ApplicableTemplates`.

## Inheriting a template choice

A bare `template <Name>` can also appear at application, module, feature or slice scope. It assigns the default template for the declarations below that scope until a more specific scope overrides it:

```screenplay
template AppShell

module Invoicing
  template FeatureShell

  feature InvoiceManagement
    template FeatureShell

    slice StateView InvoiceDetails
      template FeatureShell
```

Use scoped template assignments when a whole feature area shares the same shape. Use a screen-local `template <Name>` body when the screen fills slots directly. The assignment is preserved as authored metadata; a renderer that cannot apply a scoped template must report that limitation instead of falling back to an unstructured screen.

## Template content and picker metadata

A screen or dialog template can provide its own chrome for a slot with `content <slot>`, and say how a template picker presents it:

```screenplay
module Invoicing
  screen template MasterDetail
    display "Master / detail"
    description "A list beside the selected item"
    scopes feature, slice

    header
    list
    details

    content header
      title "Invoices"
```

- `content <slot>` holds screen directives the template renders in that slot itself. The slot must be one the template declares (`PLAY0629`). Content may use another template, but templates cannot nest in a cycle (`PLAY0656`).
- `display` and `description` are the name and one-line description a picker shows.
- `scopes` restricts where the template may be used: `application`, `module`, `feature`, `subfeature` or `slice` (optionally with slice kinds, see below), or `none`. Without it the structural role decides. Using a restricted template outside its scopes is `PLAY0631`. A layout may declare `scopes` too.

These map to Scene's `ScreenTemplate.Content`, `DisplayName`, `Description` and `Metadata.Scopes` and `Metadata.SliceKinds`.

## Category and picker metadata

Every `layout`, `screen template` and `dialog template` declares a `category` - the group a template picker lists it under, such as `category Workspace` or `category "Business / List"`. A template without one is `PLAY0720`, a warning. A blueprint package is built with warnings as errors, so there a missing category fails the build; an own template only warns until the author gives it one. The category is a free string; one a picker does not know is kept and shown as written. It never decides where a template may be used - `scopes` does.

## Applicability: `scopes` and slice kinds

`scopes` restricts where a template may be used. At `slice` scope it can also name the slice kinds the template is for:

```screenplay
module Invoicing
  screen template InvoiceList
    category List
    scopes slice stateview

  dialog template RegisterInvoiceDialog
    category Dialog
    scopes slice statechange

  screen template Workspace
    category Workspace
    scopes module, feature, subfeature
```

- `scopes slice stateview` allows the template on a state view slice only, `scopes slice statechange` on a state change slice only, `scopes slice statechange|stateview` on either. `scopes slice` alone allows any slice that presents something. Kind names are case-insensitive and print lower-case.
- A slice that presents nothing - an automation or translate slice - takes no template: using a restricted template there is `PLAY0631`, as is using one on a slice kind it excludes.
- A template that declares no `scopes` is not restricted: the structural role decides, and the compiler accepts it wherever it is used.

Scene's `ApplicableTemplates` uses `scopes` and `sliceKinds` metadata in place of its per-level defaults when deciding where a template applies. An Automation slice has no presentation. Use these restrictions to keep a template meant for one kind of task, such as a state-change dialog, out of unrelated template choices.

## `exposure` — letting a template be configured

Declare an exposure when screens should be able to configure part of a shared template without changing the template itself. It names the component property that can change and, for collections, which operations are allowed. Put the declaration at the top level and name the layout or template that owns the component:

```screenplay
exposure for MasterDetail
  property navigation.items label "Navigation" operations add, reorder fields label, icon
  property "invoices:list".pageSize label "Page size"

exposure for DetailPane
  property navigation.items reexposes MasterDetail operations add
```

- The component is an identifier or a quoted exact stable id, followed by the property path.
- `operations` marks the property as a collection and grants `add`, `remove`, `reorder` and `edit-fields` one by one (`operations none` grants nothing). Without `operations` the property is a single value.
- `fields` restricts which item fields a consumer may change.
- `reexposes <Owner>` passes an outer owner's exposure through a nested template. The outer owner must expose the same property (`PLAY0624`), and re-exposures may not form a cycle (`PLAY0623`).

The owner must be a layout, screen template or dialog template (`PLAY0622`).

## `instance` — configuring what was exposed

After the owner exposes a property, a screen or nested template can store its configuration in an `instance`. This example sets a page size and adds a navigation item:

```screenplay
instance InvoiceList
  set "invoices:list".pageSize = 50
  items navigation.items
    item open
      label = "Open"
      icon = "folder"
```

- `set <component>.<path> = <value>` stores a typed literal for a single-value exposure.
- `items <component>.<path>` adds items to a collection exposure; each `item <id>` holds `<field> = <value>` lines.

The instance must be a screen, screen template or dialog template (`PLAY0626`). Storing a value no exposure exposes is `PLAY0627`; using `set` on a collection or `items` on a single value is `PLAY0628`. These map to Scene's `ExposureDeclaration` and `InstanceContribution`.

Once the slots and template choices are in place, [arrange them](layout-arrangement.md) to control how they share space, or [fill them from a screen](screens.md#level-2--structure).

## See also

- [Layout arrangement](layout-arrangement.md) — how a layout or template arranges the slots it declares.
- [Screens](screens.md) — the three levels a screen is expressible at, and how its names resolve.
- [Contributions](contributions.md) — the many-to-one counterpart of a slot.
- [Navigation regions](navigation.md) — the topbar and sidebar of the shell, with module level inherit, extend and override.
- [UI profile](ui-profile.md) — where a build selects its layout, theme, platforms and packages.
