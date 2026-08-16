// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_document;

/// <summary>
/// The invoicing sample declares a layout, screen templates and a dialog template, but no freeform
/// <c>variant</c> and no <c>when</c> override - so those node kinds would go unwalked and nothing would say
/// so. This document declares every one of them, held against the same reflection oracle.
/// </summary>
public class and_it_declares_every_structural_kind : Specification
{
    const string Source =
        """
        layout AppShell
          navigation contributes Navigation
          content

          arrangement freeform
            variant width regular, height regular
              place navigation at 0,0   size 240,fill
              place content    at 240,0 size fill,fill

            variant width compact, height regular
              place content at 0,0 size fill,fill
              place navigation hidden

        module Invoicing
          screen template MasterDetail
            fits slot content

            sidebar
            main

            arrangement flow
              row gap 16
                sidebar width 280
                main grow

              when width compact
                column
                  main
                  sidebar

          dialog template ConfirmDialog
            body
            actions
        """;

    ApplicationSyntax _document;
    given.a_counting_walker _walker;
    IReadOnlyList<SyntaxNode> _expected;

    void Establish()
    {
        _document = new ScreenplayCompiler().Compile(Source).Value!;
        _walker = new();
        _expected = given.SyntaxNodes.Under(_document);
    }

    void Because() => _walker.VisitApplication(_document);

    [Fact] void should_reach_every_node_the_tree_holds() => _walker.Nodes.Count.ShouldEqual(_expected.Count);
    [Fact] void should_reach_the_layout() => _walker.Nodes.OfType<LayoutSyntax>().Count().ShouldEqual(1);
    [Fact] void should_reach_the_screen_template() => _walker.Nodes.OfType<ScreenTemplateSyntax>().Count().ShouldEqual(1);
    [Fact] void should_reach_the_dialog_template() => _walker.Nodes.OfType<DialogTemplateSyntax>().Count().ShouldEqual(1);
    [Fact] void should_reach_every_arrangement() => _walker.Nodes.OfType<ArrangementSyntax>().Count().ShouldEqual(2);
    [Fact] void should_reach_every_declared_slot() => _walker.Nodes.OfType<SlotSyntax>().Count().ShouldEqual(6);
    [Fact] void should_reach_every_arrangement_container() => _walker.Nodes.OfType<ArrangementContainerSyntax>().Count().ShouldEqual(_expected.OfType<ArrangementContainerSyntax>().Count());
    [Fact] void should_reach_every_arrangement_slot() => _walker.Nodes.OfType<ArrangementSlotSyntax>().Count().ShouldEqual(_expected.OfType<ArrangementSlotSyntax>().Count());
    [Fact] void should_reach_the_when_override() => _walker.Nodes.OfType<ArrangementOverrideSyntax>().Count().ShouldEqual(1);
    [Fact] void should_reach_every_variant() => _walker.Nodes.OfType<VariantSyntax>().Count().ShouldEqual(2);
    [Fact] void should_reach_every_place() => _walker.Nodes.OfType<PlaceSyntax>().Count().ShouldEqual(4);
}
