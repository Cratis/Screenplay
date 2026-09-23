// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_caused_by_property;

public class and_it_is_a_member_of_the_identity : given.a_projection_reading_who_caused_the_event
{
    List<string> _diagnosed;

    void Because() => _diagnosed =
    [
        .. new[] { "subject", "name", "userName", "onBehalfOf", "onBehalfOf.subject", "onBehalfOf.onBehalfOf.userName" }
            .Where(property => _compiler.CompileProjection(Projection(property)).Diagnostics.Any())
    ];

    [Fact] void should_accept_every_one_without_a_diagnostic() => _diagnosed.ShouldBeEmpty();
}
