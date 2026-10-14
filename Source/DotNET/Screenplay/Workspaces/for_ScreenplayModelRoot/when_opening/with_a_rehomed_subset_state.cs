// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_a_rehomed_subset_state : given.a_persisted_model_root
{
    ScreenplayWorkspace _rehomed = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        var concepts = Source[..Source.IndexOf("module Projects", StringComparison.Ordinal)];
        var path = PortablePlayPath.Parse("application.play");
        _rehomed = Original.Rehome([WorkspaceDocument.Create(ModelRootDocumentKeys.For(path), path, Encoding.UTF8.GetBytes(concepts))], RehomeScope.Subset);
        foreach (var file in Directory.EnumerateFiles(RootPath, "*.play", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }

        File.WriteAllBytes(Path.Combine(RootPath, path.Value), Encoding.UTF8.GetBytes(concepts));
        File.WriteAllBytes(IdentitiesPath, PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(_rehomed)));
    }

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_accept_the_subset() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_materialized_catalog() => CatalogOf(_outcome).ShouldEqual(SemanticIdentityCatalogSerializer.Serialize(_rehomed.IdentityCatalog));
    [Fact] void should_keep_the_original_ids_of_present_declarations() =>
        _outcome.Workspace!.IdentityCatalog.Semantics.All(assignment => Original.IdentityCatalog.Semantics.Contains(assignment)).ShouldBeTrue();
}
