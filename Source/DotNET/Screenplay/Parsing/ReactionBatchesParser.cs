// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

internal static class ReactionBatchesParser
{
    static readonly string[] _keywords = ["reaction", "batches", "atomic"];

    internal static bool IsAtomic(string content) =>
        content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).SequenceEqual(_keywords, StringComparer.Ordinal);
}
