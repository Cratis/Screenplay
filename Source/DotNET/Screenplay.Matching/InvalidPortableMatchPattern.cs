// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

/// <summary>
/// A pattern is outside the portable dialect or has invalid syntax.
/// </summary>
/// <param name="message">The admission reason.</param>
public sealed class InvalidPortableMatchPattern(string message) : Exception(message);
