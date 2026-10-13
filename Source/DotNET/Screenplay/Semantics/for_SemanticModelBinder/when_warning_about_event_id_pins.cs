// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_warning_about_event_id_pins : given.a_semantic_binder
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      event Renamed\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_carry_standalone_and_inline_pins_instead_of_warning(bool inline)
    {
        var source = inline ? Source.Replace("event Renamed", "command C\n        id Uuid identifier\n        produces event Renamed", StringComparison.Ordinal) : Source;
        var pinned = Bind(source + (inline ? "          " : "        ") + "id \"Original\"\n");
        pinned.Success.ShouldBeTrue();
        pinned.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V16);
        pinned.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).ShouldBeFalse();
        pinned.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax).ShouldBeFalse();
        pinned.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().StoredName.ShouldEqual("Original");
    }

    [Fact]
    void should_keep_unpinned_models_at_their_original_version() => Bind(Source).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    void should_keep_equal_name_pins_byte_identical_to_the_unpinned_model(bool inline, bool isPublic)
    {
        var source = inline ? Source.Replace("event Renamed", "command C\n        id Uuid identifier\n        produces event Renamed", StringComparison.Ordinal) : Source;
        if (isPublic) source = source.Replace("event Renamed", "public event Renamed", StringComparison.Ordinal);
        var unpinned = Bind(source);
        var pinned = Bind(source + (inline ? "          " : "        ") + "id \"Renamed\"\n");
        unpinned.Success.ShouldBeTrue();
        pinned.Success.ShouldBeTrue();
        var model = pinned.Value!.Model;
        model.SemanticVersion.ShouldEqual(unpinned.Value!.Model.SemanticVersion);
        pinned.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).ShouldBeFalse();
        model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().StoredName.ShouldBeNull();
        var bytes = SemanticModelSerializer.Serialize(model);
        bytes.SequenceEqual(SemanticModelSerializer.Serialize(unpinned.Value.Model)).ShouldBeTrue();
        SemanticModelSerializer.Deserialize(bytes).Revision.ShouldEqual(model.Revision);
    }

    [Theory]
    [InlineData("description \"Why\"")]
    [InlineData("documentation\n          ```markdown\n          Why\n          ```")]
    void should_report_only_description_and_documentation_as_metadata(string metadata)
    {
        var result = Bind(Source + "        " + metadata + "\n");
        var information = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax);
        information.Severity.ShouldEqual(DiagnosticSeverity.Information);
        information.Message.ShouldEqual("Event 'Renamed' description and documentation are authoring metadata.");
    }

    [Theory]
    [InlineData("Original")]
    [InlineData(" Original")]
    [InlineData("Original ")]
    [InlineData(@"Original\nName")]
    [InlineData("Original-Name")]
    [InlineData("1Original")]
    [InlineData("Original.Name")]
    void should_refuse_invalid_or_colliding_stored_names(string pin)
    {
        var result = Bind(Source + $"        id \"{pin}\"\n      event Original\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventId).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_an_empty_pin_before_binding()
    {
        var result = new ScreenplayCompiler().Compile(Source + "        id \"\"\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventId).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_stored_name_collision_in_another_slice()
    {
        var result = Bind(Source + "        id \"Original\"\n    slice StateChange Other\n      event Original\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventId).ShouldBeTrue();
    }

    [Fact]
    void should_carry_one_stored_name_for_every_generation()
    {
        var result = Bind(Source.Replace("event Renamed", "event Renamed generation 1", StringComparison.Ordinal) + "        id \"Original\"\n      event Renamed generation 2\n        id \"Original\"\n");
        result.Success.ShouldBeTrue();
        var model = result.Value!.Model;
        var contract = model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single();
        contract.StoredName.ShouldEqual("Original");
        contract.PriorRevisions.Length.ShouldEqual(1);
        var bytes = SemanticModelSerializer.Serialize(model);
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).SequenceEqual(bytes).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_different_pins_between_generations()
    {
        var result = Bind(Source.Replace("event Renamed", "event Renamed generation 1", StringComparison.Ordinal) + "        id \"Original\"\n      event Renamed generation 2\n        id \"Other\"\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventId).ShouldBeTrue();
    }
}
