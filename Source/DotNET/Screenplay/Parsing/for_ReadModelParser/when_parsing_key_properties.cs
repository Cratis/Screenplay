// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser;

public class when_parsing_key_properties : Specification
{
    const string Prefix = "module M\n  feature F\n    slice StateView S\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_parse_a_composite_key()
    {
        var result = _compiler.Compile(Prefix + "      readmodel Row\n        resourceId String key\n        period String key\n        hours Int");
        result.Success.ShouldBeTrue();
        result.Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single().Properties.Where(property => property.IsKey).Select(property => property.Name).ShouldEqual("resourceId", "period");
    }

    [Theory]
    [InlineData("String optional key")]
    [InlineData("String[] key")]
    [InlineData("String generated key")]
    [InlineData("String identifier key")]
    [InlineData("String subject key")]
    [InlineData("String key optional")]
    [InlineData("String key key")]
    void should_refuse_invalid_parts(string declaration) => _compiler.Compile(Prefix + $"      readmodel Row\n        resourceId {declaration}").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKey).ShouldBeTrue();

    [Theory]
    [InlineData("command C")]
    [InlineData("event E")]
    void should_refuse_key_outside_a_read_model(string owner) => _compiler.Parse(Prefix + $"      {owner}\n        resourceId String key").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKey).ShouldBeTrue();

    [Fact]
    void should_refuse_a_nested_key() => _compiler.Parse(Prefix + "      readmodel Row\n        value String\n          nested String key").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidReadModelKey);

    [Fact]
    void should_diagnose_duplicate_key_properties_without_throwing() => _compiler.Compile(Prefix + "      readmodel Row\n        id String key\n        id String key\n      query Find => Row optional\n        by id String").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKey).ShouldBeTrue();

    [Fact]
    void should_keep_a_property_named_key() => _compiler.Compile("type lowerType\n  value String\n" + Prefix + "      readmodel Row\n        key lowerType").Success.ShouldBeTrue();

    [Fact]
    void should_refuse_a_composite_typed_part_in_a_multipart_key() => _compiler.Compile("type ObjectKey\n  value String\n" + Prefix + "      readmodel Row\n        resourceId ObjectKey key\n        period String key").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKey).ShouldBeTrue();

    [Fact]
    void should_allow_a_composite_type_as_the_single_key() => _compiler.Compile("type ObjectKey\n  value String\n" + Prefix + "      readmodel Row\n        resourceId ObjectKey key").Success.ShouldBeTrue();
}
