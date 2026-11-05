// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

internal static partial class ProjectionParser
{
    /// <summary>
    /// Parses a property mapping line for the graph projection parser.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the mapping.</param>
    /// <returns>The parsed <see cref="MappingSyntax"/>, or null for an invalid mapping.</returns>
    internal static MappingSyntax? ParseGraphMapping(ParserContext context, SourceLine line) => ParseMapping(context, line);
}
