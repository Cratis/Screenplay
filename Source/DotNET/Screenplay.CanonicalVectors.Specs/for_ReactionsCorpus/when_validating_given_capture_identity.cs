// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_validating_given_capture_identity : Specification
{
    const string Source = """
        module Billing
          feature Import
            slice Translate Records
              capture Records
                key id
                append Seen
              event Seen
              specification Present
                given capture Records
                  id = "root"
                when capture Records
                  id = "root"
                then Seen
        """;

    [Fact]
    void should_reject_duplicate_givens_even_if_the_action_reuses_the_same_instance()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var copies in new[] { false, true })
        {
            var error = Catch.Exception(() => given.v6_regression_models.ChangeSpecification(model, specification =>
            {
                var shared = specification.GivenCaptures.Single();
                return specification with
                {
                    GivenCaptures = [shared, copies ? shared with { Record = shared.Record with { } } : shared],
                    WhenCapture = shared
                };
            }));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }

    [Fact]
    void should_allow_the_action_to_reuse_a_single_given_instance_or_its_key()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var sameInstance in new[] { false, true })
        {
            var shared = given.v6_regression_models.ChangeSpecification(model, specification => specification with
            {
                WhenCapture = sameInstance ? specification.GivenCaptures.Single() : specification.WhenCapture
            });
            foreach (var run in given.v6_regression_models.Runs(shared)) run.Passed.ShouldBeTrue();
        }
    }

    [Fact]
    void should_allow_distinct_given_keys_and_keep_number_and_text_keys_distinct()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var (first, second) in new[]
        {
            (SemanticValue.Text("first"), SemanticValue.Text("second")),
            (SemanticValue.Number(1.00m), SemanticValue.Text("1"))
        })
        {
            var distinct = given.v6_regression_models.ChangeSpecification(model, specification =>
            {
                var shared = specification.GivenCaptures.Single();
                return specification with { GivenCaptures = [Key(shared, first), Key(shared, second)] };
            });
            foreach (var run in given.v6_regression_models.Runs(distinct)) run.Passed.ShouldBeTrue();
        }
    }

    [Fact]
    void should_reject_numeric_duplicate_given_keys_independently_of_scale()
    {
        var model = given.v6_regression_models.Compile(Source);
        var error = Catch.Exception(() => given.v6_regression_models.ChangeSpecification(model, specification =>
        {
            var shared = specification.GivenCaptures.Single();
            return specification with { GivenCaptures = [Key(shared, SemanticValue.Number(1m)), Key(shared, SemanticValue.Number(1.00m))] };
        }));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
    }

    static SemanticSpecificationCapture Key(SemanticSpecificationCapture capture, SemanticValue key) => capture with
    {
        Record = capture.Record with { Fields = [new("id", SemanticCaptureFieldKind.Value) { Value = key }] }
    };
}
