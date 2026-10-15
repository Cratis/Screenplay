// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kind of literal a read model property accepts as a sample data value.
/// </summary>
public enum SampleDataLiteralType
{
    /// <summary>A quoted string: text, identifiers, dates, times and enum members.</summary>
    Text = 0,

    /// <summary>A whole number.</summary>
    WholeNumber = 1,

    /// <summary>Any number.</summary>
    Number = 2,

    /// <summary><c>true</c> or <c>false</c>.</summary>
    Flag = 3,

    /// <summary>A structured value, which a row cannot hold.</summary>
    Unsupported = 4
}

