// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events.given;

public class a_possible_events_binder : for_SemanticModelBinder.given.a_semantic_binder
{
    protected const string Prefix = "module M\n  feature F\n    slice StateChange S\n      event Created\n        value String\n      event Removed\n      command C\n";
    protected const string Properties = "        id Uuid identifier\n        other Uuid\n        ids Uuid[]\n";
    protected const string Pending = "        handler\n          may produce\n            Created\n            Removed for one of ids\n          implementation\n            hint \"Return only the chosen events\"\n";
    protected const string Source = Prefix + Properties + Pending;

    private protected CompilationResult<PossibleEventsBinding> BindPossible(string source, ImmutableDictionary<string, string>? attachments = null, Func<ApplicationSyntax, ApplicationSyntax>? transform = null)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var syntax = transform is null ? parsed.Value! : transform(parsed.Value!);
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        return _binder.BindForPossibleEvents("Projects", syntax, SemanticDocumentSet.Create([document], catalog, attachments));
    }

    private protected static SemanticCommand Command(PossibleEventsBinding binding) => binding.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    protected static ApplicationSyntax TransformCommands(ApplicationSyntax application, Func<CommandSyntax, CommandSyntax> transform) => application with
    {
        Modules = [.. application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(transform)] })]
            })]
        })]
    };
}
