// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_optional_type_references : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("type T\n  note String?\n")]
    [InlineData("trigger T\n  note String?\n")]
    [InlineData(Prefix + "      event E\n        note String?\n")]
    [InlineData(Prefix + "      readmodel R\n        note String?\n")]
    [InlineData(Prefix + "      command C\n        description String?\n")]
    [InlineData(Prefix + "      command C\n        produces event E\n          note String? = name\n")]
    [InlineData(Prefix + "      query Q => observable R[]?\n        by id Uuid?\n        filter note String? from note\n")]
    [InlineData(Prefix + "      reaction R\n        when E\n          note String?\n")]
    void should_preserve_structure_and_report_each_committed_legacy_type_once(string source)
    {
        var legacy = _compiler.Parse(source);
        var canonical = _compiler.Parse(source.Replace("?", " optional", StringComparison.Ordinal));
        legacy.Success.ShouldBeTrue();
        canonical.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(legacy.Value!, canonical.Value!).ShouldBeTrue();
        legacy.Diagnostics.Count().ShouldEqual(source.Count(character => character == '?'));
        foreach (var diagnostic in legacy.Diagnostics)
        {
            diagnostic.Code.ShouldEqual(DiagnosticCodes.LegacyOptionalSuffix);
            diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Information);
            var line = source.Split('\n')[diagnostic.Location.Line - 1];
            line[(diagnostic.Location.Column - 1)..].Split('?')[0].ShouldNotContain(" ");
        }
    }

    [Theory]
    [InlineData("String optional optional")]
    [InlineData("String? optional")]
    [InlineData("String optional?")]
    [InlineData("String optional[]")]
    [InlineData("String?[]")]
    [InlineData("String Optional")]
    void should_reject_malformed_modifiers(string type) => _compiler.Parse("type T\n  note " + type).Success.ShouldBeFalse();

    [Fact]
    void should_explain_reversed_identifier_modifiers() =>
        _compiler.Parse(Prefix + "      command C\n        id Uuid identifier optional\n").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidOptionalModifierOrder).ShouldBeTrue();

    [Theory]
    [InlineData(" optional")]
    [InlineData("?")]
    void should_keep_event_identifier_rejection(string marker) =>
        _compiler.Parse(Prefix + "      event E\n        id Uuid" + marker + " identifier\n").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IdentifierOnEventProperty).ShouldBeTrue();

    [Fact]
    void should_keep_optional_contextual() =>
        _compiler.Compile("type optional\n  optional String\n  value optional optional\n").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_explain_a_missing_type() =>
        _compiler.Compile("type T\n  value optional\n").Diagnostics.Single().Message.ShouldContain("did you forget the type before 'optional'?");

    [Fact]
    void should_reserve_optional_reads() =>
        _compiler.Parse(Prefix + "      command C\n        reads R optional as r\n").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.OptionalReadsNotSupported);

    [Fact]
    void should_keep_observable_greedy()
    {
        var query = _compiler.Parse(Prefix + "      query Q => observable optional\n").Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
        query.IsObservable.ShouldBeTrue();
        query.ReturnType.Name.ShouldEqual("optional");
        query.ReturnType.IsOptional.ShouldBeFalse();
    }

    [Fact]
    void should_warn_about_type_shaped_tags_without_reporting_a_type_occurrence()
    {
        var result = _compiler.Parse(Prefix + "      event E\n        tag TagType optional\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.TagPropertyReadAsTag).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldBeFalse();
    }

    [Fact]
    void should_distinguish_file_properties_from_dotted_attachment_paths()
    {
        var type = _compiler.Parse("type T\n  file String optional\n  file Models/T.cs\n").Value!.Types.Single();
        type.Properties.Single().Type.IsOptional.ShouldBeTrue();
        type.File!.Path.ShouldEqual("Models/T.cs");
    }

    [Theory]
    [InlineData("observable?", false, "observable", true, false)]
    [InlineData("observable[]?", false, "observable", true, true)]
    [InlineData("observable observable?", true, "observable", true, false)]
    [InlineData("observable optional", true, "optional", false, false)]
    void should_print_ambiguous_query_types_without_changing_their_meaning(string type, bool observable, string name, bool optional, bool collection)
    {
        var original = _compiler.Parse(Prefix + "      query Q => " + type);
        var query = original.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
        query.IsObservable.ShouldEqual(observable);
        query.ReturnType.Name.ShouldEqual(name);
        query.ReturnType.IsOptional.ShouldEqual(optional);
        query.ReturnType.IsCollection.ShouldEqual(collection);
        var printed = new ScreenplayPrinter().Print(original.Value!);
        SyntaxJson.StructurallyEqual(original.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
        if (type == "observable?")
        {
            printed.ShouldContain("=> observable?");
            original.Diagnostics.Single().Message.ShouldContain("Keep 'observable?'");
        }
    }

    [Fact]
    void should_print_legacy_types_canonically()
    {
        var original = _compiler.Parse("type T\n  value String[]?\n");
        var printed = new ScreenplayPrinter().Print(original.Value!);
        printed.ShouldContain("value String[] optional");
        SyntaxJson.StructurallyEqual(original.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
    }
}
