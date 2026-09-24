// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_a_policy_file : Specification
{
    given.a_counting_walker _walker;

    void Establish() => _walker = new();

    void Because() => _walker.VisitPolicy(new("Access", null, null, Diagnostics.SourceLocation.Start)
    { File = new("Policies/Access.cs", Diagnostics.SourceLocation.Start) });

    [Fact] void should_visit_the_file() => _walker.Nodes.OfType<FileReferenceSyntax>().Single().Path.ShouldEqual("Policies/Access.cs");
}
