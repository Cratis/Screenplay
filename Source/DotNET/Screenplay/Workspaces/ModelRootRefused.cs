// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The exception that is thrown when a model root, its metadata or its persisted identities cannot be read or admitted.
/// </summary>
/// <param name="message">The complete display-ready reason.</param>
/// <param name="failureKind">The stable discriminator reported by <see cref="ScreenplayModelRoot"/>.</param>
/// <param name="innerException">The underlying failure, if any.</param>
[SuppressMessage("Design", "CA1064", Justification = "Thrown only inside model-root opening; ScreenplayModelRoot reports it as a typed FailureKind, never across the public API.")]
sealed class ModelRootRefused(string message, string failureKind = ModelRootRefused.UnreadableModelRoot, Exception? innerException = null) : Exception(message, innerException)
{
    internal const string UnreadableModelRoot = "UnreadableModelRoot";

    /// <summary>
    /// Gets the stable discriminator reported by <see cref="ScreenplayModelRoot"/>.
    /// </summary>
    internal string FailureKind { get; } = failureKind;

    /// <summary>
    /// Gets the discriminator the MCP server has always reported for this refusal.
    /// </summary>
    internal string McpFailureKind { get; init; } = "RequestFailed";
}
