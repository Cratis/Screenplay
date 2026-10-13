// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_warning_about_event_id_pins : given.a_semantic_binder
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      event Renamed\n";
    const string Message = "Event 'Renamed' id pin 'Original' is not yet carried to renderers; generated code uses the new name 'Renamed'.";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_warn_for_standalone_and_inline_pins_without_changing_executable_bytes(bool inline)
    {
        var source = inline ? Source.Replace("event Renamed", "command C\n        id Uuid identifier\n        produces event Renamed", StringComparison.Ordinal) : Source;
        var plain = Bind(source);
        var pinned = Bind(source + (inline ? "          " : "        ") + "id \"Original\"\n");
        pinned.Success.ShouldBeTrue();
        var warning = pinned.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers);
        warning.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        warning.Message.ShouldEqual(Message);
        warning.Location.Line.ShouldEqual(inline ? 6 : 4);
        pinned.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax).ShouldBeFalse();
        SemanticModelSerializer.Serialize(pinned.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(plain.Value!.Model));
    }

    [Fact]
    void should_not_warn_for_an_event_without_a_pin() =>
        Bind(Source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).ShouldBeFalse();

    [Theory]
    [InlineData("description \"Why\"")]
    [InlineData("documentation\n          ```markdown\n          Why\n          ```")]
    void should_keep_metadata_only_information_unchanged(string metadata)
    {
        var result = Bind(Source + "        " + metadata + "\n");
        var information = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax);
        information.Severity.ShouldEqual(DiagnosticSeverity.Information);
        information.Message.ShouldEqual("Event 'Renamed' description, documentation and id are authoring metadata.");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).ShouldBeFalse();
    }

    [Fact]
    void should_separate_pins_from_description_information()
    {
        var result = Bind(Source + "        description \"Why\"\n        id \"Original\"\n");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).Message.ShouldEqual(Message);
        var information = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax);
        information.Severity.ShouldEqual(DiagnosticSeverity.Information);
        information.Message.ShouldEqual("Event 'Renamed' description and documentation are authoring metadata.");
    }

    [Fact]
    void should_not_warn_for_source_and_stream_pins()
    {
        var result = Bind("eventsource Account\n  id \"StoredAccount\"\n  stream History\n    id \"StoredHistory\"\n" + Source);
        result.Success.ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventIdPinNotCarriedToRenderers).ShouldBeFalse();
    }
}
