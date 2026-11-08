// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>sink &lt;Name&gt; for readmodels|graphs</c> declaration - a logical place read models or graphs are
/// stored, which a deployment maps to real storage.
/// </summary>
/// <remarks>
/// A sink is realization metadata: it never enters the executable semantic model. Every application has an implicit
/// default sink for read models and one for graphs, so a model only declares the named sinks it chooses between.
/// </remarks>
/// <param name="Name">The logical name of the sink, unique application-wide ignoring case.</param>
/// <param name="Stores">The <see cref="SinkKind"/> of thing the sink stores - read models or graphs.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SinkSyntax(
    string Name,
    SinkKind Stores,
    SourceLocation Location) : SyntaxNode(Location);
