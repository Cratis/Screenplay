// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class FiniteGenerationSamples
{
    internal static IEnumerable<string> Create(GenerationPattern pattern, DateOnly? date = null)
    {
        if (pattern.HasDates && date is null) yield break;
        for (var choice = 0; choice < 3; choice++)
        {
            var body = new StringBuilder();
            var position = 0;
            foreach (var token in pattern.Tokens)
            {
                var repetitions = choice == 0 ? token.Minimum : token.Maximum;
                for (var repetition = 0; repetition < repetitions; repetition++)
                {
                    foreach (var characters in token.Expand(date))
                    {
                        body.Append(choice == 0 || (choice == 2 && position % 2 == 0) ? characters[0] : characters[^1]);
                        position++;
                    }
                }
            }
            var payload = body.ToString();
            var suffix = pattern.Checksum is null ? string.Empty : GenerationChecksum.Calculate(payload, pattern.Checksum);
            if (suffix is not null) yield return payload + suffix;
        }
    }
}
