// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_public_event_projections : given.a_protected_read_model
{
    [Fact]
    void should_bind_public_event_and_cross_slice_projections_identically_under_the_preamble()
    {
        var source = Source.Replace("        reads View by id\n", string.Empty, StringComparison.Ordinal)
            .Replace("View.value", "value", StringComparison.Ordinal) + "\n" + """
                slice Translate Outbound
                  direction outbound
                  public event Published
                    value String
                  projection Publish => Published
                    from Recorded
                      value = value
            """;
        var protectedResult = Bind(source);
        var legacyResult = Bind(source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal));
        Assert.True(protectedResult.Success, string.Join('\n', protectedResult.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(legacyResult.Success, string.Join('\n', legacyResult.Diagnostics.Select(diagnostic => diagnostic.Message)));
        protectedResult.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V25);
        ProjectionJson(protectedResult.Value.Model).ShouldEqual(ProjectionJson(legacyResult.Value!.Model));
    }

    static string ProjectionJson(ExecutableSemanticModel model)
    {
        using var json = JsonDocument.Parse(SemanticModelSerializer.Serialize(model));
        var projections = json.RootElement.GetProperty("application").GetProperty("modules").EnumerateArray()
            .SelectMany(module => module.GetProperty("features").EnumerateArray())
            .SelectMany(feature => feature.GetProperty("slices").EnumerateArray())
            .SelectMany(slice => slice.GetProperty("projections").EnumerateArray()).ToArray();
        projections.Length.ShouldEqual(2);
        return string.Join('\n', projections.Select(projection => projection.GetRawText()));
    }
}
