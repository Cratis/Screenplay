// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticScreenAvailability;

// An element bound to a command or query is offered only when the caller may use it and the part is available: suspension first, then the gates
// down module, feature, slice and construct, and an explicit gate on the element can only narrow that (decision 0169).
public class when_a_screen_element_is_bound_to_a_part_that_is_not_available : Specification
{
    const string Source =
        """
        audience Plus
        audience Incident
        module Fleet
          feature Assets
            slice StateChange ExportAsset
              available for Plus
              command ExportAsset
                assetId String identifier
                produces AssetExported
                  for assetId
              event AssetExported
              screen AssetScreen
                fits page
                title "Asset"
                action ExportAsset
                  label "Export"
                  available for not Incident
            slice StateView Assets
              available for Plus
              readmodel Asset
                assetId String key
              event AssetRegistered
              projection BuildAssets => Asset
                from AssetRegistered
                  assetId = $eventSourceId
              query AllAssets => Asset[]
              screen AssetList
                fits page
                title "Assets"
                data Asset[] via query AllAssets
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticId _exportSlice;
    ImmutableArray<SemanticElementAvailability> _everything;
    ImmutableArray<SemanticElementAvailability> _inOffer;
    ImmutableArray<SemanticElementAvailability> _notInOffer;
    ImmutableArray<SemanticElementAvailability> _gatedByTheElement;
    ImmutableArray<SemanticElementAvailability> _suspended;
    ImmutableArray<SemanticElementAvailability> _suspendedAbove;

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
        _exportSlice = _plan.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "ExportAsset").Id;
    }

    void Because()
    {
        var caller = new SemanticCaller(true, [], []);
        var context = new Dictionary<string, SemanticValue> { ["assetId"] = new SemanticTextValue("a1") };
        var module = _plan.Model.Application.Modules.Single().Id;
        _everything = SemanticScreenAvailability.Evaluate(_plan, caller, context);
        _inOffer = SemanticScreenAvailability.Evaluate(_plan, caller, context, availability: new([.. new[] { "Plus" }]));
        _notInOffer = SemanticScreenAvailability.Evaluate(_plan, caller, context, availability: new([]));
        _gatedByTheElement = SemanticScreenAvailability.Evaluate(_plan, caller, context, availability: new([.. new[] { "Plus", "Incident" }]));
        _suspended = SemanticScreenAvailability.Evaluate(_plan, caller, context, availability: new([.. new[] { "Plus" }]) { Suspended = [_exportSlice] });
        _suspendedAbove = SemanticScreenAvailability.Evaluate(_plan, caller, context, availability: new([.. new[] { "Plus" }]) { Suspended = [module] });
    }

    [Fact] void should_report_everything_available_without_a_state() => _everything.All(element => element.Availability == SemanticAvailability.Available).ShouldBeTrue();
    [Fact] void should_offer_the_button_when_the_slice_is_in_the_offer() => Action(_inOffer).Availability.ShouldEqual(SemanticAvailability.Available);
    [Fact] void should_offer_the_table_when_the_slice_is_in_the_offer() => Data(_inOffer).Availability.ShouldEqual(SemanticAvailability.Available);
    [Fact] void should_hide_the_button_when_the_slice_is_not_in_the_offer() => Action(_notInOffer).Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_say_the_part_is_not_available() => Action(_notInOffer).Cause.ShouldEqual(SemanticUnavailableCause.NotAvailable);
    [Fact] void should_hide_the_table_when_the_slice_is_not_in_the_offer() => Data(_notInOffer).Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_narrow_the_part_with_the_gate_of_the_element() => Action(_gatedByTheElement).Availability.ShouldEqual(SemanticAvailability.Unavailable);
    [Fact] void should_not_let_the_gate_of_the_element_touch_the_data() => Data(_gatedByTheElement).Availability.ShouldEqual(SemanticAvailability.Available);
    [Fact] void should_hide_the_button_of_a_suspended_slice() => Action(_suspended).Cause.ShouldEqual(SemanticUnavailableCause.Suspended);
    [Fact] void should_keep_the_other_slice_offered_when_one_is_suspended() => Data(_suspended).Availability.ShouldEqual(SemanticAvailability.Available);
    [Fact] void should_hide_everything_beneath_a_suspended_module() => _suspendedAbove.All(element => element.Cause == SemanticUnavailableCause.Suspended).ShouldBeTrue();

    static SemanticElementAvailability Action(ImmutableArray<SemanticElementAvailability> elements) => elements.Single(element => element.Kind == SemanticElementKind.Action);

    static SemanticElementAvailability Data(ImmutableArray<SemanticElementAvailability> elements) => elements.Single(element => element.Kind == SemanticElementKind.Data);
}
