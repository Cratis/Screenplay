// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ModelRootDocumentKeys;

public class when_getting_a_key : for_ScreenplayModelRoot.given.a_persisted_model_root
{
    string _key = null!;

    void Because() => _key = ModelRootDocumentKeys.For(PortablePlayPath.Parse("application.play"));

    [Fact] void should_match_model_root_discovery() => _key.ShouldEqual(new ModelRootDirectory(RootPath).Read().Single().StableKey);
    [Fact] void should_keep_the_existing_nfc_bootstrap_key() => _key.ShouldEqual("mcp:10C84FFF43AC4FD8F4D105C92186433FEF3D7C4FB23501AAA828310CE5F25B10");
}
