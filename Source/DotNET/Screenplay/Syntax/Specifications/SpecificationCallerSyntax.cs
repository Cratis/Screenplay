// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>Represents an explicitly authored caller fixture.</summary>
/// <param name="Authenticated">Whether the caller is authenticated.</param>
/// <param name="Roles">The caller roles.</param>
/// <param name="Claims">The repeated caller claim values.</param>
/// <param name="Location">The source location.</param>
public record SpecificationCallerSyntax(
    bool Authenticated,
    IEnumerable<string> Roles,
    IEnumerable<SpecificationCallerClaimSyntax> Claims,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>Represents one claim value in a caller fixture.</summary>
/// <param name="Type">The claim type.</param>
/// <param name="Value">The claim value.</param>
/// <param name="Location">The source location.</param>
public record SpecificationCallerClaimSyntax(string Type, string Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>Represents an explicit authorization denial assertion.</summary>
/// <param name="Location">The source location.</param>
public record SpecificationDeniedSyntax(SourceLocation Location) : SyntaxNode(Location);
