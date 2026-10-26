// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_reconstructing_frozen_baselines : Specification
{
    List<string> _differences = [];

    void Because()
    {
        for (uint major = 1; major <= 34; major++)
        {
            var input = given.documents.Frozen(major);
            using var parsed = JsonDocument.Parse(input);
            var revision = SemanticRevision.Parse(parsed.RootElement.GetProperty("revision").GetString()!);
            var output = new EsmAdmissionCanonicalJson(parsed.RootElement, major, 0).Write(projection: false, revision: revision);
            if (!input.SequenceEqual(output))
            {
                var first = Enumerable.Range(0, Math.Min(input.Length, output.Length)).First(index => input[index] != output[index]);
                _differences.Add($"{major} at {first}: {Encoding.UTF8.GetString(input.AsSpan(first, Math.Min(120, input.Length - first)))} != {Encoding.UTF8.GetString(output.AsSpan(first, Math.Min(120, output.Length - first)))}");
            }
        }
    }

    [Fact] void should_reconstruct_all_frozen_baseline_bytes_without_changing_schema_or_array_order() => _differences.ShouldBeEmpty();
}
#endif
