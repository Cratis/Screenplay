// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalVectors.Specs.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_v6_golden_bytes : Specification
{
    byte[] _expected = [];
    ExecutableSemanticModel _model = null!;

    void Establish() => _expected = canonical_serialization_golden_bytes.SemanticModelV6;
    void Because() => _model = SemanticModelSerializer.Deserialize(_expected);

    [Fact] void should_preserve_exact_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_pin_the_revision() => _model.Revision.ToString().ShouldEqual("rev1:28a12866397631991d1dcd4d8d2ac58920ed7fac5e72a0a2ac52e806890d96cd");
    [Fact] void should_select_language_and_semantics_v6() => (_model.LanguageVersion == LanguageVersion.V6 && _model.SemanticVersion == SemanticVersion.V6).ShouldBeTrue();
    [Fact] void should_retain_v5_absence() => _model.Application.Modules.Single().Features.SelectMany(AllSlices).SelectMany(_ => _.Specifications).Any(_ => !_.ThenAbsentReadModels.IsEmpty).ShouldBeTrue();
    [Fact] void should_hold_the_application_trigger() => _model.Application.Triggers.Single().Name.ShouldEqual("BatchArrived");
    [Fact] void should_hold_reactions_and_a_capture() => _model.Application.Modules.Single().Features.SelectMany(AllSlices).Sum(_ => _.Reactions.Length + _.Captures.Length).ShouldEqual(4);
    [Fact] void should_reject_v6_without_v6_constructs()
    {
        var module = _model.Application.Modules.Single();
        var changed = _model.Application with { Triggers = [], Modules = [module with { Features = [.. module.Features.Where(_ => _.Name != "Automation")] }] };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, changed)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
    [Fact] void should_reject_v6_fields_in_a_v5_document()
    {
        var bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(canonical_serialization_golden_bytes.SemanticModelV5)
            .Replace("\"thenReadModels\":", "\"givenClock\":\"2026-10-02T09:00:00.0000000Z\",\"thenReadModels\":", StringComparison.Ordinal));
        Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
