// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ComparedModel;

public class when_creating_from_sources_in_subfolders : Specification
{
    ComparedModel _result = null!;

    void Because() => _result = ComparedModel.FromSources("Projects", new Dictionary<string, string>
    {
        ["application.play"] = "module Projects\n  feature Registration\n    import \"Features/*.play\"\n",
        ["Features/Register.play"] = "slice StateChange Register\n  command Register\n"
    });

    [Fact] void should_retain_every_relative_path() => _result.Workspace.Documents.Select(document => document.Path.Value).ShouldContainOnly("application.play", "Features/Register.play");
    [Fact] void should_key_each_document_like_a_model_root() => _result.Workspace.Documents.Single(document => document.Path.Value == "Features/Register.play").StableKey.ShouldEqual(ModelRootDocumentKeys.For(PortablePlayPath.Parse("Features/Register.play")));
    [Fact] void should_not_claim_persisted_identities() => _result.HasPersistedIdentities.ShouldBeFalse();
}
