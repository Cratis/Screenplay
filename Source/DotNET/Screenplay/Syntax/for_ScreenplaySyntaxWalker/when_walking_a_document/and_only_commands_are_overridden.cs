// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_document;

public class and_only_commands_are_overridden : given.the_invoicing_document
{
    walker _walker;
    IReadOnlyList<CommandSyntax> _expected;

    void Establish()
    {
        _walker = new();
        _expected = [.. given.SyntaxNodes.Under(_document).OfType<CommandSyntax>()];
    }

    void Because() => _walker.VisitApplication(_document);

    [Fact] void should_dispatch_every_command_in_the_document() => _walker.Commands.Count.ShouldEqual(_expected.Count);
    [Fact] void should_dispatch_the_commands_the_document_declares() => _walker.Commands.ShouldContainOnly(_expected);
    [Fact] void should_still_reach_the_nodes_below_a_command() => _walker.Properties.ShouldNotBeEmpty();

    class walker : ScreenplaySyntaxWalker
    {
        public List<CommandSyntax> Commands { get; } = [];

        public List<PropertySyntax> Properties { get; } = [];

        public override void VisitCommand(CommandSyntax syntax)
        {
            Commands.Add(syntax);
            base.VisitCommand(syntax);
        }

        public override void VisitProperty(PropertySyntax syntax)
        {
            Properties.Add(syntax);
            base.VisitProperty(syntax);
        }
    }
}
