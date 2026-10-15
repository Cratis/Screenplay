// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kind of literal a sample data field value is.
/// </summary>
public enum SampleDataValueKind
{
    /// <summary>A quoted string.</summary>
    Text = 0,

    /// <summary>A number, kept as its authored token.</summary>
    Number = 1,

    /// <summary>The literal <c>true</c> or <c>false</c>.</summary>
    Flag = 2,

    /// <summary>The <c>null</c> literal.</summary>
    Null = 3
}
