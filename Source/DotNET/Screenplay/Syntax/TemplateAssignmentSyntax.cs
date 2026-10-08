// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a scoped <c>template &lt;Name&gt;</c> assignment.
/// </summary>
/// <param name="Name">The assigned template name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateAssignmentSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
