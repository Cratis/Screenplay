// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The exception that is thrown when typed source authoring is structurally invalid.
/// </summary>
/// <param name="message">The rejection reason.</param>
/// <param name="document">The offending document identity, when known.</param>
/// <param name="path">The offending portable document path, when known.</param>
public sealed class InvalidWorkspaceAuthoring(string message, DocumentId? document, PortablePlayPath? path) : Exception(message)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidWorkspaceAuthoring"/> class.
    /// </summary>
    /// <param name="message">The rejection reason.</param>
    public InvalidWorkspaceAuthoring(string message) : this(message, null, null)
    {
    }

    /// <summary>
    /// Gets the offending document identity, when known.
    /// </summary>
    public DocumentId? Document { get; } = document;

    /// <summary>
    /// Gets the offending portable document path, when known.
    /// </summary>
    public PortablePlayPath? Path { get; } = path;
}
