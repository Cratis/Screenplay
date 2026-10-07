// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_Samples;

public class when_declaring_dependencies_in_library : Specification
{
    ApplicationSyntax _before;
    ApplicationSyntax _after;

    void Because()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play"));
        const string path = "Samples/Library/library.play";
        var syntax = new ScreenplayCompiler().Parse(source, path).Value!;
        var changed = syntax with
        {
            Modules = syntax.Modules.Select(module => module with
            {
                Features = module.Features.Select(feature => feature.Name == "Loans" ? feature with
                {
                    DependsOn = [new("Catalog", feature.Location), new("Members", feature.Location)]
                } : feature).ToArray()
            }).ToArray()
        };
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var documents = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("library"), "library", path, source)], catalog);
        var binder = new SemanticModelBinder();
        var baseline = binder.Bind("Library", syntax, documents);
        var declared = binder.Bind("Library", changed, documents);
        // Library does not bind today: preserve its dispositions and compare syntax modulo metadata.
        baseline.Success.ShouldBeFalse();
        declared.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)).ShouldEqual(baseline.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)));
        _before = syntax;
        _after = changed with
        {
            Modules = [.. changed.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with { DependsOn = [] })]
            })]
        };
    }

    [Fact] void should_preserve_sample_syntax_modulo_the_authoring_declarations() => SyntaxJson.StructurallyEqual(_before, _after).ShouldBeTrue();

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
