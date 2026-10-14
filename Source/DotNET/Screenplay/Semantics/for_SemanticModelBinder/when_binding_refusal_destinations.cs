// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_refusal_destinations : given.a_semantic_binder
{
    static string Source(string trigger, string destination) =>
        $$"""
        trigger Requested
        module M
          feature F
            slice Automation Work
              event Refused
                message String
              command Refuse
              reaction Observer
                {{trigger}}
                  invokes Refuse
                    on refused
                      produces Refused
        {{destination}}
                        message = $refusal.message
        """;

    [Theory]
    [InlineData("every 1 minute")]
    [InlineData("when Requested")]
    [InlineData("when Startup")]
    void should_require_for_on_non_event_trigger_branches(string trigger) => Bind(Source(trigger, string.Empty)).Success.ShouldBeFalse();

    [Theory]
    [InlineData("every 1 minute")]
    [InlineData("when Requested")]
    [InlineData("when Startup")]
    void should_bind_an_explicit_destination_on_non_event_trigger_branches(string trigger) => Bind(Source(trigger, "                for \"refusals\"")).Success.ShouldBeTrue();
}
