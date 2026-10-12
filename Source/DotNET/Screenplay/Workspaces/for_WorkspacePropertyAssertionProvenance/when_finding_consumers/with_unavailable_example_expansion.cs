// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class with_unavailable_example_expansion : for_WorkspaceRefactoring.given.a_projection_effect_workspace
{
    WorkspaceSyntaxIndex _index;
    WorkspacePropertyAssertionProvenance _provenance;
    IEnumerable<SourceLocation>? _consumers;

    void Establish()
    {
        UseBindableSource(ExampleSource);
        _index = WorkspaceSyntaxIndex.Create(Workspace);
        _provenance = new(_index, new(() => null));
    }

    void Because() => _consumers = _provenance.Consumers(_index.Entries.First(entry => entry.Node is PropertyMappingSyntax && WorkspacePropertyPlan.Parents(entry, _index).Any(parent => parent.Node is SpecificationExampleSyntax)));

    [Fact] void should_report_unavailable_rather_than_known_empty_consumers() => _consumers.ShouldBeNull();
}
