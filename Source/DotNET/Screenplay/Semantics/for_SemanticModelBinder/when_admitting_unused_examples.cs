// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_unused_examples : given.a_semantic_binder
{
    const string Declarations =
        """
        concept Token : Uuid
        concept State : Enum
          values ready, done
        type Details
          name String
          count Int
        module M
          feature F
            slice StateChange Record
              command Record
                id Uuid identifier
                count Int
                detail Details
                token Token generated
                produces Recorded
                  for id
                  count = count
              event Recorded
                count Int
              readmodel View
                id Uuid identifier
                note String optional
                state State optional
                date Date optional
                instant DateTime optional
                active Bool optional
              query ViewById => View optional
                by id Uuid
        """;

    [Theory]
    [InlineData("example Input : Record\n  count = \"not a number\"")]
    [InlineData("example Input : Record\n  count = 1.5")]
    [InlineData("example Input : Record\n  id = \"not a uuid\"")]
    [InlineData("example Input : Record\n  count = null")]
    [InlineData("example Input : Record\n  detail = {\"name\":\"A\"}")]
    [InlineData("example Input : Record\n  detail = {\"name\":\"A\",\"count\":\"wrong\"}")]
    [InlineData("example Input : Record\n  generated token = \"not a uuid\"")]
    [InlineData("example Input : Record\n  for \"not a uuid\"")]
    [InlineData("example Fact : Recorded\n  for \"not a uuid\"")]
    [InlineData("example StateView : View\n  id = null")]
    [InlineData("example StateView : View\n  state = \"unknown\"")]
    [InlineData("example StateView : View\n  date = \"2026-02-30\"")]
    [InlineData("example StateView : View\n  instant = \"2026-10-12T12:00:00\"")]
    [InlineData("example StateView : View\n  active = \"true\"")]
    [InlineData("example Input : Record\n  count = unknown")]
    [InlineData("example StateView : View\n  for \"unknown\"")]
    void should_refuse_invalid_stated_values_without_a_use_site(string example) => Bind(example + "\n" + Declarations).Success.ShouldBeFalse();

    [Fact]
    void should_identify_the_unused_scalar_admission_failure()
    {
        var result = Bind("example Input : Record\n  count = \"wrong\"\n" + Declarations);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnadmittedSpecificationExampleValue && diagnostic.Message.Contains("Input", StringComparison.Ordinal));
    }

    [Fact]
    void should_admit_partial_examples_without_defaults_or_version_promotion()
    {
        var baseline = Bind(Declarations);
        Assert.True(baseline.Success, string.Join('\n', baseline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plain = baseline.Value!.Model;
        var result = Bind("example Input : Record\n  count = 10\nexample StateView : View\n  note = null\nexample GeneratedInput : Record\n  generated token = \"11111111-1111-1111-1111-111111111111\"\n" + Declarations);
        result.Success.ShouldBeTrue();
        result.Value!.Model.Revision.ShouldEqual(plain.Revision);
    }

    [Fact]
    void should_refuse_an_unused_event_destination_without_a_producer_contract()
    {
        const string Source = "example Fact : Recorded\n  for \"11111111-1111-1111-1111-111111111111\"\nmodule M\n  feature F\n    slice StateView View\n      event Recorded\n        count Int";
        var result = Bind(Source);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnadmittedSpecificationExampleValue);
    }

    [Fact]
    void should_normalize_an_unused_generated_identifier_like_a_used_fixture()
    {
        var source = "example Input : Record\n  for \"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"\n" + Declarations.Replace("command Record\n        id Uuid identifier", "command Record\n        id Token generated identifier", StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_not_let_an_override_hide_an_invalid_example_value()
    {
        var result = Bind("example Input : Record\n  count = \"wrong\"\n" + Declarations + "\n      specification Overrides\n        when Input\n          id = \"11111111-1111-1111-1111-111111111111\"\n          count = 10\n          detail = {\"name\":\"A\",\"count\":1}\n        then Recorded count = 10");
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnadmittedSpecificationExampleValue);
    }
}
