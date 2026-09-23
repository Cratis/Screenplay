// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_asserting_localized_validation_messages : Specification
{
    const string Source =
        """
        concept ProjectName : String
          validate
            not empty message $strings.projects.nameRequired
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                name ProjectName
                amount Int
                validate
                  amount > 0 message $strings.projects.amountRequired
                  require amount < 10
                    message "$strings.projects.amountTooLarge"
              specification InvalidAmount
                when RegisterProject
                  name = "Alpha"
                  amount = 0
                then error "$strings.projects.amountRequired"
              specification InvalidName
                when RegisterProject
                  name = ""
                  amount = 1
                then error "$strings.projects.nameRequired"
              specification FailedRequirement
                when RegisterProject
                  name = "Alpha"
                  amount = 10
                then error "$strings.projects.amountTooLarge"
        """;

    SemanticSpecificationRun[] _runs;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string key = "localized-rules";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _runs = [.. plan.Specifications.Values.Select(specification => new SemanticSpecificationRunner().Run(plan, specification.Id))];
    }

    [Fact] void should_pass_each_key_assertion() => _runs.All(run => run.Passed).ShouldBeTrue();
    [Fact] void should_report_three_rejections() => _runs.Length.ShouldEqual(3);
    [Fact] void should_mark_every_key_as_symbolic() => _runs.All(run => ((SemanticRejected)run.Execution).MessageIsStringKey).ShouldBeTrue();
    [Fact] void should_keep_the_keys_verbatim() => _runs.Select(run => ((SemanticRejected)run.Execution).Details).Order(StringComparer.Ordinal).SequenceEqual(
        ["$strings.projects.amountRequired", "$strings.projects.amountTooLarge", "$strings.projects.nameRequired"]).ShouldBeTrue();
}
