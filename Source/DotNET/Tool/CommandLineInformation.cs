// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Tool;

static class CommandLineInformation
{
    internal const string Usage = """
        Screenplay compiler and model authoring tools

        Usage:
          screenplay [<file.play|folder>] [--warnaserror] [--no-color]
          screenplay mcp <model-folder>
          screenplay --help
          screenplay --version

        A folder of .play files describes one application.
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
            (arguments.Length == 2 && arguments[0] == "mcp" && (arguments[1] == "--help" || arguments[1] == "-h")))
        {
            output.WriteLine(Usage);
            return true;
        }

        return false;
    }
}
