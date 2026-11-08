// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices.given;

public class a_model_with_dependent_slices : Specification
{
    protected const string Source = """
        concept ProjectName : String

        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name ProjectName
                produces Registered
                  for projectId
                  name = name
              event Registered
                name ProjectName
            slice StateView Listing
              readmodel Project
                projectId Uuid
                name ProjectName
              query All => Project[]
              projection Projects => Project
                from Registered
                  projectId = $eventSourceId
                  name = name
            slice StateChange Archive
              command Archive
                projectId Uuid identifier
                produces Archived
                  for projectId
              event Archived
        """;

    protected ScreenplayWorkspace _before = null!;
    protected ScreenplayWorkspace _after = null!;
    protected ModelDifference _result = null!;
    protected bool _byAddress;

    protected SliceChange Slice(string address) => _result.Slices.Slices.Single(slice => slice.Address == address);

    void Establish()
    {
        _before = Create(Source);
        _after = _before;
    }

    protected void Compare() => _result = _byAddress
        ? ModelComparison.Compare(ComparedModel.WithoutIdentities(_before), ComparedModel.WithoutIdentities(_after))
        : ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    protected static ScreenplayWorkspace Create(string source, SemanticIdentityCatalog? catalog = null) => ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))],
        catalog ?? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected void ChangeSource(string source) => _after = Create(source, _byAddress ? null : _before.IdentityCatalog);
}
