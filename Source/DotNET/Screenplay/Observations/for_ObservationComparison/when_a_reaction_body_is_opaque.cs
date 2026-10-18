// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_a_reaction_body_is_opaque : given.a_compiled_model
{
    void Establish()
    {
        Reaction();
        Model(ReactionSource.Replace("          produces Audited\n            name = name", "          ```csharp\n          return [];\n          ```", StringComparison.Ordinal));
    }

    void Because() => _result = Compare();

    [Fact] void should_disclose_the_opaque_body() => _result.Findings.Any(value => value.Reason == ObservationUnknownReason.OpaqueReaction).ShouldBeTrue();
    [Fact] void should_not_establish_complete_comparison() => _result.Complete.ShouldBeFalse();
}
