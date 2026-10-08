// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class CommandDestinationTypes
{
    internal static TypeRefSyntax? DestinationType(CommandSyntax command, ProducesSyntax produced, ProducesSyntax[] productions, ConsistencyDeclarations declarations)
    {
        if (produced.For is PathExpressionSyntax path) return PathType(command, path.Path, declarations);
        if (produced.For is not null) return null;
        var identifier = command.Properties.FirstOrDefault(property => property.IsIdentifier)?.Name;
        if (productions.Any(sibling => sibling.For is not null && (sibling.For is not PathExpressionSyntax destination || destination.Path != identifier)) ||
            (productions.Any(sibling => sibling.InlineEvent is not null && sibling.For is null) && productions.Any(sibling => sibling.InlineEvent is null && sibling.For is null)))
        {
            return null;
        }
        if (produced.InlineEvent is null)
        {
            return productions.Any(sibling => sibling.For is not null || sibling.InlineEvent is not null) ? null
                : command.Properties.FirstOrDefault(property => property.IsGenerated && property.IsIdentifier)?.Type ?? new("Uuid", false, false, produced.Location);
        }

        return command.Properties.Where(property => property.IsIdentifier && !property.Type.IsOptional && !property.Type.IsCollection).ToArray() is [var property] ? property.Type : null;
    }

    internal static TypeRefSyntax? PathType(CommandSyntax command, string path, ConsistencyDeclarations declarations)
    {
        var type = declarations.Property(command.Properties, path, out _)?.Type;
        if (type is null) return null;
        var segments = path.Split('.');
        for (var depth = 1; depth < segments.Length; depth++)
        {
            if (declarations.Property(command.Properties, string.Join('.', segments.Take(depth)), out _) is { } parent)
            {
                type = type with { IsCollection = type.IsCollection || parent.Type.IsCollection, IsOptional = type.IsOptional || parent.Type.IsOptional };
            }
        }

        return type;
    }
}
