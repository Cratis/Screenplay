// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.given;

/// <summary>
/// Builds application trees node by node, in shapes no parser produces.
/// </summary>
/// <remarks>
/// The round trip harness starts from text, so every tree it reaches is one the parser was willing to build.
/// That is exactly the tree that cannot carry a rule implementation on a kind other than <c>rule</c>, or two
/// implementations where the grammar reads one - so a printer more permissive than the grammar is invisible
/// to it. These trees are assembled directly, the way a generator assembles them from its own model.
/// </remarks>
public static class a_hand_built_application
{
    public const string CommandName = "ApproveCustomer";
    public const string ConceptName = "OrganizationNumber";
    public const string QueryName = "ListCustomers";
    public const string PropertyName = "orgNumber";

    public static ApplicationSyntax WithCommandRule(ValidationRuleSyntax rule) =>
        Application([], [Slice(SliceType.StateChange, [Command([Validate(rule)], null)], [])]);

    public static ApplicationSyntax WithConceptRule(ValidationRuleSyntax rule) =>
        Application(
            [new ConceptSyntax(ConceptName, "String", [], [], SourceLocation.Start, [Validate(rule)])],
            [Slice(SliceType.StateChange, [Command([], null)], [])]);

    public static ApplicationSyntax WithHandler(HandlerSyntax handler) =>
        Application([], [Slice(SliceType.StateChange, [Command([], handler)], [])]);

    public static ApplicationSyntax WithPerformer(PerformerSyntax performer) =>
        Application([], [Slice(SliceType.StateView, [], [Query(performer)])]);

    public static ValidationRuleSyntax CommandRule(CompilationResult<ApplicationSyntax> result) =>
        ReparsedCommand(result).Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();

    public static ValidationRuleSyntax ConceptRule(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();

    public static HandlerSyntax? Handler(CompilationResult<ApplicationSyntax> result) =>
        ReparsedCommand(result).Handler;

    public static PerformerSyntax? Performer(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single().Performer;

    static CommandSyntax ReparsedCommand(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    static DeclarativeValidateSyntax Validate(ValidationRuleSyntax rule) =>
        new([rule], SourceLocation.Start);

    static CommandSyntax Command(IEnumerable<ValidateSyntax> validations, HandlerSyntax? handler) =>
        new(
            CommandName,
            [new PropertySyntax(PropertyName, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            null,
            validations,
            [],
            handler,
            SourceLocation.Start);

    static QuerySyntax Query(PerformerSyntax performer) =>
        new(
            QueryName,
            new TypeRefSyntax("CustomerListReadModel", true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            Performer: performer);

    static SliceSyntax Slice(SliceType type, IEnumerable<CommandSyntax> commands, IEnumerable<QuerySyntax> queries) =>
        new(type, "Approval", [], commands, queries, [], [], [], [], [], [], SourceLocation.Start);

    static ApplicationSyntax Application(IEnumerable<ConceptSyntax> concepts, IEnumerable<SliceSyntax> slices) =>
        new(
            [],
            concepts,
            [],
            [new ModuleSyntax("Customers", [], [new FeatureSyntax("Approval", [], slices, SourceLocation.Start)], SourceLocation.Start)],
            SourceLocation.Start);
}
