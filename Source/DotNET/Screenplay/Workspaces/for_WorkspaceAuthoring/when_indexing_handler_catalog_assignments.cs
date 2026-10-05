// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_indexing_handler_catalog_assignments
{
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(128)]
    public void should_enumerate_the_catalog_once_not_once_per_handler(int count)
    {
        var application = ApplicationIdentity.Create("A");
        var source = "module M\n  feature F\n" + string.Join('\n', Enumerable.Range(0, count + 1).Select(position =>
            $"    slice StateChange S{position}\n      command C\n        handler\n          implementation"));
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var assignments = Enumerable.Range(0, count).Select(position => new SemanticIdentityAssignment(
            Owner(position), SemanticId.Create(SemanticAddress.ForModule(application, $"Durable{position}")), SemanticIdentityOrigin.Persisted)).ToArray();
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Create(application, [], [.. assignments], []));
        var enumerationCount = 0;
        var assignmentCount = 0;
        var inventory = WorkspaceImplementationInventory.Create(WorkspaceSyntaxIndex.Create(workspace), CountedAssignments());
        enumerationCount.ShouldEqual(1);
        assignmentCount.ShouldEqual(count);
        inventory.Entries.Length.ShouldEqual(count + 1);
        for (var position = 0; position < count; position++)
        {
            inventory.Entries[position].Owner.ShouldEqual(assignments[position].Address);
            inventory.Entries[position].OwnerId.ShouldEqual(assignments[position].Id);
            inventory.Entries[position].IdentityOrigin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        }

        var fallback = inventory.Entries[^1];
        fallback.Owner.ShouldEqual(Owner(count));
        fallback.OwnerId.ShouldEqual(SemanticId.Create(Owner(count)));
        fallback.IdentityOrigin.ShouldEqual(SemanticIdentityOrigin.LegacyBootstrap);
        fallback.IsProvisional.ShouldBeTrue();
        inventory.Entries.Select(entry => entry.RequirementId).Distinct(StringComparer.Ordinal).Count().ShouldEqual(count + 1);

        IEnumerable<SemanticIdentityAssignment> CountedAssignments()
        {
            enumerationCount++;
            foreach (var assignment in assignments)
            {
                assignmentCount++;
                yield return assignment;
            }
        }
    }

    static SemanticAddress Owner(int position) => SemanticAddress.ForCommand(SemanticAddress.ForSlice(ApplicationIdentity.Create("A"), "M", ["F"], $"S{position}"), "C");
}
