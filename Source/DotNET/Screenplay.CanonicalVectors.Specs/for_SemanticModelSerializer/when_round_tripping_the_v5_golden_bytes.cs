// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalVectors.Specs.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_v5_golden_bytes : Specification
{
    byte[] _expected = [];
    ExecutableSemanticModel _model = null!;

    void Establish() => _expected = canonical_serialization_golden_bytes.SemanticModelV5;
    void Because() => _model = SemanticModelSerializer.Deserialize(_expected);

    [Fact] void should_preserve_exact_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_pin_the_revision() => _model.Revision.ToString().ShouldEqual("rev1:3ad964ba480ed7e45cefc6463532225c9ddaf8222b16f07ce44a75ecab450225");
    [Fact] void should_select_language_and_semantics_v5() => (_model.LanguageVersion == LanguageVersion.V5 && _model.SemanticVersion == SemanticVersion.V5).ShouldBeTrue();
    [Fact] void should_retain_prior_event_revisions() => _model.Application.Modules.Single().Features.Single().Features.Single().Slices.SelectMany(_ => _.Events).Any(_ => !_.PriorRevisions.IsEmpty).ShouldBeTrue();
    [Fact] void should_omit_transition_cardinality() => Encoding.UTF8.GetString(_expected).ShouldNotContain("\"affectedInstance\":{\"cardinality\"");
    [Fact] void should_reject_v5_without_absence()
    {
        var module = _model.Application.Modules.Single();
        var root = module.Features.Single();
        var feature = root.Features.Single();
        var changed = _model.Application with { Modules = [module with { Features = [root with { Features = [feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with { Specifications = [.. slice.Specifications.Select(specification => specification with { ThenAbsentReadModels = [] })] })]
        }] }] }] };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V5, SemanticVersion.V5, changed)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
    [Fact] void should_reject_the_v5_field_in_a_v4_document()
    {
        var bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(canonical_serialization_golden_bytes.SemanticModelV4)
            .Replace("\"thenReadModels\":", "\"thenAbsentReadModels\":[],\"thenReadModels\":", StringComparison.Ordinal));
        Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
