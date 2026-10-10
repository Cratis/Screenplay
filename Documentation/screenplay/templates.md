# Layouts and templates

Four words, each meaning exactly one thing:

| Word | What it is | How many |
| --- | --- | --- |
| **Layout** | The application's base navigational look — the shell holding a top bar, a navigation region, a content region, a footer. | An application has **one**, and selects it. |
| **Screen template** | A reusable shape that goes *inside* that shell, declared at module level. | An application has **many**. |
| **Dialog template** | The same, for content that opens *over* the application. | An application has **many**. |
| **Screen** | An instance — it names the structure it fills and provides the content. | One per thing a user looks at. |

A layout and a template are both made of the same two things: the **slots** they declare, and the **[arrangement](layout-arrangement.md)** positioning those slots. What separates them is where they sit and what they say about their parent.

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
- `category <name>` and `type <name>` provide catalog metadata for designers and renderers.
- `exposes <name> [<Type>]` names a value the structure makes available to descendants or package tooling.
- `outlet <name>` declares a recursive fill point for package components and design-time tools.
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
  layout AppShell
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

`fits slot <name>` is the single rule that makes nesting work at every level, and it works by **slot name** rather than by where the template was declared: a template fits whichever structure in scope declares a slot of that name — the application layout, or another template that is itself inside it. Nesting therefore goes as deep as you build it, and the same word means the same thing however deep you go.

Templates are declared on a `module`. A feature or slice does not declare its own templates; it uses the ones its module declares. If two templates claim the same slot name, that is reported as ambiguous rather than guessed at — placing a template in the wrong region is far harder to diagnose than being told the name is not unique.

It is optional. A template that does not say which slot it fills is still a valid declaration — where it lands is then decided by whatever renders it.

## `dialog template` — a shape over the application

A dialog template is a screen template in everything but one respect: it declares no `fits slot`, because a dialog occupies no slot of the structure it opens over.

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

The same directive fills a dialog template — a dialog is filled exactly like a screen, because from the screen's side there is no difference:

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

A screen never names the application's `layout`. The shell is selected once, per build, by a `ui profile` — which is what keeps a screen portable across web, mobile and desktop instead of tied to one shell.

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

- `content <slot>` holds screen directives the template renders in that slot itself. The slot must be one the template declares (`PLAY0629`).
- `display` and `description` are the name and one-line description a picker shows.
- `scopes` restricts where the template may be used: `application`, `module`, `feature`, `subfeature` or `slice`, or `none`. Without it the structural role decides. Using a restricted template outside its scopes is `PLAY0631`. A layout may declare `scopes` too.

These map to Scene's `ScreenTemplate.Content`, `DisplayName`, `Description` and `Metadata.Scopes`.

## `exposure` — letting a template be configured

A template author decides which of its components a screen inside it may configure. That is an **exposure**, declared at the top level and keyed by the layout or template that owns the component:

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

A screen, or a template nested in an owner's slot, stores values for what was exposed to it:

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

## See also

- [Layout arrangement](layout-arrangement.md) — how a layout or template arranges the slots it declares.
- [Screens](screens.md) — the three levels a screen is expressible at, and how its names resolve.
- [Contributions](contributions.md) — the many-to-one counterpart of a slot.
- [UI profile](ui-profile.md) — where a build selects its layout, theme, platforms and packages.
