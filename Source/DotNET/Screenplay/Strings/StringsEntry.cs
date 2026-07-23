// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings;

/// <summary>
/// Represents a single localized string in a <see cref="StringsFile"/>.
/// </summary>
/// <param name="Key">The dotted key of the string, such as <c>invoices.title</c>.</param>
/// <param name="Value">The value of the string, with <c>{placeholder}</c> tokens kept verbatim.</param>
public record StringsEntry(string Key, string Value);
