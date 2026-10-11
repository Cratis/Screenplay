// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_scoped_publication_plan : Specification
{
    protected SemanticExecutionPlan _plan;
    protected SemanticExecutionPlanCompilation _compilation;
    protected SemanticExecutionRequest _request;
    protected SemanticFact _fact;
    protected SemanticExecutionResult _result;

    protected void Prepare(bool deferPublication)
    {
        var publication = deferPublication ? "from Other\n  total = 0\nevery\n  add total by 1" : "from Selected\n  total = 1";
        var publicationSource = string.Join(Environment.NewLine, publication.Split('\n').Select(line => $"          {line}"));
        var source = $$"""
            module M
              feature F
                slice StateChange Registration
                  command Select
                    item String identifier
                    produces Selected
                      for item
                  event Selected
                  event Other
                slice Translate Publication
                  direction outbound
                  public event Published
                    total Int
                  projection Publisher => Published
            {{publicationSource}}
                slice StateView View
                  readmodel Summary
                    item String key
                    total Int optional
                  projection DeferredSummary => Summary
                    remove via join on Published
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Publication"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scoped-publication"), "scoped-publication", "Publication.play", source);
        var compilation = new SemanticModelCompiler().Compile("Publication", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        var selection = slices.Where(slice => slice.Name == "Registration" || (!deferPublication && slice.Name == "Publication"));
        _compilation = SemanticExecutionPlan.Compile(model, selection.Select(slice => slice.Id));
        Assert.True(_compilation.Success, string.Join(Environment.NewLine, _compilation.Issues.Select(issue => issue.Details)));
        _plan = _compilation.Plan!;
        var command = _plan.Commands.Values.Single();
        _request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("one"))], []);
        _fact = new(_plan.Events.Values.Single(contract => contract.Name == "Selected").Id, SemanticValue.Text("one"), [])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("one")))
        };
    }
}
