// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings;

/// <summary>
/// The exception that is thrown when a line in a strings file is not a valid string assignment.
/// </summary>
/// <param name="lineNumber">The 1-based line number of the invalid line.</param>
/// <param name="line">The invalid line.</param>
public class InvalidStringsLine(int lineNumber, string line)
    : Exception($"Invalid strings line {lineNumber}: '{line}' - expected '<key.path> = \"<value>\"'");
