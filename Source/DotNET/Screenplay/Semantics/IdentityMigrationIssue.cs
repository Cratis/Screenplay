// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Semantics;

sealed record IdentityMigrationIssue(ImmutableArray<string> Arguments, SemanticAddress Address)
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    internal object Describe() => new { Arguments, Address = new { Address.Kind, Address.Parts } };

    internal static void Reject(string message, IEnumerable<IdentityMigrationIssue> issues)
    {
        var reported = issues.DistinctBy(issue => (string.Join(',', issue.Arguments), issue.Address)).ToImmutableArray();
        if (!reported.IsEmpty)
        {
            throw new InvalidSemanticContract($"{message} Identity migration issues: {JsonSerializer.Serialize(reported.Select(issue => issue.Describe()), _options)}")
            {
                IdentityMigrationIssues = reported
            };
        }
    }
}
