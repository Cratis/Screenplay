// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_lowering_inline_events : given.a_semantic_binder
{
    const string Prefix = "module Projects\n  feature Naming\n    slice StateChange Rename\n";
    const string Command = "      command Rename\n        projectId Uuid identifier\n        name String\n";
    const string Inline = Prefix + Command + "        produces event Renamed\n          tag audit\n          name String = name\n";
    const string Explicit = Prefix + Command + "        produces Renamed\n          for projectId\n          name = name\n      event Renamed\n        tag audit\n        name String\n";

    [Fact]
    void should_lower_to_identical_explicit_bytes()
    {
        var inline = Bind(Inline);
        var explicitForm = Bind(Explicit);
        inline.Success.ShouldBeTrue();
        explicitForm.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(inline.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(explicitForm.Value!.Model)).ShouldBeTrue();
    }

    [Fact]
    void should_ignore_authoring_metadata_in_esm()
    {
        var withMetadata = Bind(Inline + "          id \"OldName\"\n          description \"A new name\"\n          documentation\n            ```markdown\n            More **details**.\n            ```\n");
        withMetadata.Success.ShouldBeTrue();
        withMetadata.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ReportOnlySemanticSyntax);
        SemanticModelSerializer.Serialize(withMetadata.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Bind(Inline).Value!.Model)).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_slice_identity_when_extracting_and_reordering()
    {
        const string Reordered = Prefix + "      event Renamed\n        tag audit\n        name String\n" + Command + "        produces Renamed\n          for projectId\n          name = name\n";
        SemanticModelSerializer.Serialize(Bind(Inline).Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Bind(Reordered).Value!.Model)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_an_implicit_destination_without_an_identifier() =>
        Bind(Inline.Replace("projectId Uuid identifier", "projectId Uuid", StringComparison.Ordinal)).Success.ShouldBeFalse();

    [Fact]
    void should_keep_cross_source_execution_fail_closed() =>
        Bind(Inline.Replace("        name String\n", "        name String\n        otherId Uuid\n", StringComparison.Ordinal).Replace("          tag audit", "          for otherId\n          tag audit", StringComparison.Ordinal)).Success.ShouldBeFalse();

    [Fact]
    void should_not_bind_an_inline_default_onto_a_plain_omission()
    {
        var result = Bind(Inline + "        produces Legacy\n          name = name\n      event Legacy\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.ExplicitProducesTargetsRequired && value.Message.Contains("Legacy", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_lower_an_inline_event_with_an_explicit_plain_sibling_to_identical_bytes()
    {
        const string Sibling = "        produces Legacy\n          for projectId\n          name = name\n      event Legacy\n        name String\n";
        var inline = Bind(Inline + Sibling);
        var explicitForm = Bind(Explicit.Replace("      event Renamed", Sibling + "      event Renamed", StringComparison.Ordinal));
        inline.Success.ShouldBeTrue();
        explicitForm.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(inline.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(explicitForm.Value!.Model)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_plain_omission_distinct_from_inline_default()
    {
        var legacy = Explicit.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal);
        var result = Bind(legacy);
        result.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(result.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Bind(Inline).Value!.Model)).ShouldBeFalse();
    }
}
