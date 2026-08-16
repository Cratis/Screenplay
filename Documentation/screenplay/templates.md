# Layouts and templates

Four words, each meaning exactly one thing:

| Word | What it is | How many |
| --- | --- | --- |
| **Layout** | The application's base navigational look — the shell holding a top bar, a navigation region, a content region, a footer. | An application has **one**, and selects it. |
| **Screen template** | A reusable shape that goes *inside* that shell, at module, feature or slice level. | An application has **many**. |
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
- `arrangement` says how those slots share the space. It is optional: a layout that only names its slots is a complete declaration.

An application selects its layout from a [ui profile](ui-profile.md), the same way it selects its theme:

```screenplay
layout AppShell
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

`fits slot <name>` is the single rule that makes nesting work at every level: a module's template fits a slot on the application layout, a feature's template fits a slot the module's template declares, a slice's fits one the feature's declares. The same word means the same thing however deep you go.

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

## See also

- [Layout arrangement](layout-arrangement.md) — how a layout or template arranges the slots it declares.
- [Screens](screens.md) — the three levels a screen is expressible at, and how its names resolve.
- [Contributions](contributions.md) — the many-to-one counterpart of a slot.
- [UI profile](ui-profile.md) — where a build selects its layout, theme, platforms and packages.
