// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_document_is_nested_too_deep : given.a_workspace_with_assigned_events
{
    Exception? _deepest;
    Exception? _tooDeep;

    void Because()
    {
        var sixteen = string.Join('/', Enumerable.Range(1, 16).Select(level => $"d{level}"));
        _deepest = Catch.Exception(() => Original.Rehome([Document("concepts.play", Concepts), Document($"{sixteen}/events.play", Events)]));
        _tooDeep = Catch.Exception(() => Original.Rehome([Document("concepts.play", Concepts), Document($"{sixteen}/d17/events.play", Events)]));
    }

    [Fact] void should_accept_sixteen_directory_levels() => _deepest.ShouldBeNull();
    [Fact] void should_refuse_seventeen_directory_levels() => _tooDeep.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
}
