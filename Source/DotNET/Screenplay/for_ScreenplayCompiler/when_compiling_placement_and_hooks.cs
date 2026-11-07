// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_placement_and_hooks : given.a_placement_document
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source());

    FeatureSyntax Products => _result.Value!.Modules.Single().Features.Single();
    IEnumerable<ToolbarButtonSyntax> Toolbar => Products.Outlets.Single(outlet => outlet.Name == "toolbar").Items.OfType<ToolbarButtonSyntax>();
    ToolbarButtonSyntax Withdraw => Products.Outlets.Single(outlet => outlet.Name == "rowActions").Items.OfType<ToolbarButtonSyntax>().Single();
    SliceSyntax Slice(string name) => Products.Slices.Single(slice => slice.Name == name);

    [Fact] void should_report_no_warnings_or_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_read_where_a_screen_fits_in_order() => Slice("SetProductPrice").Screens.Single().Fits.ShouldEqual([ScreenFit.Drawer, ScreenFit.Pane, ScreenFit.Dialog]);
    [Fact] void should_leave_a_screen_without_fits_empty() => Slice("ProductDetails").Screens.Single().Fits.ShouldBeEmpty();
    [Fact] void should_read_a_hook_that_opens_a_dialog() => Toolbar.First().Opens.ShouldEqual(HookOpening.Dialog);
    [Fact] void should_read_a_hook_that_opens_in_a_slot() => (Toolbar.Last().Opens, Toolbar.Last().OpensSlot).ShouldEqual((HookOpening.Slot, "detail"));
    [Fact] void should_read_what_a_hook_acts_on() => Toolbar.Last().Subject.ShouldEqual(ScreenActionSubjectKind.Item);
    [Fact] void should_leave_an_automatic_hook_without_opens() => Toolbar.First().OpensSlot.ShouldBeNull();
    [Fact] void should_read_a_hook_that_runs() => Withdraw.Opens.ShouldEqual(HookOpening.Runs);
    [Fact] void should_read_when_a_hook_is_enabled() => ((ComparisonConditionSyntax)Withdraw.EnabledWhen!).Left.ShouldEqual("item.status");
    [Fact] void should_read_the_confirmation() => Withdraw.Confirm.ShouldEqual("Withdraw this product?");
    [Fact] void should_let_a_screenless_slice_be_issued_by_a_runs_hook() => Slice("WithdrawProduct").Screens.ShouldBeEmpty();
    [Fact] void should_read_a_picker_field() => _result.Value!.Modules.Single().Forms!.Single().Fields.First().Choices.ShouldEqual(new FormFieldChoicesSyntax("AllCategories", "name", new SourceLocation(15, 7)));
    [Fact] void should_read_what_a_slot_shows() => Slice("ProductCatalog").Screens.Single().Directives.OfType<ScreenTemplateReferenceSyntax>().Single().Slots.Last().Directives.Single()
        .ShouldEqual(new ScreenShowSyntax("ProductDetails", "sku", "products", new SourceLocation(78, 13)));
    [Fact] void should_count_a_runs_hook_as_issuing_its_command() => ModelCompleteness.Check(_result, CompletenessChecks.None).ShouldBeEmpty();
}
