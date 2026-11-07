// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// The exception that is thrown when the steps cannot move a document between two versions: no step covers part of the
/// way, two steps cover the same part, or a step goes backwards.
/// </summary>
/// <param name="message">Why the plan is invalid.</param>
public sealed class InvalidMigrationPlan(string message) : Exception(message);
