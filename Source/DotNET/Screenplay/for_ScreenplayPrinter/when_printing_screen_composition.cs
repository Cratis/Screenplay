// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_screen_composition : given.a_printer
{
    const string Source =
        """
        exposure for Shell
          property "shell:header".title label "Header title"
          property "shell:header".actions label "Header actions" operations add, reorder fields label, icon

        exposure for Pane
          property "shell:header".title reexposes Shell

        instance Browse
          set "shell:header".title = "Invoices"
          items "shell:header".actions
            item "export:csv"
              label = "Export"
              icon = null

        module Sales
          screen template Shell
            outlet detail
            display "Sales shell"
            description "A list beside its details"
            scopes slice
            header contributes Actions
            list
            detail
            navigation contributes Navigation

            content header
              component scene.web.Header shellHeader
                id "shell:header"
                property title = "Sales"

            arrangement flow
              column gap 8
                header height 56
                grid gap 16 columns 2 rows 1 grow 1.5 span 1
                  list grow 2 span 1
                  detail grow

          screen template Pane
            scopes none
            body

          feature Invoices
            slice StateView Browse
              command Register
                invoiceId String

              screen Browse
                template Shell
                navigate to Browse
                  outlet detail
                contribute to Actions order 10
                  action Register
                contribute to Navigation
                  section filters
                    title "Filters"
        """;

    RoundTripResult _roundtrip;

    ApplicationSyntax Reparsed => _roundtrip.Reparsed.Value!;
    ScreenTemplateSyntax Shell => Reparsed.Modules.Single().ScreenTemplates.First();
    ScreenSyntax Screen => Reparsed.Modules.Single().Features.Single().Slices.Single().Screens.Single();
    ArrangementContainerSyntax Grid => (ArrangementContainerSyntax)((ArrangementContainerSyntax)((ArrangementContainerSyntax)Shell.Arrangement!.Root!).Children.Single()).Children.Last();

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_serialize_the_same_syntax_after_printing() => SyntaxJson.Serialize(Reparsed).GetRawText().ShouldEqual(SyntaxJson.Serialize(_roundtrip.Original!.Value!).GetRawText());
    [Fact] void should_keep_exact_exposed_component_ids() => Reparsed.Exposures.First().Properties.First().Component.ShouldEqual("shell:header");
    [Fact] void should_keep_collection_operations() => Reparsed.Exposures.First().Properties.Last().Operations.ShouldEqual(["add", "reorder"]);
    [Fact] void should_keep_editable_fields() => Reparsed.Exposures.First().Properties.Last().EditableFields.ShouldEqual(["label", "icon"]);
    [Fact] void should_keep_re_exposures() => Reparsed.Exposures.Last().Properties.Single().ReExposes.ShouldEqual("Shell");
    [Fact] void should_keep_typed_instance_values() => ((LiteralExpressionSyntax)Reparsed.InstanceContributions.Single().Contributions.First().Value!).Value.ShouldEqual("Invoices");
    [Fact] void should_keep_contributed_items() => Reparsed.InstanceContributions.Single().Contributions.Last().Items.Single().Id.ShouldEqual("export:csv");
    [Fact] void should_keep_null_item_values() => ((LiteralExpressionSyntax)Reparsed.InstanceContributions.Single().Contributions.Last().Items.Single().Values.Last().Value).Value.ShouldBeNull();
    [Fact] void should_keep_template_content() => Shell.Content.Single().Directives.Single().ShouldBeOfExactType<ScreenComponentSyntax>();
    [Fact] void should_keep_template_display_name() => Shell.DisplayName.ShouldEqual("Sales shell");
    [Fact] void should_keep_template_scopes() => Shell.Scopes.ShouldEqual(["slice"]);
    [Fact] void should_keep_an_empty_scope_restriction() => Reparsed.Modules.Single().ScreenTemplates.Last().RestrictsScopes.ShouldBeTrue();
    [Fact] void should_keep_grid_columns() => Grid.Columns.ShouldEqual(2);
    [Fact] void should_keep_grid_rows() => Grid.Rows.ShouldEqual(1);
    [Fact] void should_keep_container_grow() => Grid.Grow.ShouldEqual(1.5);
    [Fact] void should_keep_slot_grow_weight() => ((ArrangementSlotSyntax)Grid.Children.First()).GrowFactor.ShouldEqual(2d);
    [Fact] void should_keep_screen_contributions() => Screen.Contributions.Select(_ => _.ContributionPoint).ShouldEqual(["Actions", "Navigation"]);
    [Fact] void should_keep_contribution_order() => Screen.Contributions.First().Order.ShouldEqual(10);
    [Fact] void should_keep_navigation_outlets() => Screen.Directives.OfType<ScreenNavigateSyntax>().Single().Outlet.ShouldEqual("detail");
}
