// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class without_publishing_v35 : given.a_possible_events_binder
{
    [Fact]
    void should_refuse_to_write_v2_below_v35()
    {
        var binding = BindPossible(Source).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V34, SemanticVersion.V34, binding.Application));
    }

    [Fact]
    void should_not_silently_drop_v2_from_a_candidate_v35_graph()
    {
        var binding = BindPossible(Source).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
    }

    [Fact]
    void should_keep_the_public_v34_factory_closed()
    {
        var binding = BindPossible(Source).Value!;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V34, SemanticVersion.V34, binding.Application));
    }

    [Fact]
    void should_keep_the_public_factory_versions_unchanged()
    {
        var binding = BindPossible(Source).Value!;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
        LanguageVersion.V1.Major.ShouldEqual(1u);
        SemanticVersion.V1.Major.ShouldEqual(1u);
    }
}
