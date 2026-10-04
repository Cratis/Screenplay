// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_resolving_operation_specification_dependencies
{
    const string Operation = "      operation Send\n        uses Mailer\n";
    const string Event = "      event Send\n";
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("sibling", "Send", "ambiguous", 2)]
    [InlineData("shadow", "Send", "wrongKind", 1)]
    [InlineData("unique", "Send", "resolved", 1)]
    [InlineData("collision", "Send", "ambiguous", 2)]
    [InlineData("sibling", "A.Send", "resolved", 1)]
    [InlineData("inline", "Send", "resolved", 1)]
    [InlineData("sibling", "Opaque.Send", "unresolved", 0)]
    public void should_resolve_all_step_kinds_before_requiring_an_operation(string scenario, string reference, string expected, int count)
    {
        var declarations = scenario switch
        {
            "sibling" => "    slice StateChange A\n" + Operation + "    slice StateChange B\n" + Event,
            "shadow" => "    slice StateChange A\n" + Operation,
            _ => string.Empty
        };
        var local = scenario switch
        {
            "shadow" => Event,
            "unique" => Operation,
            "collision" => Event + Operation,
            _ => string.Empty
        };
        var production = scenario == "inline" ? "        produces operation Send\n          uses Mailer\n" : string.Empty;
        var source = "system Mailer\nimport Opaque.Send\nmodule M\n  feature F\n" + declarations + "    slice StateChange C\n" + local +
            "      command Ask\n" + production + "      specification T\n        given operation " + reference + " fails\n        when Ask\n        then operation " + reference + "\n        then compensated " + reference + "\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        var arguments = JsonSerializer.SerializeToElement(new { address = "M.F.C.T", kind = "Specification", direction = "outgoing" });
        var result = JsonSerializer.SerializeToElement(McpDependencyQueries.Read(snapshot, arguments), Options);
        var rows = result.GetProperty("page").GetProperty("items").EnumerateArray()
            .Where(row => row.GetProperty("reference").GetProperty("kinds").EnumerateArray().Any(kind => kind.GetString() == "Operation")).ToArray();
        rows.Length.ShouldEqual(3);
        rows.Select(row => row.GetProperty("reference").GetProperty("role").GetString()).Order(StringComparer.Ordinal)
            .SequenceEqual(["givenOperationFailure", "thenCompensated", "thenOperation"]).ShouldBeTrue();
        foreach (var row in rows)
        {
            row.GetProperty("resolution").GetString().ShouldEqual(expected);
            var targets = row.GetProperty("targets").EnumerateArray().ToArray();
            targets.Length.ShouldEqual(count);
            if (expected == "ambiguous") targets.Select(target => target.GetProperty("kind").GetString()).Order(StringComparer.Ordinal).SequenceEqual(["Event", "Operation"]).ShouldBeTrue();
            if (expected == "wrongKind") targets.Single().GetProperty("kind").GetString().ShouldEqual("Event");
            if (expected == "resolved") targets.Single().GetProperty("kind").GetString().ShouldEqual("Operation");
            row.TryGetProperty("semanticId", out _).ShouldBeFalse();
        }
        var specification = snapshot.Index.Find("M.F.C.T", "Specification").Single();
        snapshot.Index.Readiness.ExecutionReadiness(specification.Syntax).ShouldContain("ESM v9 (PLAY0268)");
    }

    [Theory]
    [InlineData("operations", false)]
    [InlineData("operations", true)]
    [InlineData("slice", false)]
    [InlineData("slice", true)]
    [InlineData("command", false)]
    [InlineData("command", true)]
    public void should_preserve_physical_declaration_and_owner_collision_evidence(string scenario, bool reverse)
    {
        const string prefix = "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n";
        var first = prefix + (scenario == "command" ? "      command C\n        produces operation Send\n          uses Mailer\n      command C\n" : Operation);
        var second = scenario == "operations" ? prefix + Operation : prefix + "      command Other\n";
        const string referring = "module M\n  feature F\n    slice StateChange T\n      command Ask\n        produces S.Send\n      specification Check\n        given operation S.Send fails\n        when Ask\n        then operation S.Send\n        then compensated S.Send\n";
        WorkspaceDocument[] documents = scenario == "command" ? [Document("one.play", first), Document("reference.play", referring)]
            : [Document("one.play", first), Document("two.play", second), Document("reference.play", referring)];
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeFalse();
        var references = snapshot.Index.References.Where(reference => reference.Name == "S.Send").ToArray();
        references.Length.ShouldEqual(4);
        foreach (var reference in references)
        {
            var edge = new McpReferenceEdge(reference, snapshot.Index.Resolve(reference));
            edge.Resolution.ShouldEqual("ambiguous");
            edge.Targets.Count(target => target.Kind == "Operation").ShouldEqual(scenario == "operations" ? 2 : 1);
            edge.Targets.Select(target => target.Location.Path).Contains("one.play").ShouldBeTrue();
            if (scenario != "command") edge.Targets.Select(target => target.Location.Path).Contains("two.play").ShouldBeTrue();
            var serialized = JsonSerializer.SerializeToElement(edge.Targets.Select(McpReadResults.Summary), Options);
            serialized.EnumerateArray().Any(target => target.TryGetProperty("semanticId", out _)).ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_genuine_event_generations_and_unique_scoped_operations(bool reverse)
    {
        var first = Document("intent.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n" + Operation + "      event Recorded\n      event Recorded generation 2\n");
        var second = Document("reference.play", "module M\n  feature F\n    slice StateChange T\n" + Operation + "      command Ask\n        produces Send\n        produces S.Send\n        produces S.Recorded\n      specification Check\n        given operation S.Recorded fails\n        when Ask\n        then operation S.Send\n        then compensated Send\n");
        var snapshot = new McpSnapshot(reverse ? [second, first] : [first, second]);
        var references = snapshot.Index.References.Where(reference => reference.Name == "Send" || reference.Name == "S.Send" || reference.Name == "S.Recorded").ToArray();
        references.Length.ShouldEqual(6);
        foreach (var reference in references)
        {
            var edge = new McpReferenceEdge(reference, snapshot.Index.Resolve(reference));
            edge.Targets.Length.ShouldEqual(1);
            edge.Resolution.ShouldEqual(reference.Role == "givenOperationFailure" ? "wrongKind" : "resolved");
            edge.Targets.Single().Location.Path.ShouldEqual(reference.Name == "Send" ? "reference.play" : "intent.play");
            if (reference.Name == "S.Recorded") ((EventSyntax)edge.Targets.Single().Syntax).Generation.ShouldEqual(2u);
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public void should_resolve_the_complete_physical_winning_scope_even_when_assembly_selects_a_different_sibling(int permutation, bool mixedKind)
    {
        var declarations = new[]
        {
            Document("a.play", "module M\n  feature F\n    slice StateChange S\n      command Other\n"),
            Document("b.play", "module M\n  feature F\n    slice StateChange S\n" + (mixedKind ? Event : Operation)),
            Document("other.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange U\n" + Operation)
        };
        int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
        const string referring = "module M\n  feature F\n    slice StateChange T\n      command Ask\n        produces Send\n      specification Check\n        given operation Send fails\n        when Ask\n        then operation Send\n        then compensated Send\n";
        var snapshot = new McpSnapshot([.. orders[permutation].Select(index => declarations[index]), Document("reference.play", referring)]);
        snapshot.Compilation.Success.ShouldBeFalse();
        var references = snapshot.Index.References.Where(reference => reference.Name == "Send").ToArray();
        references.Length.ShouldEqual(4);
        foreach (var reference in references)
        {
            var edge = new McpReferenceEdge(reference, snapshot.Index.Resolve(reference));
            edge.Resolution.ShouldEqual("ambiguous");
            edge.Targets.Length.ShouldEqual(4);
            edge.Targets.Where(target => target.Kind == "Event" || target.Kind == "Operation").Select(target => target.Address)
                .Order(StringComparer.Ordinal).SequenceEqual(["M.F.S.Send", "M.F.U.Send"]).ShouldBeTrue();
            edge.Targets.Select(target => target.Location.Path).Distinct().Order(StringComparer.Ordinal)
                .SequenceEqual(["a.play", "b.play", "other.play"]).ShouldBeTrue();
        }
        snapshot.Index.ResolveProduction("S.Send", ["M", "F", "T"]).Resolution.ShouldEqual("ambiguous");
        snapshot.Index.ResolveProduction("M.F.S.Send", ["M", "F", "T"]).Resolution.ShouldEqual("ambiguous");
        snapshot.Index.ResolveProduction("U.Send", ["M", "F", "T"]).Resolution.ShouldEqual("resolved");
        var inspections = snapshot.Index.CandidateInspectionCount;
        foreach (var reference in references) snapshot.Index.Resolve(reference);
        snapshot.Index.CandidateInspectionCount.ShouldEqual(inspections);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_suffix_reference_ambiguity_separate_from_exact_ownership(bool reverse)
    {
        var documents = new[]
        {
            Document("short.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n" + Operation),
            Document("long.play", "module N\n  feature M\n    feature F\n      slice StateChange S\n        operation Send\n          uses Mailer\n"),
            Document("reference.play", "module M\n  feature F\n    slice StateChange T\n      command Ask\n        produces M.F.S.Send\n      specification Check\n        given operation M.F.S.Send fails\n        when Ask\n        then operation M.F.S.Send\n        then compensated M.F.S.Send\n")
        };
        var declarations = new McpSnapshot([.. reverse ? documents.Take(2).Reverse() : documents.Take(2)]);
        declarations.Compilation.Success.ShouldBeTrue();
        declarations.Index.HasExactOwnershipCollision("Operation", "Send", ["M", "F", "S"]).ShouldBeFalse();
        declarations.Index.HasExactOwnershipCollision("Operation", "Send", ["N", "M", "F", "S"]).ShouldBeFalse();
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        var references = snapshot.Index.References.Where(reference => reference.Name == "M.F.S.Send").ToArray();
        references.Length.ShouldEqual(4);
        foreach (var reference in references)
        {
            var edge = new McpReferenceEdge(reference, snapshot.Index.Resolve(reference));
            edge.Resolution.ShouldEqual("ambiguous");
            edge.Targets.Select(target => target.Address).Order(StringComparer.Ordinal)
                .SequenceEqual(["M.F.S.Send", "N.M.F.S.Send"]).ShouldBeTrue();
        }
        snapshot.Index.ResolveProduction("Send", ["M", "F", "T"]).Resolution.ShouldEqual("resolved");
        snapshot.Index.ResolveProduction("S.Send", ["M", "F", "T"]).Resolution.ShouldEqual("ambiguous");
        snapshot.Index.ResolveProduction("N.M.F.S.Send", ["M", "F", "T"]).Targets.Single().Address.ShouldEqual("N.M.F.S.Send");
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
