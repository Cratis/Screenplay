// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

/// <summary>
/// A module with no navigation, a toolbar with no buttons and an item with no sub-items leave that member out of the
/// canonical JSON until it holds something. Adding the first child must still work; a member the kind does not declare
/// must still be refused.
/// </summary>
public class when_adding_to_a_member_that_is_omitted_while_empty : Specification
{
    const string Source = """
        layout Shell
          category application
          content

          navigation sidebar
            item sales title "Sales" target Sales

        module Sales
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                reason String
                produces OrderPlaced
                  for orderId
                  reason = reason
              event OrderPlaced
                reason String

            slice StateView OrderList
              readmodel OrderRow
                title String

              screen OrderList
                toolbar actions
                  item place action PlaceOrder
        """;

    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringResult _region = null!;
    WorkspaceAuthoringResult _button = null!;
    WorkspaceAuthoringResult _child = null!;
    WorkspaceAuthoringResult _unknown = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    void Because()
    {
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var module = index.Entries.Single(entry => entry.Node is ModuleSyntax);
        var toolbar = index.Entries.Single(entry => entry.Node is ScreenToolbarSyntax);
        var item = index.Entries.Single(entry => entry.Node is NavigationItemSyntax);
        var region = new NavigationRegionSyntax(NavigationRegionKind.Topbar, NavigationRegionMode.Extend, [new NavigationItemSyntax("home", "Home", SourceLocation.Start) { Target = new("Sales", null, SourceLocation.Start) }], SourceLocation.Start);
        var button = new ToolbarButtonSyntax("export", "Export", SourceLocation.Start) { Slice = "PlaceOrder" };
        var child = new NavigationItemSyntax("orders", "Orders", SourceLocation.Start) { Target = new("Sales", "Orders", SourceLocation.Start) };
        _region = Propose(new AddWorkspaceNode(module.Handle, module.Node, "navigation", region));
        _button = Propose(new AddWorkspaceNode(toolbar.Handle, toolbar.Node, "buttons", button));
        _child = Propose(new AddWorkspaceNode(item.Handle, item.Node, "children", child));
        _unknown = Propose(new AddWorkspaceNode(module.Handle, module.Node, "nothing", region));
    }

    [Fact] void should_add_the_first_navigation_region_to_a_module() => _region.Accepted.ShouldBeTrue();
    [Fact] void should_add_the_first_button_to_a_toolbar() => _button.Accepted.ShouldBeTrue();
    [Fact] void should_add_the_first_sub_item_to_an_item() => _child.Accepted.ShouldBeTrue();
    [Fact] void should_print_the_added_region() => Text(_region).ShouldContain("navigation topbar extends");
    [Fact] void should_print_the_added_button() => Text(_button).ShouldContain("button export title \"Export\" slice PlaceOrder");
    [Fact] void should_print_the_added_sub_item() => Text(_child).ShouldContain("item orders title \"Orders\" target Sales.Orders");
    [Fact] void should_still_refuse_a_member_the_kind_does_not_declare() => _unknown.Accepted.ShouldBeFalse();

    static string Text(WorkspaceAuthoringResult result) => result.WritePlan!.Entries.Single().After!.Text;

    WorkspaceAuthoringResult Propose(WorkspaceAstOperation operation) => _workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Operations = [operation]
    });
}
