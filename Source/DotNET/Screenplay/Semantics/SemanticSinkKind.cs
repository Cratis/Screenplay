// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The kind of thing a <see cref="SemanticSink"/> stores.
/// </summary>
public enum SemanticSinkKind
{
    /// <summary>The sink stores read models.</summary>
    ReadModels = 0,

    /// <summary>The sink stores graphs.</summary>
    Graphs = 1
}
