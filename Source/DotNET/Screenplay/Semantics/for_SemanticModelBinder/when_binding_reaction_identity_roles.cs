// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_reaction_identity_roles : given.a_semantic_binder
{
    [Theory]
    [InlineData("", "")]
    [InlineData(" role \"z\" and role \"A\" and role \"a\"", "A,a,z")]
    void should_admit_and_sort_the_exact_roles(string roles, string expected)
    {
        var result = Bind(Source($"runs as system{roles}"));
        result.Success.ShouldBeTrue();
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        var reaction = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        reaction.RunsAs!.Kind.ShouldEqual(SemanticReactionIdentityKind.System);
        string.Join(',', reaction.RunsAs.Roles).ShouldEqual(expected);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
    }

    [Theory]
    [InlineData("person", "A", "B")]
    [InlineData("system", "", "B")]
    [InlineData("system", "A", "A")]
    void should_refuse_malformed_programmatic_identity(string kind, string first, string second)
    {
        var source = Source("runs as system");
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var reaction = slice.Reactions.Single();
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Reactions = [reaction with { RunsAs = new(kind, [first, second], reaction.Location) }] }] }] }] };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("document"), "document", "application.play", source);
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
    }

    static string Source(string identity) => $"module M\n  feature F\n    slice Automation S\n      event E\n      reaction R\n        {identity}\n        when E";
}
