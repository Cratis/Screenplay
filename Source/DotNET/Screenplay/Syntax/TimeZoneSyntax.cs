// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the application root <c>time zone "&lt;id&gt;"</c> or <c>time zone configurable default "&lt;id&gt;"</c> declaration.
/// </summary>
/// <param name="Id">The IANA time zone id as written.</param>
/// <param name="Configurable">Whether the zone is only the default of a deployment-configurable zone.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TimeZoneSyntax(string Id, bool Configurable, SourceLocation Location) : SyntaxNode(Location);
