// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_preserving_wire_paths_and_original_members : Specification
{
    ApplicationSyntax _syntax = null!;
    ImmutableArray<WorkspaceSyntaxEntry> _entries;
    JsonElement _wire;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            module Billing
              feature Accounts
                slice StateChange Register
                  command Register
                    first String
                    second String
                  event Opened
            """).Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single() with
        {
            Captures = [new CaptureSyntax("Capture", new("webhook", [new("path", "/accounts", SourceLocation.Start)], SourceLocation.Start), null, [], [], [], [], SourceLocation.Start)],
            DescriptionLocation = SourceLocation.Start,
            DescriptionRawLength = 10
        };
        _syntax = parsed with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        _wire = SyntaxJson.Serialize(_syntax);
    }

    void Because() => _entries = WorkspaceSyntaxIndex.ForSyntax(_syntax, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));

    [Fact] void should_resolve_every_occurrence_path_in_the_wire_document() => _entries.All(entry => At(entry.Handle.Path).GetProperty("kind").GetString() == entry.Kind).ShouldBeTrue();
    [Fact] void should_preserve_original_collection_indices() => _entries.Where(entry => entry.Node is PropertySyntax).Select(entry => entry.Index).SequenceEqual([0, 1]).ShouldBeTrue();
    [Fact] void should_use_the_camel_case_parent_member() => _entries.Single(entry => entry.Node is CaptureSourceSyntax).Member.ShouldEqual("source");
    [Fact] void should_not_confuse_a_structural_kind_with_the_discriminator() => At(_entries.Single(entry => entry.Node is CaptureSourceSyntax).Handle.Path).GetProperty("syntaxKind").GetString().ShouldEqual("webhook");
    [Fact] void should_exclude_source_metadata() => _entries.Any(entry => entry.Member == "location" || entry.Member == "descriptionLocation" || entry.Member == "descriptionRawLength").ShouldBeFalse();
    [Fact] void should_not_assign_addresses_to_unsupported_capture_nodes() => _entries.Where(entry => entry.Node is CaptureSyntax or CaptureSourceSyntax or CaptureSourceSettingSyntax).All(entry => entry.Address is null).ShouldBeTrue();
    [Fact] void should_keep_supported_property_addresses() => _entries.Where(entry => entry.Node is PropertySyntax).All(entry => entry.Address?.Kind == SemanticKind.Property).ShouldBeTrue();
    [Fact] void should_keep_the_document_root() => _entries[0].Handle.Path.ShouldEqual(string.Empty);
    [Fact] void should_keep_original_typed_member_order() => _entries.Where(entry => entry.Parent == _entries.Single(item => item.Node is SliceSyntax).Handle).Select(entry => entry.Member).SequenceEqual(["events", "commands", "captures"]).ShouldBeTrue();

    JsonElement At(string path)
    {
        var value = _wire;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            value = value.ValueKind == JsonValueKind.Array ? value[int.Parse(segment, CultureInfo.InvariantCulture)] : value.GetProperty(segment);
        }

        return value;
    }
}
