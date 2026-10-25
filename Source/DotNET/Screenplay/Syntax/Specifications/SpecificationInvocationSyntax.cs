// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents <c>then invokes &lt;Command&gt;</c> - an invocation the specification's slice's reactions are expected to make,
/// with the values it is expected to pass.
/// </summary>
/// <param name="Command">The name of the invoked command.</param>
/// <param name="Values">The <see cref="PropertyMappingSyntax">property values</see> the invocation is expected to pass; unstated properties are not compared.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationInvocationSyntax(string Command, IEnumerable<PropertyMappingSyntax> Values, SourceLocation Location) : SyntaxNode(Location);
