// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Tool.Mcp;

namespace Cratis.Screenplay.Tool;

static class ToolCommands
{
    internal static readonly IReadOnlyDictionary<CliCommand, Func<string[], TextWriter, TextWriter, int>> Dispatch = new Dictionary<CliCommand, Func<string[], TextWriter, TextWriter, int>>
    {
        [CliCommandCatalog.Check] = (args, output, error) => ModelCheck.Run(args, output, error, !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null),
        [CliCommandCatalog.Test] = (args, output, error) => ModelTest.Run(args[1..], output, error),
        [CliCommandCatalog.Mcp] = (args, _, _) => McpCommand.Run(args),
        [CliCommandCatalog.Contract] = (args, output, error) => ContractCommand.Run(args[1..], output, error),
        [CliCommandCatalog.Help] = (args, output, error) => ModelCheck.Run(args, output, error, !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null),
        [CliCommandCatalog.Version] = (args, output, error) => ModelCheck.Run(args, output, error, !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null)
    };

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (CommandLineInformation.TryPrint(args, output)) return 0;

        return Dispatch[CliCommandCatalog.Resolve(args)](args, output, error);
    }
}
