// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines how many rows a <see cref="SemanticGraphCollect"/> assigns.
/// </summary>
public enum SemanticGraphCollectCardinality
{
    /// <summary>A list of rows, empty when nothing is reached.</summary>
    Many = 0,

    /// <summary>One row, or null when nothing is reached.</summary>
    One = 1
}
