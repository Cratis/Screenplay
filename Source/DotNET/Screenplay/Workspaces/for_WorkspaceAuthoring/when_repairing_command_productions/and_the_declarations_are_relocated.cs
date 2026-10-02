// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_declarations_are_relocated : given.a_command_production
{
    EventSyntax _original;
    EventSyntax _relocated;

    void Establish()
    {
        Create(Source);
        _original = Declaration();
    }

    void Because()
    {
        var module = Source.IndexOf("module Projects", StringComparison.Ordinal);
        Create(Source[module..], Source[..module]);
        _relocated = Declaration();
    }

    EventSyntax Declaration() => (EventSyntax)((AddWorkspaceNode)Find(DiagnosticCodes.UnknownEvent).Operations.Single()).Node;

    [Fact] void should_infer_the_same_contract_in_a_folder() => SyntaxJson.StructurallyEqual(_original, _relocated).ShouldBeTrue();

    [Fact]
    void should_infer_the_same_contract_after_reordering()
    {
        var module = Source.IndexOf("module Projects", StringComparison.Ordinal);
        Create(Source[module..] + "\n" + Source[..module]);
        SyntaxJson.StructurallyEqual(_original, Declaration()).ShouldBeTrue();
    }

    [Fact]
    void should_resolve_a_nested_command_property_without_erasing_its_concept()
    {
        Create(Source.Replace("name = name", "name = details.name", StringComparison.Ordinal));
        Declaration().Properties.First().Type.Name.ShouldEqual("ProjectName");
    }
}
