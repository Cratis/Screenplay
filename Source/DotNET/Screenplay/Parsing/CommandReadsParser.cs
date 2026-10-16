// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

internal static class CommandReadsParser
{
    static readonly string[] _keywords = ["command", "reads", "protected"];

    internal static bool IsDeclaration(string content) =>
        content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(2).SequenceEqual(_keywords.Take(2), StringComparer.Ordinal);

    internal static bool IsProtected(string content) =>
        content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).SequenceEqual(_keywords, StringComparer.Ordinal);
}
