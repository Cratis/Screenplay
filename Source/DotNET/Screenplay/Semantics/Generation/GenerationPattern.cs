// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed record GenerationPattern(ImmutableArray<GenerationPatternToken> Tokens, string? Checksum)
{
    internal const int LengthLimit = 256;
    internal int ChecksumWidth => Checksum is null ? 0 : GenerationChecksum.Width(Checksum);
    internal int MinimumLength => Tokens.Sum(token => token.Width * token.Minimum) + ChecksumWidth;
    internal int MaximumLength => Tokens.Sum(token => token.Width * token.Maximum) + ChecksumWidth;
    internal bool HasDates => Tokens.Any(token => token.Segment is not null);

    internal static GenerationPattern Parse(string pattern, string? checksum = null)
    {
        var result = new GenerationPattern([.. new GenerationPatternParser(pattern).Parse()], checksum);
        if (result.MaximumLength > LengthLimit) throw new InvalidGenerationPattern("final expanded length exceeds 256 characters including check digits", pattern.Length);

        return result;
    }
}
