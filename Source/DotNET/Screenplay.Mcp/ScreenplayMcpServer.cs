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
    /// <param name="root">The existing physical application directory to serve.</param>
    /// <param name="input">The reader supplying newline-delimited JSON-RPC requests.</param>
    /// <param name="output">The writer receiving only JSON-RPC responses, flushed after each response.</param>
    /// <exception cref="McpFailure">The root is rejected or a request exceeds the size limit.</exception>
    /// <exception cref="IOException">A file-system or transport operation fails.</exception>
    /// <remarks>
    /// The caller owns both streams and their encoding; neither stream is closed by this method.
    /// Startup, transport, and request-size failures propagate to the caller. Request errors are
    /// returned as protocol responses. No console configuration, installation, update, or network
    /// operations are performed. Only explicit apply and recovery requests mutate model files.
    /// </remarks>
    public static void Run(string root, TextReader input, TextWriter output) =>
        new McpConnection(new McpTools(new McpRoot(root))).Run(input, output);
}
