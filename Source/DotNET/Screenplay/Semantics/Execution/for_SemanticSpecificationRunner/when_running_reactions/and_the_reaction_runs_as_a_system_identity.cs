// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_the_reaction_runs_as_a_system_identity : given.a_v6_scenario
{
    [Theory]
    [InlineData("role \"Automation\"", "runs as system role \"Automation\"", true)]
    [InlineData("role \"Automation\"", "", false)]
    [InlineData("claim \"actor\" matches \"system\"", "runs as system", false)]
    [InlineData("not claim \"actor\" matches \"system\"", "runs as system", false)]
    [InlineData("role \"Automation\" or claim \"actor\" matches \"system\"", "runs as system role \"Automation\"", true)]
    [InlineData("authenticated", "runs as system", true)]
    void should_run_the_full_pipeline_with_only_the_declared_identity(string condition, string identity, bool accepted)
    {
        Compile(Source(condition, identity, accepted));
        var run = Run("Invoking");
        run.Passed.ShouldBeTrue();
        run.Execution.Kind.ShouldEqual(accepted ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
        if (!accepted) ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Theory]
    [InlineData("module", true)]
    [InlineData("module", false)]
    [InlineData("feature", true)]
    [InlineData("feature", false)]
    void should_enforce_inherited_authorization(string scope, bool accepted)
    {
        var source = Source("role \"Automation\"", accepted ? "runs as system role \"Automation\"" : "runs as system", accepted)
            .Replace("        authorize Access\n", "", StringComparison.Ordinal);
        source = scope == "module"
            ? source.Replace("module M\n", "module M\n  authorize Access\n", StringComparison.Ordinal)
            : source.Replace("  feature F\n", "  feature F\n    authorize Access\n", StringComparison.Ordinal);
        Compile(source);
        Run("Invoking").Passed.ShouldBeTrue();
    }

    static string Source(string condition, string identity, bool accepted) =>
        $"""
        policy Access
          require {condition}
        module M
          feature F
            slice Automation Work
              command Complete
                item String identifier
                authorize Access
                produces Completed
                  for item
              event Started
                item String
              event Completed
              reaction Worker
                {identity}
                when Started
                  item
                  invokes Complete
                    item = item
              specification Invoking
                given caller
                  authenticated
                  role "Automation"
                  claim "actor" = "system"
                when append Started
                  item = "one"
                {(accepted ? "then Completed" : "then denied")}
        """;
}
