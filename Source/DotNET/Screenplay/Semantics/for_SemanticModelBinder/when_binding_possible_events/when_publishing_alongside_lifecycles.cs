// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class when_publishing_alongside_lifecycles : given.a_possible_events_binder
{
    ExecutableSemanticModel _model;
    byte[] _bytes;

    void Because()
    {
        var binding = BindPossible(Source + "\n" + a_lifecycle_source.Source);
        Assert.True(binding.Success, string.Join('\n', binding.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _model = binding.Value.Model;
        _bytes = SemanticModelCanonicalJson.Serialize(_model);
    }

    [Fact] void should_select_language_v36() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V36);
    [Fact] void should_select_semantic_v36() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_round_trip_the_public_factory() => ExecutableSemanticModel.Create(LanguageVersion.V36, SemanticVersion.V36, _model.Application).Revision.ShouldEqual(_model.Revision);
    [Fact] void should_strict_read_both_features() => SemanticModelSerializer.Deserialize(_bytes).Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
}
