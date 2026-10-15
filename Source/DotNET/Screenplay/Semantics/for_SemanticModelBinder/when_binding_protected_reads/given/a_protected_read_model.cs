// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.given;

public class a_protected_read_model : for_SemanticModelBinder.given.a_semantic_binder
{
    public const string Source = """
        command reads protected
        module M
          feature F
            slice StateChange Write
              event Recorded
                value String
              event Removed
              command Update
                id String identifier
                value String
                reads View by id
                validate
                  require View.value != ""
                produces Recorded
                  for id
                  value = View.value
            slice StateView Lookup
              readmodel View
                id String key
                value String
              projection Build => View
                from Recorded
                  id = $eventSourceId
                  value = value
                remove with Removed
              query Get => View optional
                by id String
        """;

    protected CompilationResult<SemanticCompilation> BindCommandSyntax(Func<CommandSyntax, CommandSyntax> transform)
    {
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        syntax = syntax with
        {
            Modules = [.. syntax.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(transform)] })]
                })]
            })]
        };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", Source);
        return _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
    }

    protected CompilationResult<SemanticCompilation> BindReactionSyntax(string source, Func<ReactionSyntax, ReactionSyntax> transform)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var syntax = parsed.Value! with
        {
            Modules = [.. parsed.Value!.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with { Reactions = [.. (slice.Reactions ?? []).Select(transform)] })]
                })]
            })]
        };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        return _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
    }

    protected static void Refusal(CompilationResult<SemanticCompilation> result, SemanticDecisionReadRefusal reason)
    {
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == Diagnostics.DiagnosticCodes.UnprotectableDecisionRead && diagnostic.Message.Contains(reason.ToString(), StringComparison.Ordinal));
    }

    protected static SemanticCommand Command(ExecutableSemanticModel model) =>
        model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Write").Commands.Single();

    protected static SemanticReadModel View(ExecutableSemanticModel model) =>
        model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Lookup").ReadModels.Single();
}
