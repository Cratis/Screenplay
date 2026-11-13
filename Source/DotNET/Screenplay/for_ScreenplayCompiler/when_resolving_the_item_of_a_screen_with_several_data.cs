// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_resolving_the_item_of_a_screen_with_several_data : given.a_compiler
{
    const string Head = "concept ItemId : Uuid\nmodule Work\n  feature Items\n    slice StateView List\n      readmodel Header\n        title String\n      readmodel Row\n        rowId ItemId key\n      readmodel Other\n        otherId ItemId key\n      query HeaderOf => Header\n      query Rows => Row[]\n      query Others => Other[]\n      command Retry\n        itemId ItemId identifier\n      command Tag\n        tags ItemId[]\n      screen List\n";

    [Fact]
    void should_bind_the_plain_action_to_the_collection_a_table_shows()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Retry for item");
        var action = Screen(resolved.Value!).Directives.OfType<ScreenActionSyntax>().Single();
        action.Subject.ShouldEqual(ScreenActionSubjectKind.Item);
        action.Inputs.Single().Binding.ShouldEqual("item.rowId");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_bind_the_plain_action_regardless_of_data_order()
    {
        var resolved = Resolve("        data Row[] via query Rows\n        data Header via query HeaderOf\n        table Row\n        action Retry for item");
        Screen(resolved.Value!).Directives.OfType<ScreenActionSyntax>().Single().Inputs.Single().Binding.ShouldEqual("item.rowId");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_bind_the_toolbar_action_to_the_collection_a_table_shows()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        toolbar\n          item retry action Retry for item");
        var item = Screen(resolved.Value!).Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single();
        item.Parameters.Single().Binding.Path.ShouldEqual("item.rowId");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_use_a_table_in_a_section_for_an_action_in_the_same_section()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        section Rows\n          table Row\n          action Retry for item");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_name_the_candidates_when_two_tables_show_two_collections()
    {
        var resolved = Resolve("        data Row[] via query Rows\n        data Other[] via query Others\n        table Row\n        table Other\n        action Retry for item");
        var diagnostic = resolved.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
        diagnostic.Message.ShouldContain("Row[], Other[]");
        diagnostic.Message.ShouldContain("table over exactly one collection");
        Screen(resolved.Value!).Directives.OfType<ScreenActionSyntax>().Single().Inputs.ShouldBeEmpty();
    }

    [Fact]
    void should_stay_unresolved_when_two_data_have_no_table()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        action Retry for item");
        resolved.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject).ShouldEqual(1);
        Screen(resolved.Value!).Directives.OfType<ScreenActionSyntax>().Single().Inputs.ShouldBeEmpty();
    }

    [Fact]
    void should_not_use_a_table_over_a_record_to_narrow()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Header\n        action Retry for item");
        resolved.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_keep_a_single_data_unchanged()
    {
        var resolved = Resolve("        data Row[] via query Rows\n        action Retry for item");
        Screen(resolved.Value!).Directives.OfType<ScreenActionSyntax>().Single().Inputs.Single().Binding.ShouldEqual("item.rowId");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_not_narrow_a_guarded_action_by_a_table()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action \"Retry\" for item\n          when item.rowId == item.rowId execute Retry");
        resolved.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_report_an_unknown_item_field_after_narrowing_to_the_table_collection()
    {
        Codes("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Retry for item\n          with itemId from item.missing", DiagnosticCodes.UnknownActionSubjectField).Length.ShouldEqual(1);
        Codes("        data Row[] via query Rows\n        action Retry for item\n          with itemId from item.missing", DiagnosticCodes.UnknownActionSubjectField).Length.ShouldEqual(1);
    }

    [Fact]
    void should_report_a_collection_cardinality_mismatch_after_narrowing_like_a_single_data()
    {
        var narrowed = Codes("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        action Tag for item\n          with tags from item.rowId", DiagnosticCodes.UnknownActionArgumentProperty);
        var single = Codes("        data Row[] via query Rows\n        action Tag for item\n          with tags from item.rowId", DiagnosticCodes.UnknownActionArgumentProperty);
        single.Length.ShouldEqual(1);
        string.Join('|', narrowed.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Join('|', single.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_bind_a_screen_level_table_for_an_action_in_a_section_without_tables()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        table Row\n        section Tools\n          action Retry for item");
        resolved.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject);
    }

    [Fact]
    void should_not_use_a_table_in_a_sibling_section_for_an_action_at_screen_level()
    {
        var resolved = Resolve("        data Header via query HeaderOf\n        data Row[] via query Rows\n        section A\n          table Row\n        action Retry for item");
        resolved.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject).ShouldEqual(1);
    }

    Diagnostic[] Codes(string screen, string code) => [.. _compiler.Compile(Head + screen).Diagnostics.Where(diagnostic => diagnostic.Code == code)];

    CompilationResult<ApplicationSyntax> Resolve(string screen) => ScreenActionSubjects.Resolve(_compiler.Compile(Head + screen).Value!);

    static ScreenSyntax Screen(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Screens.Single();
}
