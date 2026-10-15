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
    Integer = 1,

    /// <summary>Any number.</summary>
    Number = 2,

    /// <summary><c>true</c> or <c>false</c>.</summary>
    Boolean = 3,

    /// <summary>A structured value, which a row cannot hold.</summary>
    Unsupported = 4
}

/// <summary>
/// Maps primitive type names to the kind of literal a sample data row writes for them.
/// </summary>
public static class SampleDataLiteralTypes
{
    static readonly Dictionary<string, SampleDataLiteralType> _types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = SampleDataLiteralType.Text,
        ["uuid"] = SampleDataLiteralType.Text,
        ["guid"] = SampleDataLiteralType.Text,
        ["date"] = SampleDataLiteralType.Text,
        ["dateonly"] = SampleDataLiteralType.Text,
        ["datetime"] = SampleDataLiteralType.Text,
        ["datetimeoffset"] = SampleDataLiteralType.Text,
        ["time"] = SampleDataLiteralType.Text,
        ["timeonly"] = SampleDataLiteralType.Text,
        ["timestamp"] = SampleDataLiteralType.Text,
        ["uri"] = SampleDataLiteralType.Text,
        ["url"] = SampleDataLiteralType.Text,
        ["integer"] = SampleDataLiteralType.Integer,
        ["int"] = SampleDataLiteralType.Integer,
        ["int16"] = SampleDataLiteralType.Integer,
        ["int32"] = SampleDataLiteralType.Integer,
        ["int64"] = SampleDataLiteralType.Integer,
        ["long"] = SampleDataLiteralType.Integer,
        ["short"] = SampleDataLiteralType.Integer,
        ["decimal"] = SampleDataLiteralType.Number,
        ["double"] = SampleDataLiteralType.Number,
        ["float"] = SampleDataLiteralType.Number,
        ["number"] = SampleDataLiteralType.Number,
        ["single"] = SampleDataLiteralType.Number,
        ["boolean"] = SampleDataLiteralType.Boolean,
        ["bool"] = SampleDataLiteralType.Boolean
    };

    /// <summary>
    /// Gets the literal type for a primitive type name.
    /// </summary>
    /// <param name="name">The primitive type name.</param>
    /// <returns>The literal type, or <c>null</c> when the name is not a known primitive.</returns>
    public static SampleDataLiteralType? Of(string name) => _types.TryGetValue(name, out var type) ? type : null;
}
