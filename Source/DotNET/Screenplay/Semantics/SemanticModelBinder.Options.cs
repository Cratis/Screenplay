// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    /// <summary>
    /// Binds with explicitly requested metadata, without changing the historical default overload.
    /// Property order is admitted only when removing it leaves the already validated ESM 37.0 base.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <param name="document">The parsed source.</param>
    /// <param name="documents">The authoritative document set.</param>
    /// <param name="options">The requested optional metadata.</param>
    /// <returns>The compilation, or a typed refusal with no executable value.</returns>
    public SemanticBindingOptionsResult Bind(string applicationName, ApplicationSyntax document, SemanticDocumentSet documents, SemanticBindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var assessmentYear = _utcAssessmentYear ?? DateTimeOffset.UtcNow.Year;
        var result = new SemanticModelBinder(assessmentYear).Bind(applicationName, document, documents);
        if (!result.Success || !options.PreserveReadModelPropertyOrder) return new(result);
        var compilation = result.Value!;
        var application = SemanticReadModelPropertyOrder.Apply(compilation.Model.Application);
        if (!SemanticReadModelPropertyOrder.IsUsed(application)) return new(result);
        if (compilation.Model.SemanticVersion != SemanticVersion.V37)
        {
            return new(result with { Value = null }, new(compilation.Model.SemanticVersion));
        }
        var model = ExecutableSemanticModel.Create(LanguageVersion.V37_1, SemanticVersion.V37_1, application, assessmentYear);
        var updated = SemanticCompilation.Create(model, compilation.Documents, compilation.SourceMap).WithSpecificationOrigins(compilation.SpecificationOrigins);
        return new(result with { Value = updated });
    }
}
