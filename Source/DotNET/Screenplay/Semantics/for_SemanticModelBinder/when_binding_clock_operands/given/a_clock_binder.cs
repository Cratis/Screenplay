// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands.given;

public class a_clock_binder : for_SemanticModelBinder.given.a_semantic_binder
{
    protected static string Source(string commandBody, string applicationHeader = "", string extra = "") =>
        $"{applicationHeader}module M\n  feature F\n    slice StateChange S\n      event Done\n      command C\n        id Uuid identifier\n        dueDate Date\n        occurred DateTime\n{commandBody}{extra}";

    private protected CompilationResult<ClockOperandsBinding> BindClock(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = _binder.Bind("Projects", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        var model = result.Value?.Model;
        return new(model is null ? null : new(model.Application, model.LanguageVersion, model.SemanticVersion), result.Diagnostics)
        {
            ImplementationRequirements = result.ImplementationRequirements,
            TypedContextDescriptors = result.TypedContextDescriptors
        };
    }

    private protected static SemanticCommand Command(ClockOperandsBinding binding) => binding.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
