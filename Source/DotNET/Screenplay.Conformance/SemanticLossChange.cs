// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes the kind of loss a ledger entry accepts.
/// </summary>
internal enum SemanticLossChange
{
    /// <summary>
    /// A value of the expected model is missing from the round-tripped one.
    /// </summary>
    Missing = 0,

    /// <summary>
    /// The round-tripped model has a value the expected model does not.
    /// </summary>
    Unexpected = 1,

    /// <summary>
    /// A value differs between the expected and the round-tripped model.
    /// </summary>
    Changed = 2,

    /// <summary>
    /// The recovered source did not compile, so nothing could be compared.
    /// </summary>
    Uncompilable = 3,

    /// <summary>
    /// The round trip reported a diagnostic.
    /// </summary>
    Diagnostic = 4,
}
