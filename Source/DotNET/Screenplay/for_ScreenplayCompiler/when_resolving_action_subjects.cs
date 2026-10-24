// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_resolving_action_subjects : given.a_compiler
{
    const string Source = "concept ItemId : Uuid\nconcept TenantId : Uuid\nmodule Work\n  feature Items\n    slice StateView List\n      readmodel Row\n        rowId ItemId key\n        itemId TenantId\n        ready Boolean\n      query Rows => Row[]\n      command Retry\n        itemId ItemId identifier\n      screen List\n        data Row[] via query Rows\n        action Retry";

    [Theory]
    [InlineData("", ScreenActionSubjectKind.Item)]
    [InlineData(" for item", ScreenActionSubjectKind.Item)]
    [InlineData(" for screen", ScreenActionSubjectKind.Screen)]
    void should_resolve_without_changing_authored_syntax(string clause, ScreenActionSubjectKind expected)
    {
        var raw = _compiler.Compile(Source + clause).Value!;
        var before = SyntaxJson.Serialize(raw);
        var resolved = ScreenActionSubjects.Resolve(raw);
        Action(resolved.Value!).Subject.ShouldEqual(expected);
        SyntaxJson.Serialize(raw).GetRawText().ShouldEqual(before.GetRawText());
        Action(raw).Inputs.ShouldBeEmpty();
        Action(resolved.Value!).Location.ShouldEqual(Action(raw).Location);
    }

    [Fact]
    void should_keep_same_name_precedence_over_key_type_fallback()
    {
        var source = Source.Replace("itemId TenantId", "itemId ItemId", StringComparison.Ordinal);
        var effective = ScreenActionSubjects.Resolve(_compiler.Compile(source).Value!).Value!;
        Action(effective).Inputs.Single().Binding.ShouldEqual("item.itemId");
    }

    [Fact]
    void should_bind_a_renamed_declared_key_by_nominal_concept()
    {
        var source = Source.Replace("        itemId TenantId\n", string.Empty, StringComparison.Ordinal);
        var resolved = ScreenActionSubjects.Resolve(_compiler.Compile(source).Value!);
        Action(resolved.Value!).Inputs.Single().Binding.ShouldEqual("item.rowId");
        resolved.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ChangedActionSubject);
    }

    [Fact]
    void should_keep_an_explicit_binding_before_same_name_and_key_fallback()
    {
        var effective = ScreenActionSubjects.Resolve(_compiler.Compile(Source + " for item\n          with itemId from $context.currentItem").Value!).Value!;
        Action(effective).Inputs.Single().Binding.ShouldEqual("$context.currentItem");
    }

    [Theory]
    [InlineData("ItemId identifier", "ItemId optional identifier")]
    [InlineData("ItemId identifier", "ItemId generated identifier")]
    [InlineData("ItemId identifier", "TenantId identifier")]
    void should_not_infer_item_from_ineligible_or_primitive_identifiers(string original, string replacement)
    {
        var raw = _compiler.Compile(Source.Replace(original, replacement, StringComparison.Ordinal)).Value!;
        Action(ScreenActionSubjects.Resolve(raw).Value!).Subject.ShouldBeNull();
    }

    [Fact]
    void should_not_use_primitive_equality_as_nominal_proof()
    {
        var source = Source.Replace("rowId ItemId key", "rowId Uuid key", StringComparison.Ordinal).Replace("itemId ItemId identifier", "itemId Uuid identifier", StringComparison.Ordinal);
        Action(ScreenActionSubjects.Resolve(_compiler.Compile(source).Value!).Value!).Subject.ShouldBeNull();
    }

    [Fact]
    void should_not_choose_between_multiple_eligible_keys()
    {
        var source = Source.Replace("        itemId TenantId\n", "        otherId ItemId key\n", StringComparison.Ordinal) + " for item";
        var resolved = ScreenActionSubjects.Resolve(_compiler.Compile(source).Value!);
        Action(resolved.Value!).Inputs.ShouldBeEmpty();
        resolved.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousActionInputBinding).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    [Fact]
    void should_retain_a_guard_without_selecting_from_a_screen_collection()
    {
        var source = Source.Replace("        action Retry", "        action \"Retry\" for screen\n          when item.ready == true execute Retry", StringComparison.Ordinal);
        var raw = _compiler.Compile(source).Value!;
        var resolved = ScreenActionSubjects.Resolve(raw);
        var action = Screen(resolved.Value!).Directives.OfType<ScreenGuardedActionSyntax>().Single();
        action.Alternatives.Single().Arguments.ShouldBeEmpty();
        resolved.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedActionSubjectContext).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    static ScreenSyntax Screen(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Screens.Single();
    static ScreenActionSyntax Action(ApplicationSyntax application) => Screen(application).Directives.OfType<ScreenActionSyntax>().Single();
}
