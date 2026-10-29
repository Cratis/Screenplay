// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_a_pending_implementation : given.a_possible_events_binder
{
    CompilationResult<PossibleEventsBinding> _result;
    SemanticCommand _command;

    void Because()
    {
        _result = BindPossible(Source);
        Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _command = Command(_result.Value!);
    }

    [Fact] void should_select_only_the_candidate_v35_graph() => (_result.Value!.LanguageVersion.Major, _result.Value!.SemanticVersion.Major).ShouldEqual((35u, 35u));
    [Fact] void should_not_invent_legacy_productions() => _command.Produces.ShouldBeEmpty();
    [Fact] void should_use_result_contract_v2() => (_command.Handler!.ResultVersion, _result.ImplementationRequirements.Single().ResultVersion).ShouldEqual((2u, 2u));
    [Fact] void should_keep_the_context_contract_at_v1() => _result.ImplementationRequirements.Single().ContextVersion.ShouldEqual(1u);
    [Fact] void should_keep_the_requirement_identity() => _command.Handler!.RequirementId.ShouldEqual(ImplementationRequirementIdentity.Create(_command.Id, SemanticImplementationRole.CommandHandler, null));
    [Fact] void should_keep_pending_hints() => _result.ImplementationRequirements.Single().Hints.SequenceEqual(["Return only the chosen events"]).ShouldBeTrue();
    [Fact] void should_keep_pending_resolution() => _result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Pending);
    [Fact] void should_emit_the_typed_context() => _result.TypedContextDescriptors.Single().RequirementId.ShouldEqual(_command.Handler!.RequirementId);
    [Fact] void should_keep_unmapped_event_payloads() => _command.Handler!.PossibleEvents.All(entry => entry.Mappings.IsEmpty).ShouldBeTrue();
    [Fact] void should_resolve_the_default_identifier() => ((SemanticResolvedExpression)_command.Handler!.PossibleEvents.First().Destination!).Target.ShouldEqual(_command.Properties.Single(property => property.IsIdentifier).Id);
    [Fact] void should_resolve_the_collection_identity() => ((SemanticOneOfExpression)_command.Handler!.PossibleEvents.Last().Destination!).Property.ShouldEqual(_command.Properties.Single(property => property.Name == "ids").Id);
    [Fact] void should_count_both_event_types_as_unrouted_producers() => SemanticDecisionReadAdmission.Producers(_result.Value!.Application.Modules.Single().Features.Single().Slices).Count(value => !value.Routed).ShouldEqual(2);
    [Fact] void should_not_enable_public_binding() => Bind(Source).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);

    [Fact]
    void should_preserve_the_positional_handler_contract()
    {
        var (requirement, version) = _command.Handler!;
        (requirement, version).ShouldEqual((_command.Handler!.RequirementId, 2u));
        ((int)SemanticExpressionKind.Unknown, (int)SemanticExpressionKind.Value, (int)SemanticExpressionKind.Resolved, (int)SemanticExpressionKind.EventContext, (int)SemanticExpressionKind.Refusal, (int)SemanticExpressionKind.ReadPath, (int)SemanticExpressionKind.OneOf).ShouldEqual((-1, 0, 1, 2, 3, 4, 5));
    }
}
