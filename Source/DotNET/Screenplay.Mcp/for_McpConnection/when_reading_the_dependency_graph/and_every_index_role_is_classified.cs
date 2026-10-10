// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Dependencies;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public partial class and_every_index_role_is_classified : given.a_graph_query
{
    string[] _roles;
    bool _allClassified;

    void Establish()
    {
        var text = File.ReadAllText(Path.Combine(Root(), "Source/DotNET/Screenplay/Indexing/ReferenceKinds.cs"));
        _roles = [.. IndexRoles().Matches(text).Select(match => match.Groups[1].Value).Distinct(), "givenEvent", "whenAppendedEvent", "thenEvent", "givenReadModel", "thenReadModel", "thenAbsentReadModel"];
    }
    void Because() => _allClassified = _roles.All(SliceReferences.Classifications.ContainsKey);

    [Fact] void should_classify_every_index_role_or_explicitly_exclude_it() => _allClassified.ShouldBeTrue();

    [GeneratedRegex(",\\s*\"([^\"\\r\\n]+)\"\\)", RegexOptions.None, 1000)]
    private static partial Regex IndexRoles();
}
