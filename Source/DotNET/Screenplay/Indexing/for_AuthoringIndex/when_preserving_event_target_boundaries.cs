// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex;

public class when_preserving_event_target_boundaries : Specification
{
    (string Name, AuthoringIndex Index, string Target, string[] Candidates, bool Implicit)[] _cases = [];

    void Because() => _cases = [.. given.event_target_applications.Boundaries().Select(value => (value.Name, new AuthoringSnapshot(value.Sources).Index, value.Target, value.Candidates, value.Implicit))];

    [Fact]
    void should_keep_the_baseline_requested_kinds_and_candidate_order()
    {
        foreach (var value in _cases)
        {
            var edge = value.Index.ResolvedReferences.Single(edge => edge.Reference.Role == "builds" || edge.Reference.Role == "buildsVariant");
            (value.Name + ":" + edge.Reference.Name + ":" + string.Join(',', edge.Reference.Kinds) + ":" + string.Join(',', edge.Candidates.Select(candidate => candidate.Address))).ShouldEqual(value.Name + ":" + value.Target + ":ReadModel:" + string.Join(',', value.Candidates));
        }
    }

    [Fact]
    void should_keep_explicit_and_inferred_candidate_identity()
    {
        foreach (var value in _cases)
        {
            var edge = value.Index.ResolvedReferences.Single(edge => edge.Reference.Role == "builds" || edge.Reference.Role == "buildsVariant");
            edge.Candidates.Select(candidate => candidate.Kind + ":" + candidate.IsImplicit).ShouldEqual(value.Candidates.Select(_ => "ReadModel:" + value.Implicit));
        }
    }

    [Fact]
    void should_keep_the_outbound_admission_diagnostic_for_collisions_and_projection_name_fallback()
    {
        foreach (var value in given.event_target_applications.Boundaries().Where(value => value.Name == "same slice readmodel" || value.Name == "projection name fallback"))
        {
            var compilation = new AuthoringSnapshot(value.Sources).Compilation;
            compilation.Success.ShouldBeFalse();
            compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0609").Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Severity}:{diagnostic.Location.Path}:{diagnostic.Location.Line}:{diagnostic.Location.Column}:{diagnostic.Message}").ShouldEqual(["PLAY0609:Error:model.play:3:5:Outbound Translate slice 'Publish' must produce exactly one local public event type; found 0."]);
        }
    }

    [Fact]
    void should_keep_each_authored_reference_location_and_owner()
    {
        foreach (var value in _cases)
        {
            var reference = value.Index.References.Single(reference => reference.Role == "builds" || reference.Role == "buildsVariant");
            reference.Location.Path.ShouldEqual("model.play");
            reference.Location.Column.ShouldEqual(value.Target == "V" ? 9 : 7);
            reference.Owner!.Address.ShouldEqual("M.F.Publish.P");
            reference.Scope.ShouldEqual(["M", "F", "Publish"]);
        }
    }
}
