// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Hosts the Screenplay MCP protocol over caller-owned text streams.
/// </summary>
public static class ScreenplayMcpServer
{
    /// <summary>
    /// Runs one sequential MCP connection until the input reaches its end.
    /// </summary>
    /// <param name="root">The existing physical application directory to serve (including its corresponding Git worktree roots), or null to choose a root per workspace.</param>
    /// <param name="input">The reader supplying newline-delimited JSON-RPC requests.</param>
    /// <param name="output">The writer receiving only JSON-RPC responses, flushed after each response.</param>
    /// <exception cref="McpFailure">The root is rejected or a request exceeds the size limit.</exception>
    /// <exception cref="IOException">A file-system or transport operation fails.</exception>
    /// <remarks>
    /// The caller owns both streams and their encoding; neither stream is closed by this method. A null root
    /// starts a dynamic server: open-workspace chooses the root from an explicit path, the client's roots, or
    /// the working directory. A supplied root admits switching only to the same relative model folder in a
    /// registered Git worktree of its repository; switching discards outstanding proposals. Startup, transport,
    /// and request-size failures propagate to the caller. Request
    /// errors are returned as protocol responses. No console configuration, installation, update, or network
    /// operations are performed. Only explicit apply and recovery requests mutate model files.
    /// </remarks>
    public static void Run(string? root, TextReader input, TextWriter output) =>
        new McpConnection(new McpTools(root is null ? null : new McpRoot(root))).Run(input, output);
}
