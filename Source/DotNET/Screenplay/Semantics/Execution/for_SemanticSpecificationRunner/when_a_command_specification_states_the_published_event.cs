// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// Decision 0098 (#866): the accepted path of a publishing command is specified where the command is, and states the public
// event an outbound Translate slice publishes for it.
public class when_a_command_specification_states_the_published_event : Specification
{
    const string Source =
        """
        module Catalog
          feature Pricing
            slice StateChange SetPrice
              command SetPrice
                productId String identifier
                price Decimal
                produces PriceSet
                  for productId
                  price = price
              event PriceSet
                price Decimal
              specification SettingAPricePublishesIt
                when SetPrice
                  productId = "p1"
                  price = 59
                then PriceSet
                  price = 59
                then PriceChanged
                  productId = "p1"
                  newPrice = 59
              specification TheStatedPublicationMustMatch
                when SetPrice
                  productId = "p1"
                  price = 59
                then PriceSet
                  price = 59
                then PriceChanged
                  productId = "p1"
                  newPrice = 60
            slice Translate PublishPrice
              direction outbound
              public event PriceChanged
                productId String
                newPrice Decimal
              projection PricePublisher => PriceChanged
                from PriceSet
                  productId = $eventSourceId
                  newPrice = price
        """;

    CompilationResult<SemanticCompilation> _compilation;
    CompilationResult<Syntax.ApplicationSyntax> _syntax;
    SemanticSpecificationRun _publishing;
    SemanticSpecificationRun _mismatched;

    void Establish()
    {
        _syntax = new ScreenplayCompiler().Compile(Source);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Catalog"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("catalog"), "catalog", "Catalog.play", Source);
        _compilation = new SemanticModelCompiler().Compile("Catalog", SemanticDocumentSet.Create([document], catalog));
    }

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_compilation.Value!.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _publishing = runner.Run(plan, plan.Specifications.Values.Single(_ => _.Name == "SettingAPricePublishesIt").Id);
        _mismatched = runner.Run(plan, plan.Specifications.Values.Single(_ => _.Name == "TheStatedPublicationMustMatch").Id);
    }

    [Fact] void should_not_call_the_published_event_unreachable() => _syntax.Diagnostics.Select(_ => _.Code).ShouldNotContain(DiagnosticCodes.UnreachableSpecificationOutcome);
    [Fact] void should_compile() => Assert.True(_compilation.Success, string.Join('\n', _compilation.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_pass_when_the_publication_is_stated() => Assert.True(_publishing.Passed, string.Join('\n', _publishing.Failures));
    [Fact] void should_fail_when_the_stated_publication_differs() => _mismatched.Passed.ShouldBeFalse();
}
