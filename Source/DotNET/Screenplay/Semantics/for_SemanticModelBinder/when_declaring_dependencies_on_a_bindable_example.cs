// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_declaring_dependencies_on_a_bindable_example : Specification
{
    byte[] _before;
    byte[] _after;

    void Because()
    {
        const string path = "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v2/source/RegisterProject.play";
        var source = File.ReadAllText(Path.Combine(Root(), path));
        var syntax = new ScreenplayCompiler().Parse(source, path).Value!;
        var module = syntax.Modules.Single();
        var changed = syntax with
        {
            Modules = [module with
            {
                DependsOn = [new("PlannedModule", module.Location)],
                Features = [.. module.Features.Select(feature => feature with { DependsOn = [new("PlannedFeature", feature.Location)] })]
            }]
        };
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var documents = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("example"), "example", path, source)], catalog);
        var binder = new SemanticModelBinder();
        var baseline = binder.Bind("Projects", syntax, documents);
        var declared = binder.Bind("Projects", changed, documents);
        Assert.True(baseline.Success, string.Join('\n', baseline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(declared.Success, string.Join('\n', declared.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _before = SemanticModelSerializer.Serialize(baseline.Value!.Model);
        _after = SemanticModelSerializer.Serialize(declared.Value!.Model);
    }

    [Fact] void should_not_change_executable_model_bytes_revisions_or_identities() => _after.ShouldEqual(_before);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
