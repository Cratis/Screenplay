// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_document;

public class and_a_subtree_is_pruned : given.the_invoicing_document
{
    walker _walker;

    void Establish() => _walker = new();

    void Because() => _walker.VisitApplication(_document);

    [Fact] void should_reach_the_slices() => _walker.Slices.ShouldNotBeEmpty();
    [Fact] void should_not_descend_into_a_slice_the_override_did_not_continue_from() => _walker.Commands.ShouldBeEmpty();

    class walker : ScreenplaySyntaxWalker
    {
        public List<SliceSyntax> Slices { get; } = [];

        public List<CommandSyntax> Commands { get; } = [];

        public override void VisitSlice(SliceSyntax syntax) => Slices.Add(syntax);

        public override void VisitCommand(CommandSyntax syntax)
        {
            Commands.Add(syntax);
            base.VisitCommand(syntax);
        }
    }
}
