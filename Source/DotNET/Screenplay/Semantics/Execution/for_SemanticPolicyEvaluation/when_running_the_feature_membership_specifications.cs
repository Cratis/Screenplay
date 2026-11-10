// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

// The plan grants a set of features: membership and prefix tests decide, and an absent plan denies (decision 0152).
public class when_running_the_feature_membership_specifications : Specification
{
    const string Source =
        """
        policy FeatureEnabled
          reads Plan as plan by assetId
          require plan.features contains "DataQuality" and plan.tier starts with "pro"
        policy NotRestricted
          reads Plan as plan by assetId
          require not plan.features contains "Restricted"
        module Fleet
          feature Tokens
            slice StateChange Request
              event Requested
                assetId String
              command RequestFeature
                assetId String identifier
                authorize FeatureEnabled
                produces Requested
                  for assetId
                  assetId = assetId
              command RequestOpen
                assetId String identifier
                authorize NotRestricted
                produces Requested
                  for assetId
                  assetId = assetId
              specification GrantedFeatureOnAProPlan
                given readmodel Plan
                  assetId = "v1"
                  features
                    item "Reports"
                    item "DataQuality"
                  tier = "professional"
                given caller
                  authenticated
                when RequestFeature
                  assetId = "v1"
                then Requested
                  assetId = "v1"
              specification FeatureMissingFromThePlan
                given readmodel Plan
                  assetId = "v1"
                  features
                    item "Reports"
                  tier = "professional"
                given caller
                  authenticated
                when RequestFeature
                  assetId = "v1"
                then denied
              specification WrongTier
                given readmodel Plan
                  assetId = "v1"
                  features
                    item "DataQuality"
                  tier = "basic"
                given caller
                  authenticated
                when RequestFeature
                  assetId = "v1"
                then denied
              specification NoPlanDenies
                given caller
                  authenticated
                when RequestFeature
                  assetId = "v2"
                then denied
              specification NegationOnAPresentPlan
                given readmodel Plan
                  assetId = "v1"
                  features
                    item "Reports"
                  tier = "basic"
                given caller
                  authenticated
                when RequestOpen
                  assetId = "v1"
                then Requested
                  assetId = "v1"
              specification NegationWithoutAPlanStillDenies
                given caller
                  authenticated
                when RequestOpen
                  assetId = "v2"
                then denied
            slice StateView Plans
              readmodel Plan
                assetId String key
                features String[]
                tier String
              event PlanSet
                features String[]
                tier String
              projection Build => Plan
                from PlanSet
                  assetId = $eventSourceId
                  features = features
                  tier = tier
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

    [Fact] void should_select_v45() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V45);
    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(6);
    [Fact] void should_pass_every_specification() => _runs.Where(run => !run.Passed).Select(run => run.ToString()).ShouldBeEmpty();
}
