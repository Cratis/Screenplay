// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_event_consumers_are_cataloged : given.a_command_production
{
    const string InlineSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          projectId Uuid = projectId\n          name String = \"something\"\n";

    [Theory]
    [InlineData("      constraint Claim\n        unique event Renamed\n", typeof(UniqueEventConstraintSyntax), "event")]
    [InlineData("      constraint Claim\n        unique name on Renamed\n", typeof(UniquePropertyConstraintSyntax), "event")]
    [InlineData("      constraint Claim\n        unique event Other\n        unique event Renamed\n", typeof(UniqueEventConstraintSyntax), "event")]
    [InlineData("      constraint Claim\n        unique event Other\n        released by Renamed\n", typeof(UniqueEventConstraintSyntax), "releasedBy")]
    [InlineData("      constraint Claim\n        unique name on Other\n        released by Renamed\n", typeof(UniquePropertyConstraintSyntax), "releasedBy")]
    [InlineData("      projection Names => Names\n        from Renamed\n          name = name\n", typeof(EventSpecSyntax), "event")]
    [InlineData("      projection Names => Names\n        join names on projectId\n          with Renamed\n            name = name\n", typeof(JoinEventSyntax), "event")]
    [InlineData("      projection Names => Names\n        children names identified by projectId\n          from Renamed\n            name = name\n", typeof(EventSpecSyntax), "event")]
    [InlineData("      projection Names => Names\n        children names identified by projectId\n          remove with Renamed\n", typeof(RemoveWithSyntax), "event")]
    [InlineData("      projection Names => Names\n        nested name\n          from Other\n            name = name\n          clear with Renamed\n", typeof(ClearWithSyntax), "event")]
    [InlineData("      projection Names => Names\n        remove with Renamed\n", typeof(RemoveWithSyntax), "event")]
    [InlineData("      projection Names => Names\n        remove via join on Renamed\n", typeof(RemoveViaJoinSyntax), "event")]
    [InlineData("      projection Names\n        variant Active\n          enters on Renamed\n", typeof(ProjectionEntersOnSyntax), "event")]
    [InlineData("      reducer Names => Names\n        on Renamed\n", typeof(ReducerRuleSyntax), "event")]
    [InlineData("      reaction Observe\n        when Renamed\n", typeof(NamedTriggerSourceSyntax), "name")]
    [InlineData("      reaction Observe\n        every 1 minutes\n          produces Renamed\n", typeof(ProducesSyntax), "event")]
    [InlineData("      capture Names\n        source webhook\n          path /names\n        append Renamed\n          when name\n            name = $.name\n", typeof(CaptureAppendSyntax), "event")]
    [InlineData("      specification Example\n        given Renamed\n", typeof(SpecificationEventSyntax), "eventType")]
    [InlineData("      specification Example\n        then Renamed\n", typeof(SpecificationEventSyntax), "eventType")]
    [InlineData("      specification Example\n        when append Renamed\n", typeof(SpecificationEventSyntax), "eventType")]
    [InlineData("      screen Observe\n        on event Renamed\n          notify info \"Changed\"\n", typeof(EventInteractionTriggerSyntax), "eventName")]
    [InlineData("behavior Observe\n  on event Renamed\n    notify info \"Changed\"\n", typeof(EventInteractionTriggerSyntax), "eventName")]
    [InlineData("      command Guard\n        concurrency\n          events Renamed\n", typeof(ConcurrencySyntax), "eventTypes")]
    [InlineData("seed\n  for \"project\"\n    Renamed\n      name = \"Seeded\"\n", typeof(SeedEventSyntax), "event")]
    void should_share_typed_event_references_between_consumers_and_rename(string consumer, Type kind, string member)
    {
        Create(InlineSource + consumer);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        index.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var production = index.Entries.Single(entry => entry.Node is ProducesSyntax { InlineEvent: not null });
        var declaration = index.Entries.Single(entry => entry.Node is EventSyntax);
        var binding = new WorkspaceReferenceBindings(index).Bindings.Single(value => value.Reference.Entry.Handle != production.Handle &&
            value.Reference.Entry.Node.GetType() == kind && value.Reference.Member == member && value.Reference.Text == "Renamed");
        binding.Target!.Entry!.Handle.ShouldEqual(declaration.Handle);
        WorkspaceEventRepairs.HasConsumers(index, RepairFor(index)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("      projection Names => Names\n        all\n          name = \"Any event\"\n")]
    [InlineData("import External.Renamed\n")]
    [InlineData("policy Allowed\n  file Policies/Allowed.cs\n")]
    [InlineData("policy Allowed\n  ```csharp\n  return Renamed.IsAllowed;\n  ```\n")]
    void should_fail_closed_for_implicit_or_opaque_consumers(string consumer)
    {
        Create(InlineSource + consumer);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        index.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        WorkspaceEventRepairs.HasConsumers(index, RepairFor(index)).ShouldBeTrue();
    }

    static WorkspaceDiagnosticRepair RepairFor(WorkspaceSyntaxIndex index) => new(
        DiagnosticCodes.EventSourceIdInPayload,
        index.Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping && mapping.Property == "projectId").Handle,
        []);
}
