// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_a_plain_implicit_identifier : given.a_reducer_identity_model
{
    [Fact]
    void should_not_compare_a_fixture_uuid_fallback_with_the_query_domain()
    {
        var source = Model.Replace("\n          for resource", string.Empty, StringComparison.Ordinal)
            .Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(source).Success.ShouldBeTrue();
        Warnings(source).ShouldBeEmpty();
    }
}
