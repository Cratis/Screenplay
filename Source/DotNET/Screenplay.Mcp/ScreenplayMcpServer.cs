// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Hosts the Screenplay MCP protocol over caller-owned text streams.
/// </summary>
public static class ScreenplayMcpServer
{
    /// <summary>
    /// Runs one sequential MCP connection until the input reaches its end.
    /// </summary>
    /// <param name="root">The physical application directory to serve (including its corresponding Git worktree roots): an existing directory, or a missing one whose parent exists; created on the first write. Null chooses a root per workspace.</param>
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
    public static void Run(string? root, TextReader input, TextWriter output) => Run(root, input, output, null);

    /// <summary>Runs a connection with host-owned named rule execution for run-specifications.</summary>
    /// <param name="root">The application directory, or null to choose a root per workspace.</param>
    /// <param name="input">The caller-owned JSON-RPC reader.</param>
    /// <param name="output">The caller-owned JSON-RPC writer.</param>
    /// <param name="provider">The host factory creating one disposable provider per run-specifications call, or null to retain opaque-body refusals.</param>
    /// <remarks>Screenplay does not compile or execute C#. Provider isolation and resource limits belong to the host.</remarks>
    public static void Run(string? root, TextReader input, TextWriter output, ISemanticBodyExecutionProviderFactory? provider) =>
        Run(new ScreenplayMcpRun(root, provider), input, output);

    /// <summary>Runs a host-configured connection with cancellation for rule execution.</summary>
    /// <param name="configuration">The root, run factory and host cancellation.</param>
    /// <param name="input">The request stream.</param>
    /// <param name="output">The response stream.</param>
    /// <remarks>Timeout and cancellation map opaque bodies to Unsupported. The synchronous connection waits for the asynchronous provider, then disposes it before returning the report.</remarks>
    public static void Run(ScreenplayMcpRun configuration, TextReader input, TextWriter output) =>
        new McpConnection(new McpTools(configuration.Root is null ? null : new McpRoot(configuration.Root), configuration.ProviderFactory, configuration.CancellationToken)).Run(input, output);
}
