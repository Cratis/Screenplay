// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_the_described_invoicing_slice : Specification
{
    SemanticExecutionPlan _plan;
    string _requirementId;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"));
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var module = application.Modules.Single(value => value.Name == "Invoicing");
        var feature = module.Features.Single(value => value.Name == "InvoiceManagement");
        var slice = feature.Slices.Single(value => value.Name == "ApplyLateFee");
        var focused = new ApplicationSyntax(
            [],
            application.Concepts.Where(value => value.Name == "InvoiceId" || value.Name == "DaysOverdue" || value.Name == "Money"),
            application.Policies.Where(value => value.Name == "IsPerson" || value.Name == "IsInvoicingStaff" || value.Name == "IsAccountant"),
            [new ModuleSyntax(module.Name, [], [new FeatureSyntax(feature.Name, [], [slice], feature.Location) { Authorize = feature.Authorize }], module.Location) { Authorize = module.Authorize }],
            application.Location);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("sample"), "sample", "invoicing.play", source);
        var bound = new SemanticModelBinder().Bind("Invoicing", focused, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
        _requirementId = bound.ImplementationRequirements.Single().RequirementId;
        _plan = SemanticExecutionPlan.Compile(bound.Value.Model).Plan!;
    }

    [Fact] void should_execute_the_denial_without_a_body() => Run("DenyingALateFeeForAnInvoiceManager").Passed.ShouldBeTrue();

    [Fact]
    void should_reach_the_body_in_the_acceptance_specification()
    {
        var run = Run("ApplyingALateFee");
        run.Passed.ShouldBeFalse();
        var unsupported = (SemanticUnsupported)run.Execution;
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        unsupported.Details.ShouldContain(_requirementId);
        unsupported.World.Facts.ShouldBeEmpty();
    }

    SemanticSpecificationRun Run(string name) => new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single(specification => specification.Name == name).Id);
}
