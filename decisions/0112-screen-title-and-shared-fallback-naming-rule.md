---
id: 0112
title: A top-level `title` is the screen title, and names fall back through one shared rule
status: proposed
stage: none
class: contract
reversibility: cheap
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A screen has no title of its own.

- `ScreenSyntax(Name, File, Directives, Location)` has `Description`, documented as report-only and "distinct from the
  UI title" (`ScreenSyntax.cs:41-55`). `title "<text>"` is a body directive, `ScreenTitleSyntax` (`ScreenSyntax.cs:152`),
  documented as a section title (`screens.md` widget table; the section example is `screens.md:44-47`). SoloCRM writes
  it directly under each screen (`Crm.play:67`, `:191`, `:239`, `:303`, `:486`, `:507`, `:600`, `:682`).
- Other text already has a home: actions use `label` (`ScreenSyntax.cs:80`), guarded actions require a label head,
  toolbar buttons and navigation items use `title` (`navigation.md:23-30`), templates use `display` with a fallback to
  `Name` (`LayoutSyntax.cs:165`, `:247`). All accept `$strings.<key>`.
- Scene's `Screen` has no title (`Screen.cs:32-60`). `NavItem.Title`, `BlueprintScreen.Title` and
  `NavigationItem.Label` exist.
- Stage lowers `title` to a `core:title` content component (`ScreenDirectiveConverter.cs:135`) and falls back to
  `screen.name` for navigation (`blueprint.ts:133`; `navigationTree.ts:150-157`). Its in-progress humanization work
  (Stage #736 to #738) takes the first `core:title` it finds anywhere, including nested slots, in `Object.values` order
  (`screenTitle.ts:14`, `:29`), lowercases every word after the first (`stageFormSchema.ts:120-123`) and keeps the
  `Screen` suffix. A section title can become the navigation label of a screen, and `RegisterSKU` becomes "Register sku".

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **The screen title.** One extraction rule: only a `title` directive that is a direct child of the screen body, not
   inside a section, slot or any other container, is the screen's title. The first such directive wins; each later one
   is a warning. The compiler exposes it as an effective
   `ScreenSyntax.Title`, and Scene gets an init-only `Screen.Title` (a string; `$strings` allowed). It is used for
   navigation, breadcrumbs, the window title and reports. An explicit navigation `label` still wins for navigation. No new
   keyword.
2. **One fallback rule**, documented in `internationalization.md` and shared by Studio, Stage and the Arc generator,
   for any name shown to a person when no text is authored:
   1. split the PascalCase name into words;
   2. for a screen, drop a trailing `Screen`, `Page` or `Dialog`; if the name is empty afterwards (a screen named
      `Screen`), keep the name without that step;
   3. keep a run of capitals as written, and split before the last capital of a run that is followed by a lowercase
      letter (`RegisterSKU` becomes "Register SKU"; `SKUList` becomes "SKU list");
   4. write the result in sentence case: the first word starts with a capital, every later word is lower case, and a
      run of two or more capitals kept by step 3 stays as written wherever it stands (`postalCode` becomes "Postal
      code", `RegisterSKU` "Register SKU", `SKUList` "SKU list", `ExportToPDF` "Export to PDF"). A single capital
      letter is an ordinary word (`MakeAChoice` becomes "Make a choice").

   Fallbacks are not localized. Authors write `$strings` where that matters.
3. **Authored text always wins:** a localized label or title, then a literal one, then the fallback. The fallback is never
   applied to authored text, to `description`, or to a semantic name used as an identity.
4. **Keep the words.** `title` stays for screens, buttons, navigation and dialogs. `label` stays for actions, fields and
   columns. `description` stays report-only. The documentation says which each construct uses.
5. **Consumers read the effective title.** Stage reads `Screen.Title` once Scene has it. Until then, Stage reads the
   top-level `title` of the screen body by the same rule as item 1.
6. **No ESM change.** Screen syntax is outside the executable model; `Title` follows `ast-compatibility.md:28`.

## Options considered

- **A `screen <Name> title "..."` header form.** Rejected: a second way to say what the directive already says in every
  model.
- **Reuse `description`.** Rejected: it is documented as distinct and non-localizable.
- **Humanize only in Stage.** Rejected: Studio and the Arc generator would differ.
- **Use the first nested title as the screen title.** Rejected: a content title is not necessarily a page title.
- **Rename semantic identifiers for readability.** Rejected: it changes references for a presentation concern.
- **One `title` or `label` word for everything.** Rejected: churn without a gain.

## Consequences

- **Screenplay:** the effective title, the duplicate-title warning and documentation.
- **Scene:** `Screen.Title` in .NET and TypeScript. **Stage:** `ScreenDirectiveConverter` fills it; the title lookup
  reads it; `humanize` follows the shared rule. The Stage batch (#736 to #740) is amended for this. The front-end
  `humanize` and `humanizeScreenName` on the #736 branch already follow item 2. Stage's C# fallbacks for synthesized
  scene content and command form schemas do not yet: they capitalize every word and do not split a run of capitals
  before a lowercase word (`postalCode` gives "Postal Code", `SKUList` stays "SKUList"), and move to the shared rule.
- **Arc, Chronicle, CLI:** none. **AI corpus:** states which word to use where.
- Models that already write `title` get correct navigation labels with no edit.
- Duplicate headings (navigation, window title and a content title showing the same text) are the main risk.
  `$strings` titles must be resolved before navigation is built.

## Open points

1. Whether a screen may carry a navigation label separate from its title beyond today's `contribute to Navigation`
   `label`.
2. Whether acronym detection needs an exception list. The first rule is runs of capitals, split as in item 2.
3. Whether an authored `emphasis` or `presentation placement` always wins over Stage's "first adjacent action is
   primary" default; this was not verified.

## Related

Stage #736 to #740; Scene `Screen`.
