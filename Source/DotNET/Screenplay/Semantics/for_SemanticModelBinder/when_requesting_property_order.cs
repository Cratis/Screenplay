// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order : Specification
{
    SemanticCompilation _baseline;
    SemanticCompilation _ordered;
    SemanticReadModel _readModel;

    void Because()
    {
        _baseline = property_order_binding.BindDefault().Value!;
        var result = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true));
        result.Success.ShouldBeTrue();
        _ordered = result.Compilation.Value!;
        _readModel = _ordered.Model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
    }

    [Fact] void should_parse_the_fixture_without_errors() => new ScreenplayCompiler().Parse(property_order_binding.Source).Success.ShouldBeTrue();
    [Fact] void should_preserve_the_composite_key() => _readModel.Properties.Count(property => property.IsIdentifier).ShouldEqual(2);
    [Fact] void should_preserve_the_existing_default_version() => _baseline.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_select_the_minor_only_for_the_explicit_request() => _ordered.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34_1);
    [Fact] void should_capture_declared_property_identities() => _readModel.PropertyOrder.ShouldEqual(_readModel.Properties.Select(property => property.Id));
    [Fact] void should_preserve_names_in_order() => _readModel.Properties.Select(property => property.Name).ShouldEqual(["tenantId", "entryId", "zebra", "alpha"]);
    [Fact]
    void should_keep_the_source_map()
    {
        SemanticSourceMapEntry[] expected = [.. _baseline.SourceMap.Entries];
        _ordered.SourceMap.Entries.ShouldEqual(expected);
    }

    [Fact] void should_detect_changed_source_map_origin() => AssertSourceMapMismatch([.. _baseline.SourceMap.Entries.SetItem(0, _baseline.SourceMap.Entries[0] with { Origin = SemanticIdentityOrigin.Persisted })]);
    [Fact] void should_detect_changed_source_map_role() => AssertSourceMapMismatch([.. _baseline.SourceMap.Entries.SetItem(0, _baseline.SourceMap.Entries[0] with { Role = SemanticSourceMapRole.Description })]);
    [Fact] void should_detect_changed_source_map_order() => AssertSourceMapMismatch([.. _baseline.SourceMap.Entries.Reverse()]);
    [Fact] void should_detect_a_missing_source_map_entry() => AssertSourceMapMismatch([.. _baseline.SourceMap.Entries.Skip(1)]);

    [Fact]
    void should_detect_changed_source_map_span()
    {
        SemanticSourceMapEntry[] expected = [.. _baseline.SourceMap.Entries];
        var span = expected[0].Span;
        expected[0] = expected[0] with { Span = SemanticSourceSpan.Create(span.Document, span.Start + 1, span.Length, span.StartLine, span.StartColumn + 1, span.EndLine, span.EndColumn + 1) };
        AssertSourceMapMismatch(expected);
    }

    [Fact] void should_keep_specification_origins() => _ordered.SpecificationOrigins.Keys.ShouldEqual(_baseline.SpecificationOrigins.Keys);

    [Fact]
    void should_leave_the_disabled_options_byte_identical()
    {
        var disabled = property_order_binding.Bind(new()).Compilation.Value!;
        SemanticModelSerializer.Serialize(disabled.Model).ShouldEqual(SemanticModelSerializer.Serialize(_baseline.Model));
    }

    void AssertSourceMapMismatch(SemanticSourceMapEntry[] expected) =>
        Catch.Exception(() => _ordered.SourceMap.Entries.ShouldEqual(expected)).ShouldNotBeNull();
}
