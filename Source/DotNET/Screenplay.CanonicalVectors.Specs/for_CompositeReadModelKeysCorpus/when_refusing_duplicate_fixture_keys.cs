// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_duplicate_fixture_keys : given.a_flat_key_model
{
    [Fact]
    void should_not_silently_replace_a_fixture_with_the_same_reordered_key()
    {
        var result = Compile(Specification("""
            given readmodel Row
              first = "a"
              second = "b"
              quantity = 1
            given readmodel Row
              second = "b"
              first = "a"
              quantity = 2
            when query All
            then no result
            """));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("duplicat", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }
}
