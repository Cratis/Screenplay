// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

// The policy names its own input; two commands spell their key differently and map it (decision 0153).
public class when_running_the_mapped_input_specifications : Specification
{
    const string Source =
        """
        policy PlanGrants
          asset String
          reads Plan as plan by asset
          require plan.enabled == true
        module Fleet
          feature Tokens
            slice StateChange Request
              event Requested
                subject String
              command RequestByVessel
                vesselId String identifier
                authorize PlanGrants
                  asset = vesselId
                produces Requested
                  for vesselId
                  subject = vesselId
              command RequestBySite
                siteId String identifier
                authorize PlanGrants
                  asset = siteId
                produces Requested
                  for siteId
                  subject = siteId
              specification VesselOnAnEnabledPlan
                given readmodel Plan
                  assetId = "v1"
                  enabled = true
                given caller
                  authenticated
                when RequestByVessel
                  vesselId = "v1"
                then Requested
                  subject = "v1"
              specification SiteOnAnEnabledPlan
                given readmodel Plan
                  assetId = "s1"
                  enabled = true
                given caller
                  authenticated
                when RequestBySite
                  siteId = "s1"
                then Requested
                  subject = "s1"
              specification SiteWithoutAPlanIsDenied
                given readmodel Plan
                  assetId = "v1"
                  enabled = true
                given caller
                  authenticated
                when RequestBySite
                  siteId = "v1x"
                then denied
            slice StateView Plans
              readmodel Plan
                assetId String key
                enabled Bool
              event PlanSet
                enabled Bool
              projection Build => Plan
                from PlanSet
                  assetId = $eventSourceId
                  enabled = enabled
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun[] _runs;
    SemanticCompilation _compilation;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "fleet.play", Source);
        var result = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        _compilation = result.Value!;
        _plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
    }

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _runs = [.. _plan.Specifications.Values.Select(specification => runner.Run(_plan, specification.Id))];
    }

    [Fact] void should_select_v46() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V46);
    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(3);
    [Fact] void should_pass_every_specification() => _runs.Where(run => !run.Passed).Select(run => run.ToString()).ShouldBeEmpty();
}
