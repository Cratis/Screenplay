// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>theme &lt;Name&gt;</c> block declaring a named visual theme and the component
/// packages it is meaningful against.
/// </summary>
/// <param name="Name">The theme's name.</param>
/// <param name="CompatibleWith">The component packages this theme is declared compatible with.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A theme is only meaningful relative to a specific set of component packages - an arbitrary
/// theme/package pairing can silently produce unstyled or broken components. A <see cref="UiProfileSyntax"/>
/// selecting a theme not declared compatible with one of its own <see cref="UiProfileSyntax.Packages"/> is a
/// compile-time warning, not a hard error - the pairing might still work by coincidence, but the gap is made
/// visible the same way an ambiguous or unknown name already is.
/// </remarks>
public record ThemeSyntax(string Name, IEnumerable<string> CompatibleWith, SourceLocation Location) : SyntaxNode(Location);
