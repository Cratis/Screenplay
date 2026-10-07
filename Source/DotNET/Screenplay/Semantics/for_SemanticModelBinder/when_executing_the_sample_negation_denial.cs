// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

// The full invoicing sample intentionally uses unadmitted semantics. Isolate its actual policy and
// denial fixture with the command's supplied input shape so this new example has an executable verdict.
public class when_executing_the_sample_negation_denial : given.a_semantic_binder
{
    SemanticSpecificationRun _result;

    void Because()
    {
        var lines = File.ReadAllLines(Path.Combine(Root(), "Samples/Invoicing/invoicing.play"));
        var policy = string.Join('\n', lines.SkipWhile(line => line != "policy IsPerson").Take(2));
        var scenario = string.Join('\n', lines.SkipWhile(line => line.Trim() != "specification RejectingAServiceRegisteringAnInvoice").TakeWhile(line => line.Length > 0));
        (scenario.Length > 0).ShouldBeTrue();
        var source = $"""
            {policy}
            module Invoicing
              feature InvoiceManagement
                slice StateChange RegisterInvoice
                  command RegisterInvoice
                    invoiceId Uuid identifier
                    invoiceNumber String
                    authorize IsPerson
            {scenario}
            """;
        var bound = Bind(source);
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = bound.Value!.Model;
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        _result = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
    }

    [Fact] void should_deny_the_service() => ((SemanticRejected)_result.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_pass_the_authored_denial_assertion() => _result.Passed.ShouldBeTrue();

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
