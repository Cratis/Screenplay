// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_ModelSource;

public class when_loading_a_root_with_persisted_identities : Tool.given.a_renderable_model
{
    ModelSourceLoad _load;
    ScreenplayWorkspace _shared;
    int _exitCode;

    void Establish() => ToolCommands.Run(["rename", "Projects.Registration.List.Projects", "ProjectList", Root], TextWriter.Null, TextWriter.Null).ShouldEqual(0);

    void Because()
    {
        _load = ModelSource.Load(Root);
        _shared = ScreenplayModelRoot.Open(Root).Workspace!;
        _exitCode = ToolCommands.Run(["export", Root], Output, Error);
    }

    [Fact] void should_use_the_persisted_identities() => _load.HasPersistedIdentities.ShouldBeTrue();
    [Fact] void should_hold_the_identities_the_shared_model_root_holds() =>
        SemanticIdentityCatalogSerializer.Serialize(_load.Workspace.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(_shared.IdentityCatalog)).ShouldBeTrue();
    [Fact] void should_export_the_model_the_shared_model_root_compiles() =>
        Output.ToString().ShouldEqual(Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_shared.Compilation.Value!.Model!)));
    [Fact] void should_export() => _exitCode.ShouldEqual(0);
}
