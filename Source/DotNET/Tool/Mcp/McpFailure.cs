// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp;

/// <summary>
/// The exception that is thrown when an MCP request cannot be admitted or completed.
/// </summary>
/// <param name="message">The reason for rejection.</param>
/// <param name="code">The JSON-RPC error code, or zero for a tool execution failure.</param>
sealed class McpFailure(string message, int code = 0) : Exception(message)
{
    internal int Code { get; } = code;
}
