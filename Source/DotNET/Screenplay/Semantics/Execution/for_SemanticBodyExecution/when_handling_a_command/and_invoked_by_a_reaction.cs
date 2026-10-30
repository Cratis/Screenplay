// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_invoked_by_a_reaction : given.a_command_handler
{
    SemanticSpecificationRun _run = null!;

    void Establish()
    {
        const string reaction = """

              event Requested
                name String
              reaction OnRequested
                when Requested
                  name
                  invokes Place
                    name = name
              specification RequestedOrder
                when append Requested
                  name = "First"
                then Placed
                  for "First"
                  name = "Handled"
                then Recorded
                  for "First"
                  name = "Handled"
        """;
        Configure(Source.Replace("slice StateChange Place", "slice Automation Place", StringComparison.Ordinal)
            .Replace("        label String optional\n", string.Empty, StringComparison.Ordinal)
            .Replace("      event Placed", reaction + "\n      event Placed", StringComparison.Ordinal));
    }

    void Because() => _run = new SemanticSpecificationRunner(new SemanticEvaluator(Provider, Bodies)).Run(Plan, Plan.Specifications.Values.Single().Id);

    [Fact] void should_satisfy_the_specification() => _run.Failures.ShouldBeEmpty();
    [Fact] void should_execute_the_invoked_handler_once() => Provider.HandleCalls.ShouldEqual(1);
    [Fact] void should_keep_the_invoked_command_values() => Contexts.Single().Command.Properties.Single().Value.ShouldEqual(SemanticValue.Text("First"));
    [Fact] void should_leave_caller_owned_provider_disposal_to_the_host() => Provider.Disposals.ShouldEqual(0);
}
