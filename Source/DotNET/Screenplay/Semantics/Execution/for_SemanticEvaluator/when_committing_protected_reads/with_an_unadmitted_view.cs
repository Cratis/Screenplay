// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_an_unadmitted_view : given.a_protected_command
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_report_unsupported_projection_instead_of_absence_or_using_a_snapshot(bool history, bool snapshot)
    {
        CompileScoped();

        // A scoped host may supply history whose unadmitted views have not been computed.
        _world = history ? SemanticWorld.Create([Fact("Recorded", "read", "original")], []) : SemanticWorld.Empty;
        _world.ReadModels.ShouldBeEmpty();
        if (snapshot)
        {
            var view = _plan.ReadModels.Values.Single(view => view.Name == "View");
            var instance = new SemanticReadModelInstance(
                view.Id,
                SemanticValue.Text("read"),
                [new(view.Properties.Single(property => property.Name == "id").Id, SemanticValue.Text("read")),
                 new(view.Properties.Single(property => property.Name == "value").Id, SemanticValue.Text("original")),
                 new(view.Properties.Single(property => property.Name == "at").Id, SemanticValue.Text("2026-10-15T00:00:00.0000000Z"))]);
            _world = SemanticWorld.Create(_world.Facts, [instance]);
        }

        var result = (SemanticUnsupported)_evaluator.Execute(_plan, _world, Request());
        result.Capability.ShouldEqual(SemanticExecutionCapability.Projection);
        result.Details.ShouldEqual("Read model 'View' is not admitted by scoped execution-plan compilation.");
        result.World.ShouldBeSame(_world);
    }

    void CompileScoped()
    {
        var source = Source.Replace("value String\n      projection Build => View", "value String\n        at DateTime\n      projection Build => View", StringComparison.Ordinal)
            .Replace("value = value\n        remove with Removed", "value = value\n          at = $eventContext.occurred\n        remove with Removed", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "scoped-protected-read";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "scoped.play", source);
        var bound = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = bound.Value!.Model;
        var write = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Write");
        var scoped = SemanticExecutionPlan.Compile(model, [write.Id]);
        Assert.True(scoped.Success, string.Join('\n', scoped.Issues.Select(issue => issue.Details)));
        scoped.UnselectedIssues.ShouldContain(issue => issue.Kind == SemanticPlanIssueKind.UnsupportedEventContext);
        _plan = scoped.Plan!;
        _plan.IsUnadmittedReadModel(_plan.ReadModels.Values.Single(view => view.Name == "View").Id).ShouldBeTrue();
    }
}
