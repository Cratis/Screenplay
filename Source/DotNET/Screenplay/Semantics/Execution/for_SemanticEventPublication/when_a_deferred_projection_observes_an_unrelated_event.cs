// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEventPublication;

public class when_a_deferred_projection_observes_an_unrelated_event : a_scoped_publication_plan
{
    bool _published;
    ImmutableArray<SemanticFact> _publications;
    string? _failure;

    void Establish() => Prepare(true);
    void Because() => _published = SemanticEventPublication.TryPublish(_plan, [], [_fact], out _publications, out _failure);

    [Fact] void should_refuse_publication() => _published.ShouldBeFalse();
    [Fact] void should_identify_the_deferred_publisher() => _failure!.ShouldContain("Publisher");
    [Fact] void should_publish_nothing() => _publications.ShouldBeEmpty();
}
