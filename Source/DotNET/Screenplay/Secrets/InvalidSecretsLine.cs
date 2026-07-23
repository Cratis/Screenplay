// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// The exception that is thrown when a line in a secrets file is not a valid secret assignment.
/// </summary>
/// <param name="lineNumber">The 1-based line number of the invalid line.</param>
/// <param name="line">The invalid line.</param>
public class InvalidSecretsLine(int lineNumber, string line)
    : Exception($"Invalid secrets line {lineNumber}: '{line}' - expected '<name> = enc:v1:<base64>'");
