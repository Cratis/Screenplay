// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

public class a_fifo_connection : a_worktree_connection
{
    internal static void CreateFifo(string path)
    {
        var start = new ProcessStartInfo("mkfifo") { UseShellExecute = false, ArgumentList = { path } };
        using var process = Process.Start(start)!;
        if (!process.WaitForExit(10000))
        {
            process.Kill(entireProcessTree: true);
            throw new McpFailure("Spec FIFO setup timed out.");
        }
        process.ExitCode.ShouldEqual(0);
    }

    // A completion deadline exposes a blocked metadata open without sleeping or supplying a FIFO writer.
    internal Task<JsonElement> OpenWithinDeadline(string path) => Task.Run(() => Call("open-workspace", new { path }).GetProperty("result")).WaitAsync(TimeSpan.FromSeconds(5));
}
