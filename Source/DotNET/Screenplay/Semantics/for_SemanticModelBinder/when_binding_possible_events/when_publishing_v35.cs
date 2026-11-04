// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class when_publishing_v35 : given.a_possible_events_binder
{
    [Fact]
    void should_refuse_to_write_v2_below_v35()
    {
        var model = BindPossible(Source).Value.Model;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V34, SemanticVersion.V34, model.Application));
    }

    [Fact]
    void should_publish_the_v35_graph()
    {
        var model = BindPossible(Source).Value.Model;
        SemanticModelCanonicalJson.SerializeWithoutRevision(model.LanguageVersion, model.SemanticVersion, model.Application).ShouldNotBeEmpty();
    }

    [Fact]
    void should_keep_the_public_v34_factory_closed()
    {
        var model = BindPossible(Source).Value.Model;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V34, SemanticVersion.V34, model.Application));
    }

    [Fact]
    void should_round_trip_the_public_v35_factory()
    {
        var model = BindPossible(Source).Value.Model;
        ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application).Revision.ShouldEqual(model.Revision);
    }
}
