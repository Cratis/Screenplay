// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool;

static class CommandLineInformation
{
    internal static string Usage => $"""
        Screenplay compiler and model authoring tools

        Usage:
          {CliCommandCatalog.Check.Usage}
          {CliCommandCatalog.Test.Usage}
          {CliCommandCatalog.Mcp.Usage}
          {CliCommandCatalog.Contract.Usage}
          {CliCommandCatalog.EnvironmentKeys.Usage}
          {CliCommandCatalog.Restore.Usage}
          {CliCommandCatalog.Migrate.Usage}
          {CliCommandCatalog.Export.Usage}
          {CliCommandCatalog.Diff.Usage}
          {CliCommandCatalog.Rename.Usage}
          screenplay {CliCommandCatalog.Help.Aliases[0]}
          screenplay {CliCommandCatalog.Version.Aliases[0]}

        A folder of .play files describes one application.
        --scope reports the named scope and its direct dependent declarations after whole-application binding.
        --check selects opt-in structural completeness warnings; repeat it to combine selections.
        Completeness checks run only when the whole model has no errors.
        --open-questions report prints host-only model-note.open-question findings from .screenplay/notes.json.
        --open-questions fail exits 1 for open questions in the reported scope; unreadable notes exit 2.
        screenplay.json at the nearest model root declares validation settings; explicit flags override each setting.
        Model notes are ignored unless open questions are selected by the file or --open-questions.
        --format json writes one JSON report with diagnostics, open questions, refusal and the effective gate.
        --print-gate prints the effective gate and sources without compiling; --no-gate-file bypasses the file.
        Invalid or unreadable gate files exit 2 with their path, line and column.
        Scoped exit codes follow the reported set; a separate line shows whole-application defects.
        Exit codes: 0 clean, 1 defects (including warnings with --warnaserror), 2 could not run.
        test runs the deterministic in-memory reference evaluator, without external services.
        test exit codes: 0 all selected specifications passed, 1 failed, 2 could not run, 3 unsupported or unbound.
        environment lists the keys a model needs from the environment it runs in, and marks which are secrets.
        environment exit codes: 0 listed, 1 the model has errors, 2 could not run.
        restore pins the extensions a model's systems reference in screenplay.lock and restores their contracts into the cache.
        restore resolves only references the lock lacks; --update re-resolves all of them; --locked never resolves and fails on a reference the lock does not pin (CI).
        restore --source <directory> reads packages from a folder laid out <reference>/<version>/contract.play; --vendor also copies the contracts into the model's extensions folder.
        restore exit codes: 0 restored, 1 a reference could not be resolved or restored, 2 could not run.
        migrate rewrites files written for an earlier Screenplay version; --to defaults to the compiler's version and --dry-run writes nothing.
        migrate exit codes: 0 all migrated, 2 a file needs review, 1 could not migrate.
        A folder is a model root: test, export, diff and rename use its .screenplay/identities.json when present, as MCP does.
        export writes the canonical executable model (ESM) bytes; --scope keeps one module, feature or slice and the application-wide declarations.
        export exit codes: 0 written, 1 the model has no executable model, 2 could not run.
        diff compares two model versions (files or folders) and prints declaration, member, event contract and specification changes.
        diff exit codes: 0 compared, 2 could not run.
        rename renames a declaration through the same proposal and apply as MCP, keeping its semantic identity.
        rename exit codes: 0 renamed, 1 the rename was refused, 2 could not run.
        The MCP server uses stdio; a supplied root must exist unless --create-root is used.
        Open an empty model folder through MCP to create its first typed document.
        MCP proposals do not write files; review them before invoking apply.
        """;

    internal static bool TryPrint(string[] arguments, TextWriter output)
    {
        var command = CliCommandCatalog.Resolve(arguments);
        if (arguments.Length == 1 && command == CliCommandCatalog.Version)
        {
            var assembly = typeof(CommandLineInformation).Assembly;
            output.WriteLine(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version!.ToString());
            return true;
        }

        if ((arguments.Length == 1 && command == CliCommandCatalog.Help) ||
            (arguments.Length == 2 && command != CliCommandCatalog.Check && command != CliCommandCatalog.Help && command != CliCommandCatalog.Version && CliCommandCatalog.Help.Aliases.Contains(arguments[1])))
        {
            output.WriteLine(Usage);
            return true;
        }

        return false;
    }
}
