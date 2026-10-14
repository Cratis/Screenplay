---
id: 0076
title: Declare the validation gate in an authored screenplay.json at the model root
status: accepted
stage: implemented
decided: 2026-10-14
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Tool/**
  - Documentation/screenplay/tool.md
  - Documentation/screenplay/*.schema.json
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[#739](https://github.com/Cratis/Screenplay/issues/739) asks for a file that declares a repository's Screenplay validation gate, so scripts and CI steps stop repeating `--check`, `--warnaserror`, `--scope` and `--open-questions` for the standalone `screenplay` tool and for `cratis screenplay validate` ([Cratis/cli#347](https://github.com/Cratis/cli/issues/347)). The two tools must read it through the same library code, or they drift.

**What exists today.**
- The gate is command-line only (`Source/DotNET/Tool/ModelCheckOptions.cs`, `ModelOpenQuestions.cs`, `CommandLineInformation.cs`). The CLI shows the expanded check set in its JSON and console output; the standalone `screenplay` check has no JSON report and does not print the expanded selection (`Source/DotNET/Tool/ModelCheck.cs`).
- Neither tool locates an owning model root: each reads the folder it is given (`ModelRootDirectory`), and they derive the folder for open questions differently (the standalone tool widens to imported sources; the CLI uses the target folder or a file's parent).
- `.screenplay/` is server-owned ([0045](0045-model-notes-sidecar.md)): the MCP server and `screenplay baseline admit` write `identities.json`, `pending.json`, `notes.json` and `release-baseline.json` ([0070](0070-derive-event-mutability-from-an-admitted-release-baseline.md)) under one recovery journal, with owner-only permissions for writes. Model document operations cannot write into it.
- A model root can sit anywhere in a repository, and one repository can hold several models, so the gate belongs to the model root, not to the repository or `.cratis/`.

## Decision

1. **File:** an authored, committed `screenplay.json` at the model root, beside `.screenplay/`, not inside it. `.screenplay/` stays tool-owned; `screenplay.json` is written by people and never by the MCP server, `baseline admit` or any other tool. Owner-only `.screenplay/` permission rules do not apply to it.
2. **Locating the model root:** both tools resolve the root the same way, through one shared resolver, before reading the gate. Start at the target folder (a file target starts at its folder). The model root is the nearest folder at or above that start that contains `screenplay.json` or `.screenplay/`, searching upward and stopping at the repository root (the first folder containing `.git`) or the file-system root. If none is found, the start folder is the model root and the defaults apply. The nearest match wins, so nested models each keep their own gate and there is no ambiguity. The resolver is independent of imports.
3. **Shape (v1):** one JSON object with an optional `$schema` and one section, `validation`:

   ```json
   {
     "$schema": "https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/screenplay-v1.schema.json",
     "validation": {
       "checks": ["field-origins", "query-keys", "navigation"],
       "warningsAsErrors": true,
       "scope": "Workspaces.Tracking",
       "openQuestions": "fail"
     }
   }
   ```

   Each `checks` item is one name `--check` accepts (a check name, a diagnostic-code alias or `all`); comma-separated items are refused in JSON. Selections are deduplicated and reported in a defined canonical order. `all` expands to every check the running version supports, so an upgrade can strengthen the gate; an explicit list pins its membership. `scope` takes what `--scope` takes. `openQuestions` is `report` or `fail`; absent means not evaluated, as today. Every member is optional. A published `screenplay-v1.schema.json` lets editors validate the file. A later setting is a new section, not a new file.
4. **Strict reading:** malformed JSON, a duplicate member, an unknown top-level key, an unknown member of `validation`, an unknown check name or a wrong type is `Invalid`, never ignored, so a typo cannot open the gate. A directory at `screenplay.json`, an unreadable file, or a refused symlink or reparse point is `Unreadable`, never treated as absent. Refusals carry the location (line and column) and reason.
5. **Precedence:** an option given on the command line replaces that setting from the file; an omitted option never overrides it (`warningsAsErrors: true` stays true when `--warnaserror` is absent). If any `--check` occurs, the union of all command-line selections replaces the file's list. The file replaces the defaults; a missing file means today's defaults. `--no-gate-file` ignores the file entirely, the way to relax a declared gate for one run.
6. **One reader, two exit-code mappings:** a public `ScreenplayValidationGate.Read(string modelRoot)` in `Cratis.Screenplay` returns the declared gate or a typed failure (`Unreadable`, `Invalid`), and a public resolver applies the root rule and precedence. The standalone `screenplay` tool maps a refusal to exit 2 ("could not run"); `cratis screenplay validate` maps it to its invalid-input exit code. Neither parses the file itself.
7. **Effective gate visible:** the standalone check command gains `--format text|json`; with `json`, stdout carries one JSON report (diagnostics, open questions and any refusal), and it and the CLI's existing JSON report contain a `gate` object with the resolved `checks` (expanded), `warningsAsErrors`, `scope`, `openQuestions`, the resolved model root and gate-file path, and for each value its `source` (`commandLine`, `file`, `default`). `--print-gate` prints the same in text and exits 0 without compiling.

## Options considered

- **`screenplay.json` at the model root (chosen).** Authored configuration stays separate from tool-owned state, the file travels with the model, editors find it by name, and a section keeps room for later settings.
- **`.screenplay/validation.json` (the issue's first proposal).** Rejected: it puts a hand-edited file into the directory 0045 makes server-owned, with owner-only write permissions and one recovery journal, and blurs which files a person may edit.
- **`.cratis/screenplay.json` or a section of `.cratis/ai.json`.** Rejected: `.cratis/` is repository configuration for Cratis tooling and the AI corpus, not per-model; a repository with two models could not declare two gates.
- **Lenient reading (ignore unknown keys).** Rejected: a misspelled check name would silently remove a check from the gate.

## Default if unanswered

The gate stays command-line only, CI scripts keep repeating the flags for both tools, and Cratis/cli#347 stays blocked.

## Timeline and scope

In scope: the file, its JSON Schema, the reader and resolver, the standalone tool's use of it (including `--print-gate` and the JSON `gate` object), and the documentation. The CLI consumes the reader in Cratis/cli#347.

Out of scope: other settings (formatting, rendering), Studio or Stage display (they can read the file through the library later), and any change to what the checks themselves report. The CLI's adoption of the resolver, its exit-code mapping and its `gate` object are Cratis/cli#347.

## Verification

**Done when:** a model root with `screenplay.json` runs the declared checks with the declared warnings-as-errors, scope and open-questions mode, also when the tool is pointed at a file or a subfolder of that root; nested models keep their own gate; each command-line option overrides only its own value and `--no-gate-file` ignores the file; a missing file gives today's defaults; every invalid or unreadable case exits 2 with a location in the standalone tool; `--format json`, `--print-gate` and the JSON `gate` object show each resolved value and its source.

**Verify by:** reader specs for every accepted and refused shape (including duplicate members, a directory or unreadable file at the path), resolver specs for root location (file, subfolder, nested model, repository boundary) and precedence, Tool specs for exit codes, `--format json` and `--print-gate`, and a schema validation spec over the documented example.

## Consequences

Repositories declare their gate once; scripts run `screenplay <root>` or `cratis screenplay validate <root>` with no flags and get the same result. The docs gain a short table of which files beside a model are authored (`.play`, `screenplay.json`) and which are tool-owned (`.screenplay/`).

## Status notes

**2026-10-14 — accepted.** Accepted by Sindre Alstad Wilting (delegated through the coordinator after an independent review), with the CLI adoption in Cratis/cli#347.

## Related

Screenplay [#739](https://github.com/Cratis/Screenplay/issues/739); Cratis/cli [#347](https://github.com/Cratis/cli/issues/347). Builds on 0045 and 0070.
