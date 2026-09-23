// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceDroppedComments;

public class when_a_comment_moves_to_another_line : Specification
{
    WorkspaceDroppedComment[] _dropped = [];

    void Because()
    {
        var before = WorkspaceDocument.Create("a", PortablePlayPath.Parse("a.play"), Encoding.UTF8.GetBytes("// kept\nconcept A : String // moved\n// gone\n// gone\n"));
        var after = WorkspaceDocument.Create("a", PortablePlayPath.Parse("a.play"), Encoding.UTF8.GetBytes("// moved\nconcept A : String\n// kept\n// gone\n"));
        _dropped = [.. WorkspaceDroppedComments.Between(before, after)];
    }

    [Fact] void should_not_report_comments_that_survive_elsewhere() => _dropped.Length.ShouldEqual(1);
    [Fact] void should_report_each_missing_occurrence() => _dropped[0].Line.ShouldEqual(4);
    [Fact] void should_report_the_exact_text() => _dropped[0].Text.ShouldEqual("// gone");
}
