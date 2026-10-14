// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when a regex construct is outside fragment R.
/// </summary>
/// <param name="construct">The excluded construct.</param>
public sealed class OutsideRegexFragment(string construct) : Exception("outside fragment R: " + construct);
