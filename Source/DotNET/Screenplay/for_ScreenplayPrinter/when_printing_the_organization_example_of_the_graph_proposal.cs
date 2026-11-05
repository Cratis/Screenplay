// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_the_organization_example_of_the_graph_proposal : Specification
{
    const string Graph =
        "graph Organization\n" +
        "  node Person\n" +
        "    personId PersonId key\n" +
        "    name Name\n" +
        "  node Group\n" +
        "    groupId GroupId key\n" +
        "    name Name\n" +
        "  edge MEMBER_OF source Person target Group\n" +
        "    since DateTime\n";

    const string Projection =
        "projection OrganizationGraph => graph Organization\n" +
        "  node Person\n" +
        "    from PersonRegistered key personId\n" +
        "      name = name\n" +
        "    remove with PersonRemoved key personId detach\n" +
        "  node Group\n" +
        "    from GroupCreated key groupId\n" +
        "      name = name\n" +
        "  edge MEMBER_OF\n" +
        "    from MemberAdded source personId target groupId\n" +
        "      since = $eventContext.occurred\n" +
        "    remove with MemberRemoved source personId target groupId\n";

    const string Events =
        "event PersonRegistered\n  personId PersonId\n  name Name\n" +
        "event PersonRemoved\n  personId PersonId\n" +
        "event GroupCreated\n  groupId GroupId\n  name Name\n" +
        "event MemberAdded\n  personId PersonId\n  groupId GroupId\n" +
        "event MemberRemoved\n  personId PersonId\n  groupId GroupId\n";

    string _printed;
    string[] _codes;

    void Because()
    {
        var source = "concept PersonId : Uuid\nconcept GroupId : Uuid\nconcept Name : String\n" +
            "module M\n  feature F\n    slice StateView S\n" + GraphDocuments.Indent(Events + Graph + Projection);
        var compiler = new ScreenplayCompiler();
        var result = compiler.Compile(source);
        _codes = [.. result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];
        _printed = new ScreenplayPrinter().Print(compiler.Parse(source).Value!);
    }

    [Fact] void should_compile_without_diagnostics() => _codes.ShouldBeEmpty();
    [Fact] void should_print_the_graph_unchanged() => _printed.ShouldContain(GraphDocuments.Indent(Graph).TrimEnd('\n'));
    [Fact] void should_print_the_projection_unchanged() => _printed.ShouldContain(GraphDocuments.Indent(Projection).TrimEnd('\n'));
}
