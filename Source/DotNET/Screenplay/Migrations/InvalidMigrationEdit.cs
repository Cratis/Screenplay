// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// The exception that is thrown when a migration step returns edits that cannot be written into the text it read.
/// </summary>
/// <param name="message">What is wrong with the edits.</param>
public sealed class InvalidMigrationEdit(string message) : Exception(message);
