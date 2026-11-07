// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

/// <summary>
/// Screenplay 6 names declarations from outlets, hooks, pickers and slots. A rename that would leave one of those names
/// stale is refused rather than half done.
/// </summary>
public class when_a_rename_would_leave_a_placement_reference_stale : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _layoutHook;
    WorkspaceAuthoringResult _featureHook;
    WorkspaceAuthoringResult _picker;
    WorkspaceAuthoringResult _shown;
    WorkspaceAuthoringResult _unrelated;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop",
        [Document("model", "application.play", """
            layout AppShell
              category Shell
              content
              outlet topbarActions of actions
                default register title "Register" slice Shop.Customers.RegisterCustomer
              arrangement flow
                row
                  content grow

            module Shop
              form RegisterCustomerForm for RegisterCustomer
                field segment
                  choose from query AllSegments show label

              feature Customers
                outlet topbarActions extends
                  button rename title "Rename" slice Shop.Customers.RenameCustomer

                slice StateChange RegisterCustomer
                  command RegisterCustomer
                    customerId Uuid identifier
                    segment String
                    produces CustomerRegistered
                      segment = segment
                  event CustomerRegistered
                    segment String
                  screen RegisterCustomer
                    title "Register a customer"

                slice StateChange RenameCustomer
                  command RenameCustomer
                    customerId Uuid identifier
                    name String
                    produces CustomerRenamed
                      name = name
                  event CustomerRenamed
                    name String
                  screen RenameCustomer
                    title "Rename a customer"

                slice StateView Segments
                  readmodel Segment
                    label String
                  query AllSegments => Segment[]

                slice StateView Unused
                  readmodel UnusedRow
                    value String
                  query AllUnused => UnusedRow[]
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));

    void Because()
    {
        _layoutHook = Workspace.ProposeRename(Rename<SliceSyntax>("RegisterCustomer", "EnrollCustomer"));
        _featureHook = Workspace.ProposeRename(Rename<SliceSyntax>("RenameCustomer", "RetitleCustomer"));
        _picker = Workspace.ProposeRename(Rename<QuerySyntax>("AllSegments", "EverySegment"));
        _shown = Workspace.ProposeRename(Rename<ReadModelSyntax>("Segment", "CustomerSegment"));
        _unrelated = Workspace.ProposeRename(Rename<QuerySyntax>("AllUnused", "EveryUnused"));
    }

    [Fact] void should_refuse_renaming_the_slice_a_layout_default_hook_names() => _layoutHook.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_renaming_the_slice_a_feature_hook_names() => _featureHook.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_hook_names_the_slice() => _featureHook.Conflicts.Single().Message.ShouldContain("toolbar button");
    [Fact] void should_refuse_renaming_the_query_a_picker_reads() => _picker.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_picker_reads_the_query() => _picker.Conflicts.Single().Message.ShouldContain("picker");
    [Fact] void should_still_rename_a_query_nothing_picks_from() => _unrelated.Accepted.ShouldBeTrue();
    [Fact] void should_still_rename_the_read_model_a_picker_shows_a_property_of() => _shown.Accepted.ShouldBeTrue();
}
