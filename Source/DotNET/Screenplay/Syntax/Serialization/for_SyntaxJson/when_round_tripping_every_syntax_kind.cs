// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_every_syntax_kind : Specification
{
    SyntaxNode[] _examples;
    SyntaxNode[] _roundTrips;

    void Establish() => _examples = [.. given.syntax_examples.Types.Select(type => given.syntax_examples.Create(type))];

    void Because() => _roundTrips = [.. _examples.Select(node => SyntaxJson.Deserialize(SyntaxJson.Serialize(node)))];

    [Fact] void should_exercise_a_nonempty_population() => _examples.Length.ShouldBeGreaterThan(100);
    [Fact] void should_preserve_every_structural_member_independently_of_the_descriptors() => _examples.Zip(_roundTrips).All(pair => given.syntax_examples.SameValues(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_compare_all_round_trips_equally() => _examples.Zip(_roundTrips).All(pair => SyntaxJson.StructurallyEqual(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_preserve_every_concrete_type() => _examples.Select(node => node.GetType()).SequenceEqual(_roundTrips.Select(node => node.GetType())).ShouldBeTrue();
    [Fact] void should_assign_server_locations() => _roundTrips.All(node => node.Location == Diagnostics.SourceLocation.Start).ShouldBeTrue();
}
