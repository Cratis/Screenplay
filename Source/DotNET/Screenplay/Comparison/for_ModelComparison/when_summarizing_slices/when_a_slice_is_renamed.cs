// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_a_slice_is_renamed : given.a_model_with_dependent_slices
{
    void Establish() => _after = _before.ProposeRename(new()
    {
        ExpectedRevision = _before.Revision,
        ExpectedCatalogRevision = _before.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(_before).Entries.First(entry => entry.Node is SliceSyntax slice && string.Equals(slice.Name, "Listing", StringComparison.Ordinal)).Handle,
        ExpectedName = "Listing",
        NewName = "Overview"
    }).Workspace!;

    void Because() => Compare();

    [Fact] void should_report_the_slice_as_renamed() => Slice("Projects.Registration.Overview").Change.ShouldEqual(SliceChangeKind.Renamed);
    [Fact] void should_not_report_content_changes() => Slice("Projects.Registration.Overview").Changes.ShouldContainOnly(SliceChangeKind.Renamed);
    [Fact] void should_report_only_the_renamed_slice() => _result.Slices.Slices.Count.ShouldEqual(1);
    [Fact] void should_keep_the_previous_address() => Slice("Projects.Registration.Overview").BeforeAddress.ShouldEqual("Projects.Registration.Listing");
    [Fact] void should_carry_the_slice_identity() => Slice("Projects.Registration.Overview").SemanticId.ShouldNotBeNull();
    [Fact] void should_attribute_the_children_that_follow_it() => Slice("Projects.Registration.Overview").Declarations.ShouldContain(declaration => declaration.Kind == "Projection");
}
