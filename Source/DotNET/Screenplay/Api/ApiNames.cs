// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Spells model names the way a rendered application exposes them over HTTP.
/// </summary>
public static class ApiNames
{
    /// <summary>
    /// Converts a model name to the PascalCase type or member name a renderer generates for it.
    /// </summary>
    /// <param name="name">The authored name.</param>
    /// <returns>The words of the name joined with an upper-case first letter each.</returns>
    public static string Pascal(string name)
    {
        var result = string.Concat(name.Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        if (result.Length == 0) return "Item";
        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }

    /// <summary>
    /// Converts a model name to the camelCase name it carries in JSON.
    /// </summary>
    /// <param name="name">The authored name.</param>
    /// <returns>The PascalCase name with a lower-case first letter.</returns>
    public static string Camel(string name)
    {
        var pascal = Pascal(name);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    /// <summary>
    /// Converts a model name to a route segment.
    /// </summary>
    /// <param name="name">The authored name.</param>
    /// <returns>The lower-case segment, with a dash before every upper-case letter but the first and for every underscore.</returns>
    public static string Segment(string name)
    {
        var pascal = Pascal(name);
        var result = new StringBuilder();
        for (var index = 0; index < pascal.Length; index++)
        {
            var character = pascal[index];
            if (character == '_')
            {
                result.Append('-');
                continue;
            }

            if (char.IsUpper(character) && index > 0 && pascal[index - 1] != '_') result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }

    /// <summary>
    /// Joins a route prefix and the names of its segments into a route.
    /// </summary>
    /// <param name="prefix">The route prefix.</param>
    /// <param name="names">The names that become segments.</param>
    /// <returns>The route, starting with a slash.</returns>
    public static string Route(string prefix, IEnumerable<string> names) =>
        "/" + string.Join('/', new[] { prefix.Trim('/') }.Where(part => part.Length > 0).Concat(names.Select(Segment)));
}
