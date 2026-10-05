// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// The exception that is thrown when an MCP request cannot be admitted or completed.
/// </summary>
/// <param name="message">The reason for rejection.</param>
/// <param name="code">The JSON-RPC error code, or zero for a tool execution failure.</param>
public sealed class McpFailure(string message, int code = 0) : Exception(message)
{
    /// <summary>
    /// Gets the stable failure discriminator; messages are not machine-readable identifiers.
    /// </summary>
    public string FailureKind { get; init; } = code switch
    {
        -32700 => "InvalidJson",
        -32600 => "InvalidRequest",
        -32601 => "UnknownMethod",
        -32602 => "InvalidArguments",
        _ => "RequestFailed"
    };

    internal int Code { get; } = code;
}
