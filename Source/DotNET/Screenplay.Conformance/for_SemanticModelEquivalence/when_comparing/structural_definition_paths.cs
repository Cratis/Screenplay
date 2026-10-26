// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class structural_definition_paths : Specification
{
    static readonly string _first = $"sem1:{new string('a', 64)}";
    static readonly string _second = $"sem1:{new string('b', 64)}";
    static readonly string _third = $"sem1:{new string('c', 64)}";
    static readonly string _fourth = $"sem1:{new string('d', 64)}";

    [Theory]
    [InlineData(false, "/application/types[B]")]
    [InlineData(true, "/application/types[B]#contract")]
    public void should_refuse_duplicate_definitions_at_the_second_exact_path(bool contract, string path)
    {
        var second = contract ? $$"""{"id":"{{_second}}","contractId":"{{_first}}","name":"B"}""" : $$"""{"id":"{{_first}}","name":"B"}""";
        using var document = JsonDocument.Parse($$"""{"application":{"types":[{"id":"{{_first}}","name":"A"},{{second}}]} }""");
        var error = Assert.Throws<SemanticNormalizationFailed>(() => SemanticStructuralKeys.Assign(document.RootElement));
        error.Path.ShouldEqual(path);
        error.Message.ShouldEqual($"Cannot normalise '{path}': the identity '{_first}' is defined more than once");
    }

    [Fact]
    public void should_register_contracts_and_nested_owner_sensitive_ordered_paths()
    {
        using var document = JsonDocument.Parse($$"""{"application":{"types":[{"id":"{{_first}}","contractId":"{{_second}}","name":"A"}]},"source":{"path":[{"id":"{{_third}}"},{"id":"{{_fourth}}"}]} }""");
        var keys = SemanticStructuralKeys.Assign(document.RootElement);
        keys.TryResolve(_first, out var definition).ShouldBeTrue();
        definition.ShouldEqual("/application/types[A]");
        keys.TryResolve(_second, out var contract).ShouldBeTrue();
        contract.ShouldEqual("/application/types[A]#contract");
        keys.TryResolve(_third, out var first).ShouldBeTrue();
        first.ShouldEqual("/source/path[0]");
        keys.TryResolve(_fourth, out var second).ShouldBeTrue();
        second.ShouldEqual("/source/path[1]");
    }

    [Fact]
    public void should_assign_reference_set_definitions_by_stripped_text_not_resolved_reference()
    {
        using var document = JsonDocument.Parse($$"""{"key":{"parts":[{"id":"{{_first}}","property":"{{_third}}","value":2},{"id":"{{_second}}","property":"{{_fourth}}","value":1}]} }""");
        var keys = SemanticStructuralKeys.Assign(document.RootElement);
        keys.TryResolve(_first, out var first).ShouldBeTrue();
        first.ShouldEqual("/key/parts[~2]");
        keys.TryResolve(_second, out var second).ShouldBeTrue();
        second.ShouldEqual("/key/parts[~1]");
    }

    [Fact]
    public void should_normalize_reordered_reference_sets_using_resolved_keys_without_fallbacks()
    {
        var definitions = $$"""{"types":[{"id":"{{_first}}","name":"A"},{"id":"{{_second}}","name":"B"}]}""";
        var first = $$"""{"property":"{{_first}}","value":1}""";
        var second = $$"""{"property":"{{_second}}","value":2}""";
        var original = SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes($$"""{"application":{{definitions}},"key":{"parts":[{{second}},{{first}}]} }"""));
        var reordered = SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes($$"""{"application":{{definitions}},"key":{"parts":[{{first}},{{second}}]} }"""));
        original.FallbackKeys.ShouldBeEmpty();
        reordered.FallbackKeys.ShouldBeEmpty();
        reordered.Text.ShouldEqual(original.Text);
        original.Root.Members["key"].Text.ShouldEqual("""{"parts":{"[@/application/types[A]]":{"property":"@/application/types[A]","value":1},"[@/application/types[B]]":{"property":"@/application/types[B]","value":2}}}""");
    }
}
