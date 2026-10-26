// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes how a normalised semantic model differs from the expected one at a path.
/// </summary>
internal enum SemanticDifferenceKind
{
    /// <summary>
    /// The expected model has the value and the actual model does not.
    /// </summary>
    Missing = 0,

    /// <summary>
    /// The actual model has a value the expected model does not.
    /// </summary>
    Unexpected = 1,

    /// <summary>
    /// Both models have a value and they differ.
    /// </summary>
    Changed = 2,
}
