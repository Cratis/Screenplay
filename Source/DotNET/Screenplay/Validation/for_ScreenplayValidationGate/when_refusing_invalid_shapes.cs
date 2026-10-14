// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_refusing_invalid_shapes : given.a_gate_file
{
    ValidationGateReadResult[] _results;

    void Because()
    {
        string[] shapes =
        [
            "{", "[]", "null", "{\"validation\":null}", "{\"validation\":[]}",
            "{\"unknown\":true}", "{\"$schema\":2}", "{\"validation\":{\"unknown\":true}}",
            "{\"validation\":{},\"validation\":{}}", "{\"$schema\":\"a\",\"$schema\":\"b\"}",
            "{\"validation\":{\"scope\":\"M\",\"scope\":\"F\"}}", "{\"validation\":{\"scope\":\"M\",\"sc\\u006fpe\":\"F\"}}",
            "{\"validation\":{\"checks\":\"all\"}}", "{\"validation\":{\"checks\":[1]}}",
            "{\"validation\":{\"checks\":[\"navigation,query-keys\"]}}", "{\"validation\":{\"checks\":[\"PLAY9999\"]}}",
            "{\"validation\":{\"checks\":[\"typo\"]}}", "{\"validation\":{\"checks\":[null]}}",
            "{\"validation\":{\"warningsAsErrors\":\"true\"}}", "{\"validation\":{\"warningsAsErrors\":null}}",
            "{\"validation\":{\"scope\":false}}", "{\"validation\":{\"scope\":\" \"}}", "{\"validation\":{\"scope\":\"-bad\"}}",
            "{\"validation\":{\"openQuestions\":\"ignore\"}}", "{\"validation\":{\"openQuestions\":true}}",
            "{\"validation\":{\"checks\":[]},}", "{/* comment */}", "{} {}"
        ];
        _results = shapes.Select(shape =>
        {
            File.WriteAllText(GatePath, shape);
            return ScreenplayValidationGate.Read(Root);
        }).ToArray();
    }

    [Fact] void should_refuse_all_invalid_shapes() => _results.All(result => result.Failure.Kind == ValidationGateFailureKind.Invalid).ShouldBeTrue();
    [Fact] void should_never_return_a_partial_declaration() => _results.All(result => result.Declaration is null).ShouldBeTrue();
    [Fact] void should_locate_every_refusal() => _results.All(result => result.Failure.Path == GatePath && result.Failure.Line > 0 && result.Failure.Column > 0).ShouldBeTrue();
    [Fact] void should_name_every_reason() => _results.All(result => !string.IsNullOrWhiteSpace(result.Failure.Reason)).ShouldBeTrue();
}
