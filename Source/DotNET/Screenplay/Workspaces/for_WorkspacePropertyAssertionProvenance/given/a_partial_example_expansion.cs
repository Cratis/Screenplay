// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.given;

public class a_partial_example_expansion : for_WorkspaceRefactoring.given.a_projection_effect_workspace
{
    private protected WorkspacePropertyAssertionProvenance Provenance;
    protected WorkspaceSyntaxEntry Occurrence;
    protected IEnumerable<SourceLocation>? Consumers;
    protected IEnumerable<Diagnostic> CompilationDiagnostics = [];
    protected IReadOnlyList<Diagnostic> ExpansionDiagnostics = [];

    void Establish()
    {
        UseBindableSource(ExampleSource + "\n      specification Other\n        when append Audited\n          name = \"Ada\"\n          other = \"kept\"\n        then Audited\n          name = \"Ada\"\n          other = \"kept\"");
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        Occurrence = index.Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping && mapping.Property == "name" && WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Node is SpecificationExampleSyntax));
        var application = new AuthoringSnapshot(Workspace.Documents).Compilation.Value!;
        Provenance = new(index, new(() => new(application, [], ExpansionDiagnostics)), new(() => CompilationDiagnostics));
    }

    protected static Diagnostic Error(int line, string path = "model.play") => Diagnostic.Error(DiagnosticCodes.UnresolvedSpecificationExampleType, "Example expansion is incomplete.", new(line, 9, path));
}
