// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_an_invoked_command_is_denied : given.a_reaction_effect
{
    const string Invocations = "          invokes RecordFirst\n            name = \"First\"\n          invokes Record\n            name = \"Denied\"\n          invokes RecordFirst\n            name = \"Last\"";

    [Theory]
    [InlineData("runs as system role \"Recorder\"")]
    [InlineData("")]
    [InlineData("runs as system")]
    void should_stop_like_invokes_without_on_refused_and_not_roll_back_an_earlier_settled_command(string identity)
    {
        var source = SecuredSource(identity);
        Configure(source);
        var first = Plan.Commands.Values.Single(command => command.Name == "RecordFirst");
        var denied = Plan.Commands.Values.Single(command => command.Name == "Record");
        Answer = SemanticReactionBodyResult.Invoked([
            new(first.Id, SemanticValue.Composite([new(first.Properties.Single().Id, SemanticValue.Text("First"))])),
            new(denied.Id, SemanticValue.Composite([new(denied.Properties.Single().Id, SemanticValue.Text("Denied"))])),
            new(first.Id, SemanticValue.Composite([new(first.Properties.Single().Id, SemanticValue.Text("Last"))]))]);

        Fire();
        Failure.ShouldBeOfExactType<SemanticRejected>();
        var bodyFailure = (SemanticRejected)Failure!;
        var bodyLoop = Loop;
        Context.RunsAs.ShouldEqual(Reaction.RunsAs);
        Configure(source.Replace("          ```csharp\n          return effects;\n          ```", Invocations, StringComparison.Ordinal));
        Fire();

        Failure.ShouldBeOfExactType<SemanticRejected>();
        var invokesFailure = (SemanticRejected)Failure!;
        bodyFailure.Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        bodyFailure.Category.ShouldEqual(invokesFailure.Category);
        bodyFailure.Details.ShouldEqual(invokesFailure.Details);
        bodyLoop.Invocations.Select(invocation => invocation.Accepted).ShouldEqual([true, false]);
        bodyLoop.Invocations.Select(invocation => invocation.Accepted).ShouldEqual(Loop.Invocations.Select(invocation => invocation.Accepted));
        bodyLoop.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual([SemanticValue.Text("First")]);
        bodyFailure.World.Facts.ShouldEqual(bodyLoop.World.Facts);
        bodyLoop.World.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual([SemanticValue.Text("First")]);
        bodyLoop.World.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual(Loop.World.Facts.Select(fact => fact.Values.Single().Value));
    }

    static string SecuredSource(string identity) =>
        "policy Restricted\n  require role \"Administrator\"\n" + Source
            .Replace("      command Record\n        name String", "      command Record\n        name String\n        authorize Restricted", StringComparison.Ordinal)
            .Replace("      reaction OnRequested", "      command RecordFirst\n        name String\n        produces Recorded\n          for name\n          name = name\n      reaction OnRequested", StringComparison.Ordinal)
            .Replace("runs as system role \"Recorder\"", identity, StringComparison.Ordinal);
}
