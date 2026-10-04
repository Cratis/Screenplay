// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus.given;

internal static class v6_regression_models
{
    internal static ExecutableSemanticModel Compile(string source)
    {
        const string StableKey = "v6-regression";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Billing.play", source);
        var result = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        result.Success.ShouldBeTrue();
        return result.Value!.Model;
    }

    internal static ExecutableSemanticModel ChangeSpecification(ExecutableSemanticModel model, Func<SemanticSpecification, SemanticSpecification> change)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [change(slice.Specifications.Single())] }] }] }]
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, application);
    }

    internal static IEnumerable<SemanticSpecificationRun> Runs(ExecutableSemanticModel model)
    {
        var restored = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));
        restored.Revision.ShouldEqual(model.Revision);
        foreach (var input in new[] { model, restored })
        {
            var plan = SemanticExecutionPlan.Compile(input).Plan!;
            yield return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        }
    }
}
