// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_interaction_target_is_a_behavior_parameter : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _target;

    void Establish() => _links = Create("""
        behavior Run
          parameter Retry
          on click
            execute Retry
              with attemptId from item.id
        module Projects
          feature Registration
            slice StateChange Retry
              command Retry
                attemptId String
        """);
    void Because() => _target = _links.Incoming(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).Single(link => link.Reference.Role == "mapping-target");

    [Fact] void should_not_bind_the_parameter_operand_to_a_same_named_command() => _target.Resolution.ShouldEqual("incomplete");
    [Fact] void should_refuse_completeness_until_the_attachment_target_is_known() => _links.Complete(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).ShouldBeFalse();
}
