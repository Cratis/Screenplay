// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Tool;

static class CommandLineInformation
{
    internal const string Usage = """
        Screenplay compiler and model authoring tools

        Usage:
          screenplay [<file.play|folder>] [--scope <Module>[.<Feature>[.<Slice>]]] [--check <name>[,<name>]|all] [--warnaserror] [--no-color]
          screenplay test [<file.play|folder>] [--filter <specification-address>] [--format text|json]
          screenplay mcp <model-folder>
          screenplay --help
          screenplay --version

        A folder of .play files describes one application.
        --scope reports the named scope and its direct dependent declarations after whole-application binding.
        --check selects opt-in structural completeness warnings; repeat it to combine selections.
        Completeness checks run only when the whole model has no errors.
        Scoped exit codes follow the reported set; a separate line shows whole-application defects.
        Exit codes: 0 clean, 1 defects (including warnings with --warnaserror), 2 could not run.
        test runs the deterministic in-memory reference evaluator, without external services.
        test exit codes: 0 all selected specifications passed, 1 failed, 2 could not run, 3 unsupported or unbound.
        The MCP server uses stdio and requires an existing physical directory.
        Open an empty model folder through MCP to create its first typed document.
        MCP proposals do not write files; review them before invoking apply.
        """;

    internal static bool TryPrint(string[] arguments, TextWriter output)
    {
        if (arguments.Length == 1 && (arguments[0] == "--version" || arguments[0] == "version"))
        {
            var assembly = typeof(CommandLineInformation).Assembly;
            output.WriteLine(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version!.ToString());
            return true;
        }

        if ((arguments.Length == 1 && (arguments[0] == "--help" || arguments[0] == "-h" || arguments[0] == "help")) ||
            (arguments.Length == 2 && (arguments[0] == "mcp" || arguments[0] == "test") && (arguments[1] == "--help" || arguments[1] == "-h")))
        {
            output.WriteLine(Usage);
            return true;
        }

        return false;
    }
}
