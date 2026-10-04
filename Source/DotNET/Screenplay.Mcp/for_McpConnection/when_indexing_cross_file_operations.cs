// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_indexing_cross_file_operations
{
    const string Commands = "module M\n  feature F\n    slice StateChange A\n      command C\n        produces Recorded\n        produces Send\n        produces Recorded\n      operation Send\n        uses Mailer\n    slice StateChange Plain\n      command OnlyEvents\n        produces Recorded\n        produces Recorded\n";
    const string Declarations = "system Mailer\nmodule M\n  feature F\n    slice StateChange B\n      event Send\n      event Recorded\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_use_assembled_readiness_and_kind_resolution_in_either_file_order(bool reverse)
    {
        var documents = new[] { Document("commands.play", Commands), Document("declarations.play", Declarations) };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeTrue();
        var index = snapshot.Index;
        var command = index.Find("M.F.A.C", "Command").Single();
        index.Readiness.SyntaxOnly(command.Syntax).ShouldBeTrue();
        index.Readiness.ProducedEvents((Syntax.CommandSyntax)command.Syntax).SequenceEqual(["Recorded", "Recorded"]).ShouldBeTrue();
        var plain = index.Find("M.F.Plain.OnlyEvents", "Command").Single();
        index.Readiness.SyntaxOnly(plain.Syntax).ShouldBeFalse();
        index.Readiness.ProducedEvents((Syntax.CommandSyntax)plain.Syntax).SequenceEqual(["Recorded", "Recorded"]).ShouldBeTrue();
        var reference = index.Outgoing(command.Owner).Single(reference => reference.Role == "produces" && reference.Name == "Send");
        reference.Kinds.SequenceEqual(["Operation"]).ShouldBeTrue();
        index.Resolve(reference).Single().Address.ShouldEqual("M.F.A.Send");
        command.Location.Path.ShouldEqual("commands.play");
        index.Find("Mailer", "System").Single().Location.Path.ShouldEqual("declarations.play");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_inventory_cross_file_command_operations_with_import_placement_in_either_order(bool reverse)
    {
        var documents = new[]
        {
            Document("application.play", "module M\n  feature F\n    import \"command.play\"\n    import \"operation.play\"\n"),
            Document("command.play", "slice StateChange A\n  command C\n    produces B.Send\n  command OnlyEvents\n    produces Recorded\n"),
            Document("operation.play", "system Mailer\nslice StateChange B\n  operation Send\n    uses Mailer\n  event Recorded\n")
        };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeTrue();
        var index = snapshot.Index;
        var command = index.Find("M.F.A.C", "Command").Single();
        index.Readiness.SyntaxOnly(command.Syntax).ShouldBeTrue();
        index.Readiness.ProducedEvents((Syntax.CommandSyntax)command.Syntax).ShouldBeEmpty();
        var plain = index.Find("M.F.A.OnlyEvents", "Command").Single();
        index.Readiness.ProducedEvents((Syntax.CommandSyntax)plain.Syntax).Single().ShouldEqual("Recorded");
        var reference = index.Outgoing(command.Owner).Single(reference => reference.Role == "produces");
        reference.Kinds.SequenceEqual(["Operation"]).ShouldBeTrue();
        index.Resolve(reference).Single().Location.Path.ShouldEqual("operation.play");
    }

    [Fact]
    public void should_keep_ambiguous_kinds_unselected()
    {
        const string source = "system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      event Send\n    slice StateChange C\n      command Ask\n        produces Send\n";
        var index = new McpSnapshot([Document("model.play", source)]).Index;
        var reference = index.Outgoing("M.F.C.Ask").Single(reference => reference.Role == "produces");
        reference.Kinds.SequenceEqual(["Event", "Operation"]).ShouldBeTrue();
        index.Resolve(reference).Length.ShouldEqual(2);
    }

    [Fact]
    public void should_resolve_system_and_specification_operation_dependencies()
    {
        const string source = Commands + "      specification T\n        given operation A.Send fails\n        when C\n        then operation A.Send\n";
        var index = new McpSnapshot([Document("model.play", source), Document("systems.play", Declarations)]).Index;
        var operation = index.Find("M.F.A.Send", "Operation").Single();
        var uses = index.Outgoing(operation.Owner).Single(reference => reference.Role == "uses");
        index.Resolve(uses).Single().Kind.ShouldEqual("System");
        var specification = index.Find("M.F.Plain.T", "Specification").Single();
        foreach (var reference in index.Outgoing(specification.Owner).Where(reference => reference.Kinds.Contains("Operation")))
        {
            index.Resolve(reference).Single().Address.ShouldEqual("M.F.A.Send");
        }
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
