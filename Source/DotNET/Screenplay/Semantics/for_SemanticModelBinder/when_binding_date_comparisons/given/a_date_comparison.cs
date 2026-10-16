// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_date_comparisons.given;

public class a_date_comparison : for_SemanticModelBinder.given.a_semantic_binder
{
    public const string Source = """
        module Scheduling
          feature Windows
            slice StateChange KeepWindow
              command KeepWindow
                id String identifier
                from DateTime
                to DateTime
                date Date
                validate
                  to >= "2026-01-01T01:00:00+01:00"
                  require to >= from
                produces WindowKept
                  for id
                  to = to
                produces when date >= "2026-01-01"
                  WindowDated
                    for id
              event WindowKept
                to DateTime
              event WindowDated
        """;

    protected static readonly string[] Operators = ["==", "!=", ">", ">=", "<", "<="];

    protected static string Model(string primitive, string clause) => $$"""
        module Scheduling
          feature Windows
            slice StateChange KeepWindow
              command KeepWindow
                id String identifier
                from {{primitive}}
                to {{primitive}}
                validate
                  {{clause}}
                produces WindowKept
                  for id
              event WindowKept
        """;

    protected new CompilationResult<SemanticCompilation> Bind(string source, string? syntaxPath = null, string displayPath = "application.play")
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return base.Bind(source, syntaxPath, displayPath);
    }

    protected static SemanticCommand Command(ExecutableSemanticModel model) => model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
