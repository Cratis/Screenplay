// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Tool;

/// <summary>
/// The exception that is thrown when a resolved validation gate holds check names the check parser does not accept.
/// </summary>
/// <param name="names">The resolved check names.</param>
[SuppressMessage("Design", "CA1064", Justification = "Signals a defect inside the tool; it never crosses a public API.")]
sealed class UnrecognizedValidationGateChecks(IEnumerable<string> names)
    : Exception($"The resolved validation gate holds check names the parser does not accept: {string.Join(", ", names)}.");
