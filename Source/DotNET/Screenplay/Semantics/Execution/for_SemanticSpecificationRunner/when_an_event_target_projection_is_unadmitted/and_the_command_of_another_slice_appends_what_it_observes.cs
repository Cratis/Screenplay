// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_an_event_target_projection_is_unadmitted;

// The publication is the Translate slice's behavior: the command's specification never reaches it.
public class and_the_command_of_another_slice_appends_what_it_observes : given.a_model
{
    void Because() => _result = Run("Recording");

    [Fact] void should_pass() => Assert.True(_result.Passed, string.Join('\n', _result.Failures));
    [Fact] void should_append_only_the_commands_fact() => ((SemanticAccepted)_result.Execution).Facts.Length.ShouldEqual(1);
}
