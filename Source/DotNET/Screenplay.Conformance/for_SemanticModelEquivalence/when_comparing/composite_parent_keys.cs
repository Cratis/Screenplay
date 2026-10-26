// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class composite_parent_keys : given.compiled_models
{
    const string Model = """
        type ParentKey
          orderId Uuid
        type Line
          lineNumber Int
        module Orders
          feature Ordering
            slice StateChange Events
              event LineAdded
                parentKey ParentKey
                lineNumber Int
              event LineRemoved
                parentKey ParentKey
                lineNumber Int
            slice StateView View
              readmodel Orders
                key ParentKey
                lines Line[]
              query OrderByKey => Orders optional
                by key ParentKey
              projection Orders => Orders
                children lines identified by lineNumber
                  from LineAdded key lineNumber
                    parent parentKey
                  remove with LineRemoved key lineNumber
                    parent parentKey
        """;

    ExecutableSemanticModel _original;
    ExecutableSemanticModel _reversed;
    JsonNode _json;
    SemanticNormalizationFailed?[] _errors;

    void Establish()
    {
        _original = WithCompositeParents(Compile(Model));
        _reversed = WithCompositeParents(Compile(Model, Reversed(Workspace(Model).IdentityCatalog)));
        _json = JsonNode.Parse(SemanticModelSerializer.Serialize(_original));
    }

    void Because()
    {
        _comparison = SemanticModelEquivalence.Compare(_original, _reversed);
        _errors = [Unmapped("from"), Unmapped("removedWith")];
    }

    // Composite parent keys are an ESM writer shape; the text parser only admits scalar parents.
    static ExecutableSemanticModel WithCompositeParents(ExecutableSemanticModel model)
    {
        var type = model.Application.Types.Single(_ => _.Name == "ParentKey");
        var part = type.Properties.Single().Id;
        SemanticProjectionKey Key(SemanticProjectionKey? key) => new SemanticProjectionCompositeKey(type.Id,
            [new(part, new SemanticProjectionEventProperty(((SemanticProjectionEventProperty)((SemanticProjectionValueKey)key).Value).Path.Add(part)))]);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices[1];
        var projection = slice.Projections.Single();
        var child = projection.Scope.Children.Single();
        var scope = child.Scope with
        {
            From = [.. child.Scope.From.Select(item => item with { ParentKey = Key(item.ParentKey) })],
            Removals = [.. child.Scope.Removals.Select(item => item with { ParentKey = Key(item.ParentKey) })],
        };
        projection = projection with { Scope = projection.Scope with { Children = [child with { Scope = scope }] } };
        slice = slice with { Projections = [projection] };
        feature = feature with { Slices = feature.Slices.SetItem(1, slice) };
        module = module with { Features = [feature] };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Modules = [module] });
    }

    SemanticNormalizationFailed? Unmapped(string member)
    {
        var json = _json.DeepClone();
        var scope = json["application"]["modules"][0]["features"][0]["slices"][1]["projections"][0]["scope"]["children"][0]["scope"];
        scope[member][0]["parentKey"]["type"] = $"sem1:{new string('a', 64)}";
        return Catch.Exception(() => SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(json.ToJsonString()))) as SemanticNormalizationFailed;
    }

    [Fact] void should_reassign_identities() => _reversed.Revision.ShouldNotEqual(_original.Revision);
    [Fact] void should_preserve_equivalence_for_child_transitions_and_removals() => _comparison.IsEquivalent.ShouldBeTrue();
    [Fact] void should_reject_an_unmapped_parent_type_on_a_child_transition() => AssertUnmapped(_errors[0]);
    [Fact] void should_reject_an_unmapped_parent_type_on_a_child_removal() => AssertUnmapped(_errors[1]);

    static void AssertUnmapped(SemanticNormalizationFailed? error)
    {
        error.ShouldNotBeNull();
        error.Path.EndsWith("/parentKey/type", StringComparison.Ordinal).ShouldBeTrue();
    }
}
