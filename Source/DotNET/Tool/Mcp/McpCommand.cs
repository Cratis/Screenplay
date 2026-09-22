// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpCommand
{
    internal static int Run(string[] arguments)
    {
        if (arguments.Length != 2)
        {
            Console.Error.WriteLine("Usage: screenplay mcp <root-directory>");
            return 2;
        }

        try
        {
            Console.InputEncoding = new UTF8Encoding(false, true);
            Console.OutputEncoding = new UTF8Encoding(false, true);
            var root = new McpRoot(arguments[1]);
            new McpConnection(new McpTools(root)).Run(Console.In, Console.Out);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"MCP server stopped: {exception.Message}");
            return 2;
        }
    }
}
