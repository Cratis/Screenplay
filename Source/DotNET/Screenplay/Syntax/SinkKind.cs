// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kind of thing a <see cref="SinkSyntax">sink</see> stores.
/// </summary>
public enum SinkKind
{
    /// <summary>The sink stores read models: <c>sink &lt;Name&gt; for readmodels</c>.</summary>
    ReadModels = 0,

    /// <summary>The sink stores graphs: <c>sink &lt;Name&gt; for graphs</c>.</summary>
    Graphs = 1
}
