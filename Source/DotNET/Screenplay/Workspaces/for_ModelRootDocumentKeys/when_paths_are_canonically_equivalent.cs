// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ModelRootDocumentKeys;

public class when_paths_are_canonically_equivalent : for_ScreenplayModelRoot.given.a_persisted_model_root
{
    string _composed = null!;
    string _decomposed = null!;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        File.WriteAllText(Path.Combine(RootPath, "Cafe\u0301.play"), Source);
    }

    void Because()
    {
        _composed = ModelRootDocumentKeys.For(PortablePlayPath.Parse("Caf\u00e9.play"));
        _decomposed = ModelRootDocumentKeys.For(PortablePlayPath.Parse("Cafe\u0301.play"));
    }

    [Fact] void should_normalize_unicode_before_hashing() => _decomposed.ShouldEqual(_composed);
    [Fact] void should_match_discovery_for_decomposed_filenames() => new ModelRootDirectory(RootPath).Read().Single().StableKey.ShouldEqual(_composed);
}
