// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_carrying_passive_read_models : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source = """
        command reads protected
        module M
          feature F
            slice StateChange S
              readmodel Row
                id String key
                value String
              projection Build => Row
                from Recorded
                  id = $eventSourceId
                  value = value
                remove with Removed
              event Recorded
                value String
              event Removed
              command Decide
                id String identifier
                value String
                reads Row by id
                produces Recorded
                  for id
                  value = value
        """;

    [Fact]
    void should_record_the_identity_without_changing_executable_bytes_or_revision()
    {
        var active = Bind(Source);
        var passive = Bind(Source.Replace("      readmodel Row", "      readmodel Row\n        passive", StringComparison.Ordinal));
        active.Success.ShouldBeTrue();
        passive.Success.ShouldBeTrue();
        active.Value!.PassiveReadModels.ShouldBeEmpty();
        var compilation = passive.Value!;
        var model = compilation.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Single();
        compilation.PassiveReadModels.Single().ShouldEqual(model.Id);
        compilation.Model.Revision.ShouldEqual(active.Value.Model.Revision);
        SemanticModelSerializer.Serialize(compilation.Model).SequenceEqual(SemanticModelSerializer.Serialize(active.Value.Model)).ShouldBeTrue();
        passive.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax && diagnostic.Message.Contains("passive", StringComparison.Ordinal)).ShouldBeTrue();
        var origins = ImmutableDictionary<SemanticId, Syntax.Specifications.EffectiveSpecification>.Empty;
        compilation.WithEnvironment(EnvironmentRequirements.None).WithSpecificationOrigins(origins).PassiveReadModels.ShouldEqual(compilation.PassiveReadModels);
        compilation.WithSpecificationOrigins(origins).WithEnvironment(EnvironmentRequirements.None).PassiveReadModels.ShouldEqual(compilation.PassiveReadModels);
        compilation.WithPassiveReadModels(compilation.PassiveReadModels).SpecificationOrigins.ShouldEqual(compilation.SpecificationOrigins);
        compilation.WithPassiveReadModels(compilation.PassiveReadModels).Environment.ShouldEqual(compilation.Environment);
    }

    [Fact]
    void should_default_to_empty_when_recreated_from_portable_semantics()
    {
        var result = Bind(Source);
        result.Success.ShouldBeTrue();
        var compilation = result.Value!;
        SemanticCompilation.Create(compilation.Model, compilation.Documents, compilation.SourceMap).PassiveReadModels.ShouldBeEmpty();
    }
}
