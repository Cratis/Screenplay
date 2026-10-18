// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations.for_ObservationComparison.given;

public class a_compiled_model : Specification
{
    protected const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                produces Registered
                  for projectId
                  name = name
                produces Audited
                  for projectId
                  name = name
              event Registered
                name String
              event Audited
                name String
        """;
    protected const string ReactionSource = """
        module Projects
          feature Registration
            slice Automation Follow
              event Registered
                name String
              event Audited
                name String
              reaction Follow
                when Registered
                  name
                  produces Audited
                    name = name
        """;
    protected readonly ObservedEventType Registered = new("Registered", 1);
    protected readonly ObservedEventType Audited = new("Audited", 1);
    protected readonly ObservedProducer Register = new(ObservedProducerKind.Command, "Register");
    protected SemanticCompilation _compilation = null!;
    protected ObservationIdentityMap _map = null!;
    protected snapshot_builder _snapshot = null!;
    protected ObservationReport _result = null!;

    protected SemanticSlice Slice => _compilation.Model.Application.Modules[0].Features[0].Slices[0];

    void Establish()
    {
        Model(Source);
        _snapshot = new()
        {
            Registered = [Registered, Audited],
            Appends = [new(Registered, 1), new(Audited, 1)],
            Edges = [new(Register, Registered, 1), new(Register, Audited, 1)]
        };
    }

    protected static SemanticCompilation Compile(string source)
    {
        var workspace = ComparedModel.FromSources("Projects", new Dictionary<string, string> { ["application.play"] = source }).Workspace;
        Assert.True(workspace.Compilation.Success, string.Join('\n', workspace.Compilation.Diagnostics.Select(value => $"{value.Code}: {value.Message}")));

        return workspace.Compilation.Value!;
    }

    protected void Model(string source)
    {
        _compilation = Compile(source);
        _map = ObservationIdentityMap.FromModel(_compilation.Model);
    }

    protected ObservationReport Compare() => ObservationComparison.Compare(_compilation, _snapshot.Build(), _map);

    protected void OnlyAudited()
    {
        _snapshot.Appends = [new(Audited, 1)];
        _snapshot.Edges = [new(Register, Audited, 1)];
    }

    protected void Reaction(bool mapped = false)
    {
        Model(ReactionSource);
        _snapshot.Edges = [new(new(ObservedProducerKind.Reaction, "follow-host"), Audited, 1), new(new(ObservedProducerKind.Command, "import"), Registered, 1)];
        _snapshot.Consumers = [new("follow-host", ObservedConsumerKind.Reaction, ObservedConsumerOwner.Application, "log", [Registered], 2, 1)];
        if (mapped)
        {
            _map = _map.With(Slice.Reactions[0].Id, producer: new(ObservedProducerKind.Reaction, "follow-host"), consumerIdentifier: "follow-host");
        }
    }

    protected ObservationFinding Finding(ObservationFindingKind kind) => _result.Findings.First(value => value.Kind == kind);
}
