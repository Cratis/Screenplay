// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class when_proving_timeline_content : given.a_timeline
{
    [Fact]
    void should_keep_strict_byte_equality_when_both_models_are_executable()
    {
        var before = Model(false, false);
        var after = Model(true, false);
        WorkspaceRepairVerification.SameModel(before, after).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, Model(true, false, "String"), []).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_one_sided_model_availability(bool reverse)
    {
        var executable = Model(false, false);
        var unbound = Model(true, true);
        executable.Compilation.Value.ShouldNotBeNull();
        unbound.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineContentProof.Preserves(reverse ? unbound : executable, reverse ? executable : unbound, []).ShouldBeFalse();
    }

    [Fact]
    void should_use_a_separate_source_proof_when_neither_model_exists()
    {
        var before = Model(false, true);
        var after = Model(true, true);
        WorkspaceRepairVerification.SameModel(before, after).ShouldBeFalse();
        WorkspaceTimelineContentProof.SameSourceModuloTimelinePermutation(before, after, []).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_non_permutation_change_even_when_both_models_are_absent()
    {
        var before = Model(false, true);
        var after = Model(true, true, "String");
        before.Compilation.Value.ShouldBeNull();
        after.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineContentProof.SameSourceModuloTimelinePermutation(before, after, []).ShouldBeFalse();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_order_in_every_other_collection()
    {
        var source = "module M\n  feature F\n" + Indent(Slice("View", [], "E", "X").Replace(" optional", "[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal), 4) + Indent(Slice("Write", ["E", "X"]), 4);
        var before = Create(("root.play", source));

        // Reorder the declaration collection directly, avoiding assumptions about canonical formatting.
        var start = source.IndexOf("      event E", StringComparison.Ordinal);
        var changed = source[..start] + Indent("event X\n  id Uuid\nevent E\n  id Uuid\n", 6);
        WorkspaceTimelineContentProof.Preserves(before, Create(("root.play", changed)), []).ShouldBeFalse();
    }

    [Fact]
    void should_compare_admission_diagnostics_as_a_code_and_severity_multiset()
    {
        var before = Model(false, true);
        var after = Model(true, true);
        var extra = after.Documents.Single().Text + "import External.Event\n";
        WorkspaceTimelineContentProof.Preserves(before, Create(("root.play", extra)), []).ShouldBeFalse();
    }

    [Fact]
    void should_retain_admission_diagnostic_counts_and_severities_but_not_messages_or_locations()
    {
        var error = Diagnostic.Error(DiagnosticCodes.UnsupportedSemanticSyntax, "first reason", SourceLocation.Start);
        var moved = error with { Message = "another reason", Location = new(20, 3) };
        var timeline = new Diagnostic(DiagnosticSeverity.Information, DiagnosticCodes.EventFromLaterSlice, "timeline", SourceLocation.Start);
        WorkspaceTimelineContentProof.SameAdmissionDiagnostics([error, error, timeline], [moved, moved]).ShouldBeTrue();
        WorkspaceTimelineContentProof.SameAdmissionDiagnostics([error, error], [moved]).ShouldBeFalse();
        WorkspaceTimelineContentProof.SameAdmissionDiagnostics([error], [moved with { Severity = DiagnosticSeverity.Warning }]).ShouldBeFalse();
    }

    [Fact]
    void should_exclude_only_approved_pins_at_an_existing_placement()
    {
        var consumer = Slice("View", [], "E").Replace(" optional", "[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var before = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\"\n"),
            ("a.play", consumer),
            ("z.play", Slice("Write", ["E"])));
        var after = Create(
            ("root.play", "module M\n  feature F\n    import \"z.play\"\n    import \"*.play\"\n"),
            ("a.play", consumer),
            ("z.play", Slice("Write", ["E"])));
        var pin = new TimelineImportPin("root.play", new PlayPlacement(["M", "F"]), new FileImportSyntax("z.play", SourceLocation.Start));
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeFalse();
        WorkspaceTimelineContentProof.Preserves(before, after, [pin]).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, [pin with { Placement = PlayPlacement.Document }]).ShouldBeFalse();
        WorkspaceTimelineContentProof.Preserves(before, after, [pin with { Import = new("missing.play", SourceLocation.Start) }]).ShouldBeFalse();
    }

    [Fact]
    void should_not_normalize_literal_data_that_looks_like_syntax_json()
    {
        const string source = """
            type Payload
              kind String
              modules String[]
            module M
              feature F
                slice StateChange Write
                  command Write
                    id Uuid identifier
                    data Payload
                    produces E
                      for id
                      data = data
                  event E
                    data Payload
                  specification Example
                    when Write
                      id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      data = {"kind":"ApplicationSyntax","modules":["b","a"]}
                    then E
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      data = {"kind":"ApplicationSyntax","modules":["b","a"]}
                slice StateView View
                  readmodel ViewRead
                    id Uuid
                  query All => observable ViewRead[]
                  projection ViewProjection => ViewRead
                    from E
                      id = $eventSourceId
            """;
        var before = Create(("root.play", source));
        var after = Create(("root.play", source.Replace("[\"b\",\"a\"]", "[\"a\",\"b\"]", StringComparison.Ordinal)));
        before.Compilation.Value.ShouldBeNull();
        after.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineRepairs.Timeline(before).SourceValid.ShouldBeTrue();
        WorkspaceTimelineRepairs.Timeline(after).SourceValid.ShouldBeTrue();
        WorkspaceTimelineContentProof.SameSourceModuloTimelinePermutation(before, after, []).ShouldBeFalse();
    }

    static ScreenplayWorkspace Model(bool reversed, bool unbound, string type = "Uuid")
    {
        var consumer = "  feature Consumer\n" + Indent(Slice("View", [], "E"), 4);
        if (unbound) consumer = consumer.Replace("ViewView optional", "ViewView[]", StringComparison.Ordinal).Replace("        by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var producer = "  feature Producer\n" + Indent(Slice("Write", ["E"]), 4);
        var source = ("module M\n" + (reversed ? producer + consumer : consumer + producer)).Replace("id Uuid", "id " + type, StringComparison.Ordinal);

        return Create(("root.play", source));
    }
}
