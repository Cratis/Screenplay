// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_an_application_context : Specification
{
    [Fact]
    void should_visit_the_block_before_its_members()
    {
        var syntax = new ScreenplayCompiler().Parse("context\n  businessTimeZone time zone\n  customerTimeZone time zone\n").Value!;
        var walker = new Walker();
        walker.VisitApplication(syntax);
        walker.Visited.ShouldEqual(["context", "businessTimeZone", "customerTimeZone"]);
    }

    sealed class Walker : ScreenplaySyntaxWalker
    {
        internal List<string> Visited { get; } = [];

        public override void VisitApplicationContext(ApplicationContextSyntax syntax)
        {
            Visited.Add("context");
            base.VisitApplicationContext(syntax);
        }

        public override void VisitContextMember(ContextMemberSyntax syntax) => Visited.Add(syntax.Name);
    }
}
