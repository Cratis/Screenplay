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
    string[] _targets;
    IEnumerable<(string Code, string Message)> _beforeDiagnostics;
    IEnumerable<(string Code, string Message)> _afterDiagnostics;
    IEnumerable<(string Code, string Message)> _beforeBindingDiagnostics;
    IEnumerable<(string Code, string Message)> _afterBindingDiagnostics;
    bool _baselineBinds;

    void Because()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play"));
        const string path = "Samples/Library/library.play";
        var declaredSource = source.Replace("  feature Loans", "  feature Loans\n    depends on Catalog\n    depends on Members", StringComparison.Ordinal);
        var compiler = new ScreenplayCompiler();
        var baselineSyntax = compiler.Parse(source, path);
        var declaredSyntax = compiler.Parse(declaredSource, path);
        var changed = declaredSyntax.Value!;
        _targets = [.. changed.Modules.SelectMany(module => module.Features).Single(feature => feature.Name == "Loans").DependsOn.Select(dependency => dependency.Target)];
        _beforeDiagnostics = baselineSyntax.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message));
        _afterDiagnostics = declaredSyntax.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var baselineDocuments = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("library"), "library", path, source)], catalog);
        var declaredDocuments = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("library"), "library", path, declaredSource)], catalog);
        var binder = new SemanticModelBinder();
        var baseline = binder.Bind("Library", baselineSyntax.Value!, baselineDocuments);
        var declared = binder.Bind("Library", changed, declaredDocuments);
        // Library does not bind today: preserve its dispositions and compare parsed syntax modulo metadata.
        _baselineBinds = baseline.Success;
        _beforeBindingDiagnostics = baseline.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message));
        _afterBindingDiagnostics = declared.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message));
        _before = baselineSyntax.Value!;
        _after = changed with
        {
            Modules = [.. changed.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with { DependsOn = [] })]
            })]
        };
    }

    [Fact] void should_parse_the_added_declarations() => _targets.ShouldEqual(new[] { "Catalog", "Members" });
    [Fact] void should_preserve_sample_syntax_modulo_the_authoring_declarations() => SyntaxJson.StructurallyEqual(_before, _after).ShouldBeTrue();
    [Fact] void should_add_no_parser_diagnostics_for_valid_sibling_targets() => _afterDiagnostics.ShouldEqual(_beforeDiagnostics);
    [Fact] void should_preserve_the_existing_binding_failure() => _baselineBinds.ShouldBeFalse();
    [Fact] void should_preserve_binding_diagnostics() => _afterBindingDiagnostics.ShouldEqual(_beforeBindingDiagnostics);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
