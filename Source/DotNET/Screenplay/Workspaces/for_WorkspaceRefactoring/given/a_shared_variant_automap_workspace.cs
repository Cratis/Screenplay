// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_shared_variant_automap_workspace : a_multi_source_property_workspace
{
    internal const string VariantAutoMapSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event Changed
                name String
              event First
              event Second
            slice StateView Lookup
              readmodel One
                itemId String
                name String optional
              readmodel Two
                itemId String
                name String optional
              projection Items
                from Changed
                variant One
                  enters on First
                variant Two
                  enters on Second
              query Ones => One[]
              query Twos => Two[]
        """;

    void Establish() => UseBindableSource(VariantAutoMapSource);

    protected int InsertedMappings => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Count(entry => entry.Node is SetMappingSyntax);

    protected (string Target, string Source)[] Flows => new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(Result.Workspace)).Links.ProjectionEffects
        .Select(effect => (Target: effect.Targets.Single().Address, Source: effect.Sources.Single().Members.Single().Address)).OrderBy(flow => flow.Target, StringComparer.Ordinal).ToArray();

    protected (string Target, string Source)[] ExpectedFlows(string targetName, string sourceName) => new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(Workspace)).Links.ProjectionEffects
        .Select(effect => (Target: effect.Targets.Single().Address.Replace(".name", $".{targetName}", StringComparison.Ordinal), Source: effect.Sources.Single().Members.Single().Address.Replace(".name", $".{sourceName}", StringComparison.Ordinal)))
        .OrderBy(flow => flow.Target, StringComparer.Ordinal).ToArray();

    protected object[] VariantProperties(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Where(entry => entry.Node is PropertySyntax && entry.Address?.OwnerKind == Semantics.SemanticKind.ReadModel)
        .OrderBy(entry => entry.Address!, WorkspaceSemanticAddressComparer.Instance).Select(entry => (object)(entry.Address, entry.SemanticId)).ToArray();
}
