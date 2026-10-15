// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_reading_shared_dialect_vectors
{
    [Fact]
    void should_admit_or_refuse_every_shared_case_with_the_same_construct_offset_and_suggestion()
    {
        using var document = PortableMatchVectors.Read("portable-match-dialect.json");
        var cases = document.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        cases.Length.ShouldBeGreaterThan(80);
        foreach (var vector in cases)
        {
            var result = SemanticMatchPattern.Parse(vector.GetProperty("pattern").GetString()!);
            result.Accepted.ShouldEqual(vector.GetProperty("accepted").GetBoolean());
            if (result.Accepted)
            {
                result.JavaScriptPattern.ShouldEqual(vector.GetProperty("pattern").GetString());
                SemanticMatchPattern.Create(result).GetGroupNames().Length.ShouldEqual(1);
                continue;
            }
            result.Invalid.ShouldEqual(vector.GetProperty("invalid").GetBoolean());
            result.Construct.ShouldEqual(vector.GetProperty("construct").GetString());
            result.Offset.ShouldEqual(vector.GetProperty("offset").GetInt32());
            if (!result.Invalid) result.Suggestion.ShouldEqual(document.RootElement.GetProperty("suggestions").GetProperty(result.Construct!).GetString());
        }
    }

    [Fact]
    void should_hold_every_suggestion_to_the_shared_table()
    {
        using var document = PortableMatchVectors.Read("portable-match-dialect.json");
        foreach (var suggestion in document.RootElement.GetProperty("suggestions").EnumerateObject()) PortableMatchSuggestions.For(suggestion.Name).ShouldEqual(suggestion.Value.GetString());
    }
}
