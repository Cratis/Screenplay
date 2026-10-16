// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticQueryArguments
{
    internal static ImmutableArray<SemanticProperty> KeyProperties(SemanticKeyedQuery query) =>
        query.Argument is { } argument
            ? [new(argument.Id, argument.Name, argument.Type, false)]
            : [.. query.KeyParts.Select(part => new SemanticProperty(part.Id, part.Name, part.Type, false))];
}
