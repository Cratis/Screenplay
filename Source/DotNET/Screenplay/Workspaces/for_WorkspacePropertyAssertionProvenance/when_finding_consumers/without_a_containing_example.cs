// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class without_a_containing_example : for_WorkspaceRefactoring.given.a_projection_effect_workspace
{
    WorkspaceSyntaxIndex _index;
    WorkspacePropertyAssertionProvenance _provenance;
    Lazy<EffectiveSpecificationApplication?> _expansion;
    IEnumerable<SourceLocation>? _consumers;

    void Establish()
    {
        UseBindableSource(ExampleSource);
        _index = WorkspaceSyntaxIndex.Create(Workspace);
        _expansion = new(() => null);
        _provenance = new(_index, _expansion);
    }

    void Because() => _consumers = _provenance.Consumers(_index.Entries.First(entry => entry.Node is PropertyMappingSyntax && WorkspacePropertyPlan.Parents(entry, _index).Any(parent => parent.Node is SpecificationEventSyntax)));

    [Fact] void should_report_known_empty_consumers() => _consumers.ShouldBeEmpty();
    [Fact] void should_not_require_example_expansion() => _expansion.IsValueCreated.ShouldBeFalse();
}
