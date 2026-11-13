// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The data and the collection tables a screen container shows to the actions inside it.
/// </summary>
/// <param name="Data">The nearest data bindings.</param>
/// <param name="Tables">The data types the nearest tables list.</param>
internal sealed record ActionSubjectVisibility(IReadOnlyList<ScreenDataSyntax> Data, IReadOnlyList<string> Tables)
{
    /// <summary>
    /// Gets the visibility of a container that has no data and no table above it.
    /// </summary>
    internal static ActionSubjectVisibility None { get; } = new([], []);

    /// <summary>
    /// Gets the visibility inside a container: its own data and tables where it has any, otherwise those it inherits.
    /// </summary>
    /// <param name="directives">The directives of the container.</param>
    /// <param name="inherited">The visibility the container inherits.</param>
    /// <returns>The visibility inside the container.</returns>
    internal static ActionSubjectVisibility Within(IEnumerable<ScreenDirectiveSyntax> directives, ActionSubjectVisibility inherited)
    {
        var nodes = directives.ToArray();
        var local = nodes.OfType<ScreenDataSyntax>().ToArray();
        var tables = nodes.OfType<ScreenTableSyntax>().Select(table => table.Target).ToArray();
        return new(local.Length == 0 ? inherited.Data : local, tables.Length == 0 ? inherited.Tables : tables);
    }

    /// <summary>
    /// Gets the data an action acts on. An explicit 'for item' acts on the one collection data a table in scope shows; anything else keeps the nearest data.
    /// </summary>
    /// <param name="authored">The subject the author wrote, if any.</param>
    /// <returns>The data the action can act on.</returns>
    internal IReadOnlyList<ScreenDataSyntax> ItemSubjects(ScreenActionSubjectKind? authored)
    {
        if (authored != ScreenActionSubjectKind.Item || Data.Count <= 1) return Data;
        var shown = Data.Where(data => data.Type.IsCollection && Tables.Contains(data.Type.Name)).ToArray();
        return shown.Length == 1 ? shown : Data;
    }
}
