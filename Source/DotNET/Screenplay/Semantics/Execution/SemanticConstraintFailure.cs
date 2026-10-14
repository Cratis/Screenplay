// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents one violated append constraint in protected command result order.
/// </summary>
/// <param name="Name">The violated constraint's name.</param>
/// <param name="Message">Its rejection message verbatim.</param>
public sealed record SemanticConstraintFailure(string Name, string Message);
