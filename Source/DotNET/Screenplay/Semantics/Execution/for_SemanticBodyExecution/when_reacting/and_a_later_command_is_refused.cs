// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_a_later_command_is_refused : given.a_reaction_effect
{
    void Establish()
    {
        Configure(Source.Replace("produces Recorded", "validate\n          require name != \"Second\"\n        produces Recorded", StringComparison.Ordinal));
        var command = Plan.Commands.Values.Single(command => command.Name == "Record");
        Answer = SemanticReactionBodyResult.Invoked([
            new(command.Id, SemanticValue.Composite([new(command.Properties.Single().Id, SemanticValue.Text("First"))])),
            new(command.Id, SemanticValue.Composite([new(command.Properties.Single().Id, SemanticValue.Text("Second"))]))]);
    }
    void Because() => Fire();
    [Fact] void should_return_the_later_refusal() => Failure.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_keep_the_earlier_command_settled() => Loop.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("First"));
    [Fact] void should_record_both_invocations() => Loop.Invocations.Length.ShouldEqual(2);
}
