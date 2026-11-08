// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a named sink an application declares: a logical place read models or graphs are stored, which a
/// deployment maps to real storage. Realization metadata carried by <see cref="SemanticCompilation.Sinks"/>, never
/// part of the executable semantic model.
/// </summary>
/// <param name="Name">The logical name of the sink, as declared.</param>
/// <param name="Kind">The <see cref="SemanticSinkKind"/> of thing the sink stores.</param>
public sealed record SemanticSink(string Name, SemanticSinkKind Kind);
