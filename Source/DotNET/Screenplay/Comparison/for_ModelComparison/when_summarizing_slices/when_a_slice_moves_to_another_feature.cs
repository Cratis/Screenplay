// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_a_slice_moves_to_another_feature : given.a_model_with_dependent_slices
{
    const string Retirement = """

          feature Retirement
            slice StateView Retired
              readmodel Retiree
                projectId Uuid
              query Retirees => Retiree[]
        """;

    void Establish()
    {
        _before = Create(Source + Retirement);
        var application = _before.IdentityCatalog.Application;
        var result = _before.ProposeMove(new()
        {
            ExpectedRevision = _before.Revision,
            ExpectedCatalogRevision = _before.IdentityCatalog.Revision,
            Target = SemanticAddress.ForSlice(application, "Projects", "Registration", "Archive"),
            NewParent = SemanticAddress.ForFeature(application, "Projects", "Retirement"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
        _after = result.Workspace ?? throw new InvalidOperationException(string.Join("; ", result.Conflicts.Select(conflict => conflict.ToString())));
    }

    void Because() => Compare();

    [Fact] void should_report_the_slice_as_moved() => Slice("Projects.Retirement.Archive").Changes.ShouldContainOnly(SliceChangeKind.Moved);
    [Fact] void should_keep_the_previous_address() => Slice("Projects.Retirement.Archive").BeforeAddress.ShouldEqual("Projects.Registration.Archive");
    [Fact] void should_not_report_the_previous_address_as_removed() => _result.Slices.Slices.ShouldNotContain(slice => slice.Address == "Projects.Registration.Archive");
}
