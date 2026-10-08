// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Tool;
using Cratis.Screenplay.Tool.Mcp;

if (CommandLineInformation.TryPrint(args, Console.Out))
{
    return 0;
}

if (args.FirstOrDefault() == "mcp")
{
    return McpCommand.Run(args);
}

if (args.FirstOrDefault() == "test")
{
    return ModelTest.Run(args[1..], Console.Out, Console.Error);
}

var useColors = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;
return ModelCheck.Run(args, Console.Out, Console.Error, useColors);
