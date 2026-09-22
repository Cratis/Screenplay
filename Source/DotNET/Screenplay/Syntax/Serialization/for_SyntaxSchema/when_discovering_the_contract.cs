// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson.given;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_discovering_the_contract : Specification
{
    string[] _kinds;
    JsonElement[] _schemas;

    void Because()
    {
        _kinds = [.. SyntaxSchema.Kinds];
        _schemas = [.. _kinds.Select(SyntaxSchema.For)];
    }

    [Fact] void should_discover_every_concrete_compiler_node() => _kinds.ShouldContainOnly([.. syntax_examples.Types.Select(type => type.Name)]);
    [Fact] void should_have_a_nonempty_contract() => _kinds.Length.ShouldBeGreaterThan(100);
    [Fact] void should_have_no_duplicate_discriminators() => _kinds.Distinct(StringComparer.Ordinal).Count().ShouldEqual(_kinds.Length);
    [Fact] void should_describe_every_kind() => _schemas.Length.ShouldEqual(_kinds.Length);
    [Fact] void should_disallow_unknown_properties() => _schemas.All(schema => !schema.GetProperty("additionalProperties").GetBoolean()).ShouldBeTrue();
    [Fact] void should_require_every_discriminator() => _schemas.All(schema => schema.GetProperty("required").EnumerateArray().Any(value => value.GetString() == "kind")).ShouldBeTrue();
    [Fact] void should_define_every_child_kind() => _schemas.All(schema => schema.GetProperty("$defs").EnumerateObject().Count() == _kinds.Length).ShouldBeTrue();
}
