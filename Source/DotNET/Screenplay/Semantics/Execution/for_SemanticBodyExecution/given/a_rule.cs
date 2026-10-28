// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_rule : Specification
{
    protected const string Body = "return context.Value != null;";
    protected const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                validate
                  name rule valid message "A name is required"
                    ```csharp
                    return context.Value != null;
                    ```
                produces Placed
                  for name
              event Placed
        """;

    protected ISemanticBodyExecutionProvider Provider = null!;
    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticImplementationRequirement Requirement = null!;
    protected SemanticBodyExecutionResult Answer = new(true);
    protected SemanticRuleContext? Context;
    protected SemanticImplementationBody? ReceivedBody;
    protected SemanticExecutionResult Result = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;

    void Establish()
    {
        Configure(Source);
        Provider = Substitute.For<ISemanticBodyExecutionProvider>();
        Provider.Evaluate(Arg.Any<SemanticImplementationBody>(), Arg.Any<SemanticRuleContext>()).Returns(call =>
        {
            ReceivedBody = call.Arg<SemanticImplementationBody>();
            Context = call.Arg<SemanticRuleContext>();
            return Answer;
        });
    }

    protected void Configure(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("rules"), "rules", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.ShouldBeEmpty();
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Requirement = compilation.ImplementationRequirements.Single();
        Bodies = [new(Requirement, Body)];
        var command = Plan.Commands.Values.Single();
        Request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("First"))], []) with
        {
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "subject", "Name", "user"),
            Tenant = "tenant"
        };
    }
}
