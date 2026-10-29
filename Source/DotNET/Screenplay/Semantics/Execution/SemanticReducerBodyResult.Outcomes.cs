// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

public sealed partial record SemanticReducerBodyResult
{
    /// <summary>Supplies the next complete state.</summary>
    /// <param name="state">The read-model composite.</param>
    /// <returns>The state outcome.</returns>
    public static SemanticReducerBodyResult Next(SemanticValue state) => new(SemanticReducerBodyOutcome.State, state);

    /// <summary>Deletes the read-model instance.</summary>
    /// <returns>The deletion outcome.</returns>
    public static SemanticReducerBodyResult Delete() => new(SemanticReducerBodyOutcome.Delete);

    /// <summary>Refuses execution with a diagnostic.</summary>
    /// <param name="diagnostic">The reason execution is unsupported.</param>
    /// <returns>The unsupported outcome.</returns>
    public static SemanticReducerBodyResult Unsupported(string diagnostic) => new(SemanticReducerBodyOutcome.Unsupported, Diagnostic: diagnostic);
}
