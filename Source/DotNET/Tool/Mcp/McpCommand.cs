// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpCommand
{
    internal static int Run(string[] arguments)
    {
        var createRoot = arguments is ["mcp", "--create-root", _];
        var runWithoutRoot = arguments is ["mcp"];
        if (arguments.Length != 2 && !createRoot && !runWithoutRoot)
        {
            Console.Error.WriteLine("Usage: screenplay mcp [<root-directory>] | --create-root <directory>");
            return 2;
        }

        try
        {
            Console.InputEncoding = new UTF8Encoding(false, true);
            Console.OutputEncoding = new UTF8Encoding(false, true);
            var root = runWithoutRoot ? null : arguments[^1];
            if (createRoot)
            {
                // Desktop plugins have a host-owned data directory, not a user-config dialog.
                // Only this explicit opt-in creates a new model; ordinary MCP startup is unchanged.
                Directory.CreateDirectory(root!);
            }
            ScreenplayMcpServer.Run(root, Console.In, Console.Out);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"MCP server stopped: {exception.Message}");
            return 2;
        }
    }
}
