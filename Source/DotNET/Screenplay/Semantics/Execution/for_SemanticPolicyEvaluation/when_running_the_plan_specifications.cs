// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

// The plan-per-asset scenario of decision 0151: a specification gives the asset's plan and a caller, and says what the policy decides.
public class when_running_the_plan_specifications : Specification
{
    const string Source =
        """
        policy DataQualityEnabled
          reads AssetPlan as plan by assetId
          require authenticated and plan.dataQuality == true
        module Fleet
          feature Tokens
            slice StateChange RequestToken
              event TokenRequested
                assetId String
              command RequestScopedToken
                assetId String identifier
                authorize DataQualityEnabled
                produces TokenRequested
                  for assetId
                  assetId = assetId
              specification AssetOnThePlanIssuesAToken
                given readmodel AssetPlan
                  assetId = "vessel-1"
                  dataQuality = true
                given caller
                  authenticated
                when RequestScopedToken
                  assetId = "vessel-1"
                then TokenRequested
                  assetId = "vessel-1"
              specification AssetOffThePlanIsDenied
                given readmodel AssetPlan
                  assetId = "vessel-1"
                  dataQuality = false
                given caller
                  authenticated
                when RequestScopedToken
                  assetId = "vessel-1"
                then denied
              specification AssetWithoutAPlanIsDenied
                given caller
                  authenticated
                when RequestScopedToken
                  assetId = "vessel-2"
                then denied
            slice StateView Plans
              readmodel AssetPlan
                assetId String key
                dataQuality Bool
              event PlanSet
                dataQuality Bool
              projection Build => AssetPlan
                from PlanSet
                  assetId = $eventSourceId
                  dataQuality = dataQuality
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun[] _runs;

    void Establish()
    {
        const string StableKey = "fleet-plans";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "fleet.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _runs = [.. _plan.Specifications.Values.Select(specification => runner.Run(_plan, specification.Id))];
    }

    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(3);
    [Fact] void should_pass_every_specification() => _runs.All(run => run.Passed).ShouldBeTrue();
    [Fact] void should_deny_twice() => _runs.Count(run => run.Execution is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized }).ShouldEqual(2);
    [Fact] void should_accept_once() => _runs.Count(run => run.Execution is SemanticAccepted).ShouldEqual(1);
}
