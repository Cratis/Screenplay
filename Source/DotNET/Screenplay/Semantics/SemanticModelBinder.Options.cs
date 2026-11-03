// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    /// <summary>
    /// Binds with explicitly requested metadata, without changing the historical default overload.
    /// Property-order requests are refused until a later version admits them.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <param name="document">The parsed source.</param>
    /// <param name="documents">The authoritative document set.</param>
    /// <param name="options">The requested optional metadata.</param>
    /// <returns>The compilation, or a typed refusal with no executable value.</returns>
    public SemanticBindingOptionsResult Bind(string applicationName, ApplicationSyntax document, SemanticDocumentSet documents, SemanticBindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = Bind(applicationName, document, documents);
        if (!result.Success || !options.PreserveReadModelPropertyOrder) return new(result);
        return new(result with { Value = null }, new(result.Value!.Model.SemanticVersion));
    }
}
