// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_declaring_inline_events : given.a_compiler
{
    const string Prefix = "module Projects\n  feature Naming\n    slice StateChange Rename\n";
    const string Command = Prefix + "      command Rename\n        projectId Uuid identifier\n        otherId Uuid\n        name String\n";
    const string Inline = Command + "        produces event Renamed\n";

    [Fact]
    void should_keep_the_inline_authoring_structure()
    {
        var result = _compiler.Compile(Inline + "          tag audit\n          name String = name\n          @sequence String = name\n");
        result.Diagnostics.ShouldBeEmpty();
        var slice = result.Value!.Modules.Single().Features.Single().Slices.Single();
        slice.Events.ShouldBeEmpty();
        var production = slice.Commands.Single().Produces.Single();
        production.InlineEvent!.Properties.Select(property => property.Name).ShouldEqual("name", "sequence");
        production.InlineEvent.Tags!.Count().ShouldEqual(1);
        production.Tags.ShouldBeEmpty();
        production.For.ShouldBeNull();
        EventDeclarations.In(slice).Single().Name.ShouldEqual("Renamed");
    }

    [Fact]
    void should_resolve_plain_references_before_the_inline_declaration() =>
        _compiler.Compile(Command + "        produces Renamed\n          for projectId\n          name = name\n        produces event Renamed\n          name String = name\n").Diagnostics.ShouldBeEmpty();

    [Theory]
    [InlineData("          generation 2\n", DiagnosticCodes.InlineEventGeneration)]
    [InlineData("          origin Elsewhere\n", DiagnosticCodes.ReservedProductionMetadata)]
    [InlineData("          id \"\"\n", DiagnosticCodes.InvalidEventId)]
    [InlineData("          id\n", DiagnosticCodes.InvalidEventId)]
    [InlineData("          id \"Old\"\n          id \"Older\"\n", DiagnosticCodes.InvalidEventId)]
    [InlineData("          documentation \"not fenced\"\n", DiagnosticCodes.InvalidEventDocumentation)]
    [InlineData("          documentation\n            ```text\n            Not Markdown\n            ```\n", DiagnosticCodes.InvalidEventDocumentation)]
    [InlineData("          documentation\n            ```markdown\n            ```\n", DiagnosticCodes.InvalidEventDocumentation)]
    [InlineData("          name = name\n", DiagnosticCodes.InvalidPropertyMapping)]
    [InlineData("          for projectId\n          for projectId\n", DiagnosticCodes.DuplicateProducesTarget)]
    void should_reject_invalid_inline_bodies(string body, string code) =>
        _compiler.Compile(Inline + body).Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();

    [Theory]
    [InlineData("namespace")]
    [InlineData("sequence")]
    [InlineData("correlation")]
    [InlineData("causation")]
    [InlineData("causedBy")]
    [InlineData("occurred")]
    void should_reserve_system_metadata_in_both_production_forms(string directive)
    {
        foreach (var header in new[] { Inline, Command + "        produces Renamed\n" })
        {
            _compiler.Compile(header + $"          {directive} = name\n").Diagnostics
                .Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReservedProductionMetadata && diagnostic.Message.Contains("system-assigned", StringComparison.Ordinal)).ShouldBeTrue();
        }
    }

    [Fact]
    void should_reject_generation_in_the_inline_header() =>
        _compiler.Compile(Command + "        produces event Renamed generation 2\n").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InlineEventGeneration).ShouldBeTrue();

    [Fact]
    void should_reject_inline_events_in_reactions() =>
        _compiler.Compile(Prefix + "      reaction React\n        every 1 day\n          produces event Renamed\n").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InlineEventOutsideCommand).ShouldBeTrue();

    [Theory]
    [InlineData("      event Renamed\n")]
    [InlineData("      command Again\n        produces event Renamed\n")]
    void should_reject_collisions(string declaration) =>
        _compiler.Compile(Inline + declaration).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InlineEventCollision).ShouldBeTrue();

    [Fact]
    void should_reject_import_collisions() =>
        _compiler.Compile("import Other.Renamed\n" + Inline).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InlineEventCollision).ShouldBeTrue();

    [Fact]
    void should_require_all_destinations_when_one_targets_another_source() =>
        _compiler.Compile(Inline + "        produces event Other\n          for otherId\n").Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ExplicitProducesTargetsRequired).Location.Line.ShouldEqual(8);

    [Fact]
    void should_accept_all_explicit_destinations_at_the_syntax_level() =>
        _compiler.Compile(Inline + "          for projectId\n        produces event Other\n          for otherId\n").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_report_a_redundant_pin_as_information() =>
        _compiler.Compile(Inline + "          id \"Renamed\"\n").Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Information);

    [Fact]
    void should_warn_when_an_inline_payload_copies_its_source_identifier() =>
        _compiler.Compile(Inline + "          projectId Uuid = projectId\n").Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload).Severity.ShouldEqual(DiagnosticSeverity.Warning);

    [Fact]
    void should_inform_when_a_plain_payload_copies_its_explicit_source_identifier() =>
        _compiler.Compile(Command + "        produces Renamed\n          for projectId\n          projectId = projectId\n      event Renamed\n        projectId Uuid\n").Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload).Severity.ShouldEqual(DiagnosticSeverity.Information);

    [Fact]
    void should_retain_the_plain_omission_advice_without_warning_about_its_payload() =>
        _compiler.Compile(Command + "        produces Renamed\n          projectId = projectId\n      event Renamed\n        projectId Uuid\n").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.OmittedProductionDestination);

    [Fact]
    void should_reject_implicit_inline_and_plain_destinations_without_retargeting_the_plain_event()
    {
        var result = _compiler.Compile(Inline + "        produces Legacy\n      event Legacy\n");
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.ExplicitProducesTargetsRequired && value.Location.Line == 9);
        diagnostic.Message.ShouldContain("Legacy");
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_accept_an_explicit_identifier_plain_sibling() =>
        _compiler.Compile(Inline + "        produces Legacy\n          for projectId\n      event Legacy\n").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_reject_duplicate_typed_mapping_properties() =>
        _compiler.Compile(Inline + "          name String = name\n          name Uuid = otherId\n").Diagnostics
            .Single(value => value.Code == DiagnosticCodes.DuplicateDeclaration).Location.Line.ShouldEqual(10);

    [Theory]
    [InlineData("      event Described\n")]
    [InlineData("      command Described\n        projectId Uuid identifier\n        produces event Described\n")]
    void should_accept_markdown_descriptions_on_events(string declaration)
    {
        var indent = declaration.Contains("produces", StringComparison.Ordinal) ? "          " : "        ";
        _compiler.Compile(Prefix + declaration + $"{indent}description\n{indent}  ```markdown\n{indent}  **Details**\n{indent}  ```\n").Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("module Projects\n", "  ")]
    [InlineData(Prefix, "      ")]
    [InlineData(Command, "        ")]
    void should_not_widen_markdown_descriptions_to_other_declarations(string declaration, string indent) =>
        _compiler.Compile(declaration + $"{indent}description\n{indent}  ```markdown\n{indent}  **Details**\n{indent}  ```\n").Diagnostics
            .Any(value => value.Code == DiagnosticCodes.ExpectedCodeFence).ShouldBeTrue();

    [Fact]
    void should_keep_property_shaped_standalone_metadata_names() =>
        _compiler.Compile(Prefix + "      event Renamed\n        id String\n        description String\n        documentation String\n").Diagnostics.ShouldBeEmpty();
}
