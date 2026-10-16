// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons.given;

public class a_uuid_comparison : for_SemanticModelBinder.given.a_semantic_binder
{
    public const string Canonical = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    protected static string Model(string require, string? when = null)
    {
        var conditional = when is null ? string.Empty : $"\n        produces when {when}\n          Noted\n            for id";
        return $$"""
            command reads protected
            concept MemberId : Uuid
            concept PlantId : Uuid
            module M
              feature F
                slice StateChange Write
                  event Recorded
                    memberId MemberId
                  event Noted
                  event Removed
                  command Update
                    id String identifier
                    requestedBy MemberId
                    approvedBy MemberId
                    plant PlantId
                    raw Uuid
                    other Uuid
                    note String
                    optionalMember MemberId optional
                    members MemberId[]
                    reads View by id
                    validate
                      require {{require}}
                    produces Recorded
                      for id
                      memberId = requestedBy{{conditional}}
                slice StateView Lookup
                  readmodel View
                    id String key
                    memberId MemberId
                    owner MemberId optional
                  projection Build => View
                    from Recorded
                      id = $eventSourceId
                      memberId = memberId
                    remove with Removed
                  query Get => View optional
                    by id String
            """;
    }

    protected static string[] Operators => ["==", "!="];

    protected new CompilationResult<SemanticCompilation> Bind(string source, string? syntaxPath = null, string displayPath = "application.play")
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return base.Bind(source, syntaxPath, displayPath);
    }
}
