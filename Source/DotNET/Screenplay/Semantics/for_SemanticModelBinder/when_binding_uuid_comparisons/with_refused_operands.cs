// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons;

public class with_refused_operands : given.a_uuid_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        var requirements = new List<string>();
        foreach (var op in Operators)
        {
            requirements.AddRange(
            [
                $"requestedBy {op} plant",
                $"plant {op} requestedBy",
                $"View.memberId {op} plant",
                $"raw {op} note",
                $"note {op} raw",
                $"raw {op} 5",
                $"raw {op} true",
                $"optionalMember {op} approvedBy",
                $"approvedBy {op} optionalMember",
                $"members {op} approvedBy",
                $"approvedBy {op} members",
                $"View.owner {op} requestedBy",
                $"requestedBy {op} View.owner",
                $"requestedBy {op} $context.occurred",
                $"$context.occurred {op} requestedBy",
                $"requestedBy {op} unknown",

                // A comparison refused as a whole never has its literal checked.
                $"requestedBy {op} plant and raw {op} other",
                $"optionalMember {op} \"not-a-uuid\""
            ]);
        }

        foreach (var op in new[] { ">", ">=", "<", "<=" })
        {
            requirements.AddRange(
            [
                $"requestedBy {op} approvedBy",
                $"raw {op} other",
                $"raw {op} \"{Canonical}\"",
                $"raw {op} \"not-a-uuid\"",
                $"View.memberId {op} requestedBy"
            ]);
        }

        foreach (var requirement in requirements) _results.Add(Bind(Model(requirement)));

        foreach (var op in Operators)
        {
            foreach (var condition in new[]
            {
                $"View.memberId {op} requestedBy",
                $"requestedBy {op} View.memberId",
                $"requestedBy {op} plant",
                $"optionalMember {op} approvedBy",
                $"requestedBy {op} \"{Canonical}\" and requestedBy {op} approvedBy and requestedBy {op} plant"
            })
            {
                _results.Add(Bind(Model("note != \"\"", condition)));
            }
        }

        _results.Add(Bind(Model("note != \"\"", "requestedBy > approvedBy")));
        _results.Add(Bind("""
            module M
              feature F
                slice Automation React
                  event Started
                    first Uuid
                    second Uuid
                  event Finished
                  reaction Finish
                    when Started
                      first
                      produces Finished
                    where first == second
            """));
    }

    [Fact] void should_examine_every_refused_position() => _results.Count.ShouldEqual(68);
    [Fact] void should_refuse_them_all() => Assert.True(_results.TrueForAll(result => !result.Success), string.Join('\n', _results.Where(result => result.Success).Select(_ => "bound")));
    [Fact] void should_report_only_the_admission_diagnostic() => Assert.True(
        _results.TrueForAll(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax) && result.Diagnostics.All(diagnostic => diagnostic.Code != DiagnosticCodes.InvalidSemanticBinding)),
        string.Join('\n', _results.SelectMany(result => result.Diagnostics).Select(diagnostic => diagnostic.ToString())));
}
