// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Agents;

public class when_walking_the_allowlist : given.an_agent_model
{
    Walker _walker;

    void Because()
    {
        _walker = new();
        _walker.VisitApplication(Compiler.Compile(Model + "\n" + Allowlist).Value!);
    }

    [Fact] void should_visit_the_block() => _walker.Nodes.OfType<AgentsSyntax>().Count().ShouldEqual(1);
    [Fact] void should_visit_the_tokens() => _walker.Nodes.OfType<AgentTokensSyntax>().Count().ShouldEqual(1);
    [Fact] void should_visit_each_operation() => _walker.Nodes.OfType<AgentOperationSyntax>().Count().ShouldEqual(2);

    sealed class Walker : ScreenplaySyntaxWalker
    {
        public List<SyntaxNode> Nodes { get; } = [];

        public override void VisitNode(SyntaxNode syntax) => Nodes.Add(syntax);
    }
}
