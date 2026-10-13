// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents recovery delivery of one existing given fact to one reaction without appending it again.
/// </summary>
/// <param name="Reaction">The reaction receiving the fact.</param>
/// <param name="Given">The zero-based position in the specification's given events.</param>
public sealed record SemanticSpecificationRedelivery(SemanticId Reaction, int Given);
