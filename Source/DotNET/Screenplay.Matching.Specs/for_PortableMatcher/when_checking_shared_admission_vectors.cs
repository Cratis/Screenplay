// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_checking_shared_admission_vectors : Specification
{
    [Fact]
    void should_construct_admitted_patterns_and_throw_the_shared_reason_for_every_rejection()
    {
        using var document = MatchVectors.Read("portable-match-dialect.json");
        var root = document.RootElement;
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        cases.Length.ShouldEqual(129);
        foreach (var vector in cases)
        {
            var error = Catch.Exception(() => PortableMatcher.Compile(vector.GetProperty("pattern").GetString()!));
            if (vector.GetProperty("accepted").GetBoolean())
            {
                error.ShouldBeNull();
            }
            else
            {
                error.ShouldBeOfExactType<InvalidPortableMatchPattern>();
                var construct = vector.GetProperty("construct").GetString();
                var offset = vector.GetProperty("offset").GetInt32();
                var suggestion = vector.GetProperty("invalid").GetBoolean() ? null : root.GetProperty("suggestions").GetProperty(construct!).GetString();
                error!.Message.ShouldEqual($"{construct} at offset {offset}. {suggestion}".TrimEnd());
            }
        }
    }
}
