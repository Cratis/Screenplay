// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents a reducer's next state, deletion, or inability to execute. Any diagnostic makes the result unsupported.
/// </summary>
/// <param name="Outcome">The explicit transition outcome; the default is unsupported.</param>
/// <param name="State">The complete next read-model composite, only for a state outcome.</param>
/// <param name="Diagnostic">The execution diagnostic, not model output.</param>
public sealed partial record SemanticReducerBodyResult(SemanticReducerBodyOutcome Outcome, SemanticValue? State = null, string? Diagnostic = null);
