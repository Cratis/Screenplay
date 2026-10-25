// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

internal static class SemanticLossJsonMembers
{
    public static IEnumerable<string> Problems(JsonElement element, string path) => element.ValueKind switch
    {
        JsonValueKind.Object => ObjectProblems(element, path),
        JsonValueKind.Array => element.EnumerateArray().SelectMany((item, index) => Problems(item, $"{path}[{index}]")),
        _ => [],
    };

    static IEnumerable<string> ObjectProblems(JsonElement element, string path) =>
        element.EnumerateObject().GroupBy(member => member.Name, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .Select(group => $"Duplicate field '{group.Key}' in {path}.")
            .Concat(element.EnumerateObject().SelectMany(member => Problems(member.Value, $"{path}/{member.Name}")));
}
