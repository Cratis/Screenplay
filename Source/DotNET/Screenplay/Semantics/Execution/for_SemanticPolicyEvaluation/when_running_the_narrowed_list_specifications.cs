// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

// A fleet list returns only the vessels whose own plan includes the feature; denied rows are dropped, not refused (decision 0154).
public class when_running_the_narrowed_list_specifications : Specification
{
    const string Source =
        """
        policy OnTheFeaturePlan
          reads Plan as plan by vesselId
          require authenticated and plan.dataQuality == true
        module Fleet
          feature Vessels
            slice StateView VesselList
              readmodel Vessel
                vesselId String key
                name String
              readmodel Plan
                vesselId String key
                dataQuality Bool
              event VesselNamed
                name String
              event PlanSet
                dataQuality Bool
              projection BuildVessel => Vessel
                from VesselNamed
                  vesselId = $eventSourceId
                  name = name
              projection BuildPlan => Plan
                from PlanSet
                  vesselId = $eventSourceId
                  dataQuality = dataQuality
              query AllVessels => Vessel[]
                narrow OnTheFeaturePlan
              specification OnlyVesselsOnThePlanAreListed
                given readmodel Vessel
                  vesselId = "v1"
                  name = "Alpha"
                given readmodel Vessel
                  vesselId = "v2"
                  name = "Beta"
                given readmodel Vessel
                  vesselId = "v3"
                  name = "Gamma"
                given readmodel Plan
                  vesselId = "v1"
                  dataQuality = true
                given readmodel Plan
                  vesselId = "v2"
                  dataQuality = false
                given caller
                  authenticated
                then query AllVessels
                  result
                    vesselId = "v1"
                    name = "Alpha"
              specification NoPlansMeansAnEmptyList
                given readmodel Vessel
                  vesselId = "v1"
                  name = "Alpha"
                given caller
                  authenticated
                then query AllVessels
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun[] _runs;
    SemanticCompilation _compilation;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "fleet.play", Source);
        var result = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        _compilation = result.Value!;
        _plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
    }

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _runs = [.. _plan.Specifications.Values.Select(specification => runner.Run(_plan, specification.Id))];
    }

    [Fact] void should_select_v47() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V47);
    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(2);
    [Fact] void should_pass_every_specification() => _runs.Where(run => !run.Passed).SelectMany(run => run.Failures).ShouldBeEmpty();
}
