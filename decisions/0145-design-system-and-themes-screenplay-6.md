---
id: 0145
title: One design system per application, implemented by themes (Screenplay 6)
status: proposed
stage: designed
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A `theme` only named the component packages it was compatible with, and a `ui profile`'s `appearance` carried a few
look settings (`accent`, `corners`, `font`) that every domain of an application repeated with its own values. Nothing said
which tokens a design has, how a component is made of them, how the product speaks, or which rules a finished look must
satisfy, so hosts such as Stage, Scene and Direct each guessed (Cratis/Screenplay#1194).

## Decision

1. **One `design system` per application.** A second is `PLAY0917`. It is the contract: modes, tokens, components, words
   and rules. It holds no values.
2. **Themes implement it.** A theme that `implements` the design system assigns values (`palette`, `values` and a block per
   mode) and never redefines a component. A theme without `implements` stays a compatibility-only theme and keeps working.
3. **Words and rules live in the design system.** `words` holds `voice` and `naming`; `rules` holds
   `contrast <part> on <part> at least <ratio>`, evaluated for every theme, mode, variant and state except `disabled`
   (warning `PLAY0928`).
4. **A profile may select a non-default theme.** Without a `theme` line a profile uses the design system's required
   `default theme`; a `theme` line overrides it and must name a theme that implements the design system (`PLAY0926`).
5. **`appearance` narrows to `color scheme`.** `accent`, `corners` and `font` are deprecated with warning `PLAY0929`; the
   value belongs in the theme as a `color`, `radius` or `font` token. Removing them is a later major release.
6. **Modes belong to the design system.** Every theme gives every `color` token a value in every mode (`PLAY0921`); a
   mode block for an undeclared mode is `PLAY0918`.
7. **Token categories, component parts and states are closed sets**: categories `color`, `space`, `size`, `radius`,
   `border`, `shadow`, `font`, `type`, `motion`, `opacity`, `z`; parts `text`, `background`, `border`, `radius`, `height`,
   `padding`, `shadow`, `font`; states `default`, `hover`, `focus`, `active`, `disabled`, `selected`. A part takes only
   tokens of the categories it accepts (`PLAY0924`).
8. **A `type` token is a single value in this version.** A composite type style is not expressible yet.
9. **Contrast is the WCAG ratio, measured on `#rgb`, `#rrggbb`, `#rrggbbaa` and `rgb(...)` literals only**, after
   references are followed. A color that cannot be measured is not checked.
10. **The executable semantic model defers the construct.** Binding reports it as `PLAY0269`; hosts read the design from the
    syntax tree or from `Cratis.Screenplay.Design.DesignSystemResolver` (`SelectTheme`, `SelectMode`, `Resolve`).

## Consequences

- `DesignSystemSyntax` and the extended `ThemeSyntax` join the syntax tree and the canonical JSON; diagnostics
  `PLAY0916` to `PLAY0930` are permanent codes.
- The MCP `ui-profiles` read tool returns the design system, the themes, the resolved values per mode and the theme and mode
  each profile resolves to. `DesignSystem` is a new authoring-index kind.
- `Samples/Invoicing` and `Samples/AcmeCommerce` carry a design system and implementing themes; the Acme domains share one
  design system with one theme each.
- Documentation: `design-system.md`, `theme.md`, `ui-profile.md`, `grammar.md` and the diagnostics catalogue.

## Out of scope

Per-module design systems, theme inheritance, CSS generation and the mapping of tokens onto component packages. Those belong
to the hosts that render a design and are separate decisions.
