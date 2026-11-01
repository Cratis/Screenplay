// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticValueValidator.when_revalidating_a_memoized_composite;

public class at_a_deeper_depth : Specification
{
    SemanticValueValidator _validator;
    SemanticTypeReference _target;
    SemanticValue _nested;
    Exception? _exception;

    void Establish()
    {
        var typeId = SemanticId.Create(SemanticKind.CompositeType, "Node");
        var childId = SemanticId.Create(SemanticKind.Property, "child");
        _target = SemanticTypeReference.ForCompositeType(typeId);
        var type = new SemanticCompositeType(typeId, "Node", [new(childId, "child", _target with { IsOptional = true }, false)]);
        _validator = new(new Dictionary<SemanticId, SemanticConcept>(), new Dictionary<SemanticId, SemanticCompositeType> { [typeId] = type }, memoize: true);
        _nested = SemanticValue.Composite([]);
        _validator.Validate(_nested, _target with { IsOptional = true }, "node");
        for (var depth = 0; depth < CanonicalJson.MaximumDepth; depth++)
        {
            _nested = SemanticValue.Composite([new(childId, _nested)]);
        }
    }

    void Because() => _exception = Catch.Exception(() => _validator.Validate(_nested, _target, "node"));

    [Fact] void should_still_enforce_the_maximum_depth() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_keep_the_depth_failure_message() => _exception!.Message.ShouldEqual($"A semantic value exceeds the canonical maximum depth of {CanonicalJson.MaximumDepth}.");
}
