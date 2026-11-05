// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

// Observation spec: pins what print and rebind currently do with the language, schema and semantic versions.
// The printer receives syntax only, so no version tuple can reach it; the rebound tuple is re-selected from features.
public class when_round_tripping_inferred_versions : a_semantic_binder
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";
    const string V1Body = "      command C\n        name String";
    const string V2Body = "      command C\n        id Uuid identifier\n        produces Created\n          for id\n      event Created";
    const string V6Body = "    slice Automation A\n      reaction R\n        when Startup\n          produces Started\n            for \"start\"\n      event Started";
    const string V7Body = "      command C\n        id String identifier\n        returns id\n        produces Created\n          for id\n          id = id\n      event Created\n        id String";

    [Fact]
    void should_keep_a_minimal_v1_source_on_v1_at_every_step()
    {
        var (original, printed, rebound) = RoundTrip(Prefix + V1Body);
        original.ShouldEqual("1.0/1/1.0");
        printed.ShouldEqual("1.0/1/1.0");
        rebound.ShouldEqual(original);
    }

    [Fact]
    void should_keep_a_defining_v2_source_on_v2_at_every_step()
    {
        var (original, printed, rebound) = RoundTrip(Prefix + V2Body);
        original.ShouldEqual("2.0/2/2.0");
        printed.ShouldEqual("2.0/2/2.0");
        rebound.ShouldEqual(original);
    }

    [Fact]
    void should_refuse_an_explicit_v2_tuple_over_a_rebound_v1_graph()
    {
        var graph = Bind(Prefix + V1Body).Value!.Model.Application;
        var exception = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V2, SemanticVersion.V2, graph));
        exception.ShouldBeOfExactType<InvalidSemanticContract>();
        exception.Message.ShouldEqual("An ESM v2 model must contain a typed destination, a specification event source, or an occurrence context mapping.");
    }

    [Fact]
    void should_accept_a_retained_v22_tuple_over_a_v6_graph_and_reselect_v22_from_the_atomic_declaration()
    {
        var legacy = canonical_serialization_golden_vectors.CreateSemanticModelV6();
        var atomic = legacy.Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic };
        var retained = ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, atomic);
        Tuple(retained).ShouldEqual("22.0/22/22.0");
        Tuple(legacy).ShouldEqual("6.0/6/6.0");

        var (original, printed, rebound) = RoundTrip("reaction batches atomic\n" + Prefix + V6Body);
        original.ShouldEqual("22.0/22/22.0");
        printed.ShouldEqual(original);
        rebound.ShouldEqual(original);
    }

    [Fact]
    void should_keep_a_v7_response_source_on_v7_at_every_step()
    {
        var (original, printed, rebound) = RoundTrip(Prefix + V7Body);
        original.ShouldEqual("7.0/7/7.0");
        printed.ShouldEqual("7.0/7/7.0");
        rebound.ShouldEqual(original);
    }

    static string Tuple(ExecutableSemanticModel model) => $"{model.LanguageVersion}/{model.LanguageVersion.Major}/{model.SemanticVersion}";

    (string Original, string Printed, string Rebound) RoundTrip(string source)
    {
        var compiler = new ScreenplayCompiler();
        var first = Bind(source);
        first.Success.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(compiler.Parse(source).Value!);
        var second = Bind(printed);
        second.Success.ShouldBeTrue();
        var third = Bind(new ScreenplayPrinter().Print(compiler.Parse(printed).Value!));
        third.Success.ShouldBeTrue();
        return (Tuple(first.Value!.Model), Tuple(second.Value!.Model), Tuple(third.Value!.Model));
    }
}
#endif
