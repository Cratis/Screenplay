// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Tool;

#if DEBUG
if (args.Length > 0 && args[0] == "--specifications")
{
    return await Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args[1..]);
}
#endif

return ToolCommands.Run(args, Console.Out, Console.Error);
