// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp;

/// <summary>Host configuration for a synchronous MCP connection.</summary>
/// <param name="Root">The workspace root, or null for an unbounded connection.</param>
/// <param name="ProviderFactory">A factory creating one isolated, disposable provider per specification run.</param>
/// <param name="CancellationToken">Host cancellation passed through creation and evaluation; opaque bodies become Unsupported.</param>
public sealed record ScreenplayMcpRun(string? Root, ISemanticBodyExecutionProviderFactory? ProviderFactory, CancellationToken CancellationToken = default);
