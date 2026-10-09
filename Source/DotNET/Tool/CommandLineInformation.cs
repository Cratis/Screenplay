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
          screenplay {CliCommandCatalog.Help.Aliases[0]}
          screenplay {CliCommandCatalog.Version.Aliases[0]}

        A folder of .play files describes one application.
        --scope reports the named scope and its direct dependent declarations after whole-application binding.
        --check selects opt-in structural completeness warnings; repeat it to combine selections.
        Completeness checks run only when the whole model has no errors.
        Scoped exit codes follow the reported set; a separate line shows whole-application defects.
        Exit codes: 0 clean, 1 defects (including warnings with --warnaserror), 2 could not run.
        test runs the deterministic in-memory reference evaluator, without external services.
        test exit codes: 0 all selected specifications passed, 1 failed, 2 could not run, 3 unsupported or unbound.
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
