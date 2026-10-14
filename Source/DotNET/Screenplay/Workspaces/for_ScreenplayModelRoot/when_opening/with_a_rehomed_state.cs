// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_a_rehomed_state : given.a_persisted_model_root
{
    ScreenplayWorkspace _rehomed = null!;
    ScreenplayModelRootOutcome _outcome = null!;
    ScreenplayModelRootOutcome _baselineOutcome = null!;
    byte[] _state = [];

    void Establish()
    {
        var split = Source.IndexOf("module Projects", StringComparison.Ordinal);
        var paths = new[] { PortablePlayPath.Parse("application.play"), PortablePlayPath.Parse("Projects/Registration/Registration.play") };
        _rehomed = Original.Rehome([
            WorkspaceDocument.Create(ModelRootDocumentKeys.For(paths[0]), paths[0], Encoding.UTF8.GetBytes(Source[..split])),
            WorkspaceDocument.Create(ModelRootDocumentKeys.For(paths[1]), paths[1], Encoding.UTF8.GetBytes(Source[split..]))
        ]);
        foreach (var document in _rehomed.Documents)
        {
            var path = Path.Combine(RootPath, document.Path.Value);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. document.Bytes]);
        }

        _state = PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(_rehomed));
        File.WriteAllBytes(IdentitiesPath, _state);
    }

    void Because()
    {
        _outcome = ScreenplayModelRoot.Open(RootPath);
        _baselineOutcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath);
    }

    [Fact] void should_accept_the_split() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_materialized_catalog() => CatalogOf(_outcome).ShouldEqual(SemanticIdentityCatalogSerializer.Serialize(_rehomed.IdentityCatalog));
    [Fact] void should_preserve_every_event_assignment() => _outcome.Workspace!.IdentityCatalog.EventContracts.ShouldContainOnly(Original.IdentityCatalog.EventContracts);
    [Fact] void should_preserve_every_semantic_assignment() => _outcome.Workspace!.IdentityCatalog.Semantics.ShouldContainOnly(Original.IdentityCatalog.Semantics);
    [Fact] void should_preserve_the_workspace_revision() => _outcome.Workspace!.Revision.ShouldEqual(_rehomed.Revision);
    [Fact] void should_accept_the_baseline_check_open() => _baselineOutcome.Success.ShouldBeTrue();
    [Fact] void should_return_the_same_model_revision_for_baseline_checks() => _baselineOutcome.Workspace!.Compilation.Value!.Model.Revision.ShouldEqual(_outcome.Workspace!.Compilation.Value!.Model.Revision);
    [Fact] void should_not_rewrite_the_state() => File.ReadAllBytes(IdentitiesPath).ShouldEqual(_state);
}
