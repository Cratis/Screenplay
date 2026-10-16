// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The application shell vector: sidebar and topbar regions at application level and in three modules (inherit, extend,
/// override), hierarchical items with target and slot, toolbar buttons invoking State Change slices, blueprint template
/// references and a sample data pack the ui profile selects. Every rule it exercises has a negative vector beside it.
/// </summary>
public class when_compiling_the_screen_shell_vectors : Specification
{
    CanonicalScreenCorpusVector _corpus;
    CanonicalTypedScreenSourceCase _case;
    CompilationResult<ApplicationSyntax> _shell;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;

    void Because()
    {
        _corpus = ScreenCompositionCorpus.V1;
        _case = _corpus.TypedSourceCases.Single(sourceCase => sourceCase.Name == "screen-shell-navigation-positive");
        _shell = new ScreenplayCompiler().Compile(_case.Document.Text);
        _printed = new ScreenplayPrinter().Print(_shell.Value!);
        _reprinted = new ScreenplayCompiler().Compile(_printed);
    }

    [Fact] void should_compile_without_any_diagnostic() => _shell.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_be_pending() => _case.PendingReason.ShouldBeNull();
    [Fact] void should_declare_the_application_regions_without_a_mode() => Layout.Navigation.Select(region => (region.Region, region.Mode)).ShouldEqual((NavigationRegionKind.Topbar, NavigationRegionMode.Unspecified), (NavigationRegionKind.Sidebar, NavigationRegionMode.Unspecified));
    [Fact] void should_declare_the_logo_on_the_application_topbar() => Layout.Navigation.First().Logo!.Title.ShouldEqual("Acme");
    [Fact] void should_nest_hierarchical_sidebar_items() => Layout.Navigation.Last().Items.First().Children.Select(item => item.Id).ShouldEqual("orders", "returns");
    [Fact] void should_target_a_nested_feature_with_a_slot() => Layout.Navigation.Last().Items.First().Children.Last().Slot.ShouldEqual("side");
    [Fact] void should_extend_the_sidebar_in_the_first_module() => Module("Sales").Navigation.Single(region => region.Region == NavigationRegionKind.Sidebar).Mode.ShouldEqual(NavigationRegionMode.Extend);
    [Fact] void should_inherit_the_topbar_explicitly_in_the_first_module() => Module("Sales").Navigation.Single(region => region.Region == NavigationRegionKind.Topbar).Mode.ShouldEqual(NavigationRegionMode.Inherit);
    [Fact] void should_override_the_sidebar_in_the_second_module() => Module("Support").Navigation.Single(region => region.Region == NavigationRegionKind.Sidebar).Mode.ShouldEqual(NavigationRegionMode.Override);
    [Fact] void should_inherit_by_default_in_a_module_without_navigation() => Module("Reports").Navigation.ShouldBeEmpty();
    [Fact] void should_carry_toolbar_buttons_that_name_state_change_slices() => Buttons.Select(button => (button.Id, button.Slice)).ShouldEqual(("place", "Sales.Orders.PlaceOrder"), ("cancel", "CancelOrder"));
    [Fact] void should_carry_button_emphasis_and_icon() => (Buttons.First().Emphasis, Buttons.First().Icon).ShouldEqual((ToolbarEmphasis.Primary, "plus"));
    [Fact] void should_carry_a_module_template_reference_to_a_blueprint() => (Module("Sales").Templates.Single().Blueprint, Module("Sales").Templates.Single().Name, Module("Sales").Templates.Single().Version).ShouldEqual(("Crm", "Workspace", "^1.4"));
    [Fact] void should_carry_a_screen_template_reference_to_a_blueprint() => (ScreenReference.Blueprint, ScreenReference.Name, ScreenReference.Version).ShouldEqual(("Crm", "CustomerDetail", "~1.4.2"));
    [Fact] void should_select_the_active_pack_once_in_the_ui_profile() => _shell.Value!.UiProfiles!.Single().SampleData!.Active!.Name.ShouldEqual("NordicDemo");
    [Fact] void should_map_a_shape_to_a_read_model() => _shell.Value!.UiProfiles!.Single().SampleData!.Mappings.Single().ReadModel.ShouldEqual("CustomerRow");
    [Fact] void should_carry_the_pack_rows_and_locale() => (_shell.Value!.SampleDataPacks.Single().Locale, _shell.Value!.SampleDataPacks.Single().Sets.Single().Rows.Count()).ShouldEqual(("nb-NO", 2));
    [Fact] void should_print_canonically_and_compile_the_print_without_diagnostics() => _reprinted.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_the_second_pass() => new ScreenplayPrinter().Print(_reprinted.Value!).ShouldEqual(_printed);

    [Fact]
    void should_carry_every_expected_syntax_kind()
    {
        var collector = new KindCollector();
        collector.VisitApplication(_shell.Value!);
        var kinds = collector.Kinds;
        foreach (var kind in _case.ExpectedSyntaxKinds)
        {
            kinds.ShouldContain(kind);
        }
    }

    LayoutSyntax Layout => _shell.Value!.Layouts.Single();

    ModuleSyntax Module(string name) => _shell.Value!.Modules.Single(module => module.Name == name);

    IEnumerable<ToolbarButtonSyntax> Buttons => _shell.Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens)
        .SelectMany(screen => screen.Directives.OfType<ScreenToolbarSyntax>()).SelectMany(toolbar => toolbar.Buttons);

    ScreenTemplateReferenceSyntax ScreenReference => _shell.Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens)
        .SelectMany(screen => screen.Directives.OfType<ScreenTemplateReferenceSyntax>()).Single();

    sealed class KindCollector : ScreenplaySyntaxWalker
    {
        public HashSet<string> Kinds { get; } = [];

        public override void VisitNode(SyntaxNode node) => Kinds.Add(node.GetType().Name);
    }
}
