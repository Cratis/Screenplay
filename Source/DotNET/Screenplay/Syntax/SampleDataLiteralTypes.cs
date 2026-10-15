// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

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
        ["integer"] = SampleDataLiteralType.WholeNumber,
        ["int"] = SampleDataLiteralType.WholeNumber,
        ["int16"] = SampleDataLiteralType.WholeNumber,
        ["int32"] = SampleDataLiteralType.WholeNumber,
        ["int64"] = SampleDataLiteralType.WholeNumber,
        ["long"] = SampleDataLiteralType.WholeNumber,
        ["short"] = SampleDataLiteralType.WholeNumber,
        ["decimal"] = SampleDataLiteralType.Number,
        ["double"] = SampleDataLiteralType.Number,
        ["float"] = SampleDataLiteralType.Number,
        ["number"] = SampleDataLiteralType.Number,
        ["single"] = SampleDataLiteralType.Number,
        ["boolean"] = SampleDataLiteralType.Flag,
        ["bool"] = SampleDataLiteralType.Flag
    };

    /// <summary>
    /// Gets the literal type for a primitive type name.
    /// </summary>
    /// <param name="name">The primitive type name.</param>
    /// <returns>The literal type, or <c>null</c> when the name is not a known primitive.</returns>
    public static SampleDataLiteralType? Of(string name) => _types.TryGetValue(name, out var type) ? type : null;
}
