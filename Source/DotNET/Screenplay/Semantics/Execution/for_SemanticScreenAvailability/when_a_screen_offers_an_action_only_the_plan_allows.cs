// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticScreenAvailability;

// An action is offered exactly when the caller satisfies the policy of its command, and a policy that decides from recorded state follows the plan
// of the asset the screen is showing (decision 0158). The report presents the fact; the command still decides when it runs.
public class when_a_screen_offers_an_action_only_the_plan_allows : Specification
{
    const string Source =
        """
        policy OnTheExportPlan
          reads Plan as plan by assetId
          require authenticated and plan.exports == true
        module Fleet
          feature Assets
            slice StateChange ExportAsset
              command ExportAsset
                assetId String identifier
                authorize OnTheExportPlan
                produces AssetExported
                  for assetId
              event AssetExported
              readmodel Plan
                assetId String key
                exports Bool
              event PlanSet
                exports Bool
              projection BuildPlan => Plan
                from PlanSet
                  assetId = $eventSourceId
                  exports = exports
              screen AssetScreen
                fits page
                title "Asset"
                action ExportAsset
                  label "Export"
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticId _planModel;
    ImmutableArray<SemanticElementAvailability> _onThePlan;
    ImmutableArray<SemanticElementAvailability> _offThePlan;
    ImmutableArray<SemanticElementAvailability> _noRecordedPlan;
    ImmutableArray<SemanticElementAvailability> _anonymous;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "fleet.play", Source);
        var parsed = new ScreenplayCompiler().Parse(Source);
        parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        var bound = new SemanticModelBinder().Bind("Fleet", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        var admitted = Ui.ExecutableScreens.Admit(parsed.Value!, bound.Value!, 2026);
        admitted.Refusals.ShouldBeEmpty();
        _plan = SemanticExecutionPlan.Compile(admitted.Compilation!.Model).Plan!;
        _planModel = _plan.ReadModels.Values.Single(model => model.Name == "Plan").Id;
    }

    void Because()
    {
        var signedIn = new SemanticCaller(true, [], []);
        var context = new Dictionary<string, SemanticValue> { ["assetId"] = new SemanticTextValue("a1") };
        _onThePlan = SemanticScreenAvailability.Evaluate(_plan, signedIn, context, [Plan("a1", true)]);
        _offThePlan = SemanticScreenAvailability.Evaluate(_plan, signedIn, context, [Plan("a1", false)]);
        _noRecordedPlan = SemanticScreenAvailability.Evaluate(_plan, signedIn, context);
        _anonymous = SemanticScreenAvailability.Evaluate(_plan, new SemanticCaller(false, [], []), context, [Plan("a1", true)]);
    }

    SemanticReadModelInstance Plan(string asset, bool exports)
    {
        var model = _plan.ReadModels[_planModel];
        return new(
            _planModel,
            new SemanticTextValue(asset),
            [.. model.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name == "exports" ? new SemanticBooleanValue(exports) : new SemanticTextValue(asset)))]);
    }

    [Fact] void should_offer_the_action_on_the_plan() => _onThePlan.Single().Availability.ShouldEqual(SemanticAvailability.Available);
    [Fact] void should_withhold_the_action_off_the_plan() => _offThePlan.Single().Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_withhold_the_action_when_no_plan_is_recorded() => _noRecordedPlan.Single().Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_withhold_the_action_from_an_anonymous_caller() => _anonymous.Single().Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_name_the_policy_that_gates_it() => _offThePlan.Single().Policies.ShouldContainOnly("OnTheExportPlan");
    [Fact] void should_say_why() => _offThePlan.Single().Reason.ShouldContain("OnTheExportPlan");
    [Fact] void should_report_the_screen_and_label() => (_onThePlan.Single().Screen, _onThePlan.Single().Label).ShouldEqual(("AssetScreen", "Export"));
}
