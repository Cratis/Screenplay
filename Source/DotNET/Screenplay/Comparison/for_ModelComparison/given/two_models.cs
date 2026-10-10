// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.given;

public class two_models : Specification
{
    protected const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                produces Registered
                  for projectId
                  name = name
              event Registered
                name String
              specification Registers
                when Register
                  projectId = "11111111-1111-1111-1111-111111111111"
                  name = "First"
                then Registered
                  name = "First"
            slice StateView List
              readmodel Projects
                name String
                projectId Uuid
              query All => Projects optional
                by projectId Uuid
            slice StateView Obsolete
              readmodel Archive
                name String
                projectId Uuid
              query History => Archive optional
                by projectId Uuid
        """;

    protected const string EventSource = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
    protected ScreenplayWorkspace _before = null!;
    protected ScreenplayWorkspace _after = null!;
    protected ModelDifference _result = null!;

    void Establish()
    {
        _before = Create(Source);
        _after = _before;
    }

    protected static ScreenplayWorkspace Create(string source, SemanticIdentityCatalog? catalog = null, string application = "Projects") => ScreenplayWorkspace.Create(application,
        [WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))],
        catalog ?? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(application)));

    protected void ChangeSource(string source, SemanticIdentityCatalog? catalog = null) => _after = Create(source, catalog ?? _before.IdentityCatalog);

    protected void RenameMember()
    {
        var property = _before.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.ReadModel && assignment.Address.Name == "name" && assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "List"));
        var owner = SemanticAddress.ForReadModel(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", "List"), "Projects");
        var catalog = SemanticIdentityCatalog.Create(
            _before.IdentityCatalog.Application,
            _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment.Id == property.Id ? assignment with { Address = SemanticAddress.ForProperty(owner, "title"), Origin = SemanticIdentityOrigin.Persisted } : assignment)],
            _before.IdentityCatalog.EventContracts);
        ChangeSource(Source.Replace("readmodel Projects\n        name String", "readmodel Projects\n        title String", StringComparison.Ordinal), catalog);
    }
}
