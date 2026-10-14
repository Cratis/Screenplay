// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_document_is_in_an_excluded_directory : given.a_workspace_with_assigned_events
{
    readonly Dictionary<string, Exception?> _exceptions = [];

    void Because()
    {
        foreach (var directory in new[] { "bin", "obj", "node_modules", ".git", ".screenplay", ".ai-work" })
        {
            _exceptions[directory] = Catch.Exception(() => Original.Rehome([Document("concepts.play", Concepts), Document($"{directory}/events.play", Events)]));
        }
    }

    [Fact] void should_refuse_every_directory_discovery_skips() => _exceptions.Values.All(exception => exception is InvalidScreenplayWorkspace).ShouldBeTrue();
    [Fact] void should_name_the_source_scope() => _exceptions.Values.All(exception => exception!.Message.Contains("outside the admitted source scope", StringComparison.Ordinal)).ShouldBeTrue();
}
