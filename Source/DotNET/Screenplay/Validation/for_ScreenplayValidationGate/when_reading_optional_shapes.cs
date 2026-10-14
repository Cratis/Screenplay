// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_optional_shapes : given.a_gate_file
{
    ValidationGateReadResult[] _results;

    void Because()
    {
        var shapes = new[] { "{}", "{\"validation\":{}}", "{\"$schema\":\"\"}", "{\"validation\":{\"checks\":[]}}", "{\"validation\":{\"warningsAsErrors\":true,\"openQuestions\":\"fail\"}}" }
            .Concat(CompletenessChecks.AcceptedNames.Select(name => JsonSerializer.Serialize(new { validation = new { checks = new[] { name } } })));
        _results = shapes.Select(shape =>
        {
            File.WriteAllText(GatePath, shape);
            return ScreenplayValidationGate.Read(Root);
        }).ToArray();
    }

    [Fact] void should_accept_every_shape_and_parser_selection() => _results.All(result => result.Declaration is not null && result.Failure is null).ShouldBeTrue();
    [Fact] void should_leave_omitted_values_undeclared() => _results[0].Declaration.ShouldEqual(new DeclaredValidationGate());
    [Fact] void should_distinguish_empty_checks_from_omission() => _results[3].Declaration.Checks.Value.ShouldBeEmpty();
}
