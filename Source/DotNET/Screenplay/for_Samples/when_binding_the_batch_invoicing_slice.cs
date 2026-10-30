// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_the_batch_invoicing_slice : Specification
{
    CompilationResult<SemanticCompilation> _result;
    SemanticCommand _command;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"));
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var module = application.Modules.Single(value => value.Name == "Invoicing");
        var feature = module.Features.Single(value => value.Name == "InvoiceManagement");
        var batch = feature.Slices.Single(value => value.Name == "ProcessInvoiceBatch");

        // This probe admits the handler, not the sample's deliberately partial design specifications.
        var slice = batch with
        {
            Specifications = [],
            Events = [.. batch.Events, .. feature.Slices.SelectMany(value => value.Events).Where(value => value.Name == "InvoiceSent" || value.Name == "InvoiceCancelled")]
        };
        var focused = new ApplicationSyntax(
            [],
            application.Concepts.Where(value => value.Name == "InvoiceId" || value.Name == "BatchId"),
            application.Policies.Where(value => value.Name == "IsPerson" || value.Name == "IsInvoicingStaff" || value.Name == "IsAccountant"),
            [new ModuleSyntax(module.Name, [], [new FeatureSyntax(feature.Name, [], [slice], feature.Location) { Authorize = feature.Authorize }], module.Location) { Authorize = module.Authorize }],
            application.Location);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("sample"), "sample", "invoicing.play", source);
        _result = new SemanticModelBinder().Bind("Invoicing", focused, SemanticDocumentSet.Create([document], catalog));
        Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _command = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_select_v35() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V35);
    [Fact] void should_use_result_contract_v2() => _command.Handler!.ResultVersion.ShouldEqual(2U);
    [Fact] void should_declare_four_possible_events() => _command.Handler!.PossibleEvents.Length.ShouldEqual(4);
    [Fact] void should_reference_one_invoice_at_a_time() => _command.Handler!.PossibleEvents.Skip(1).Take(2).All(entry => entry.Destination is SemanticOneOfExpression).ShouldBeTrue();
    [Fact] void should_not_refuse_the_attachment() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.CommandHandlerRequiresAttachment).ShouldBeFalse();
}
