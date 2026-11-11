// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// The exception that is thrown when a reaction reads a value of an ability that the specification gives no answer for.
/// </summary>
/// <param name="message">The message describing the missing answer.</param>
public class SemanticAbilityAnswerMissing(string message) : Exception(message);
