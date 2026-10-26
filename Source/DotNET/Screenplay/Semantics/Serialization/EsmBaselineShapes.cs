// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Frozen C(M.0) structural vocabulary for the shipped majors 1–34. The strict per-major
/// reader/writer supplies baseline field ordering, optionality, variants and array ordering.
/// These tables identify extension boundaries independently of future semantic knowledge.
/// Never add minor-promoted fields here: they remain in the opaque canonical region.
/// Recognized fields forbidden by an older major are retained for strict rejection, not hidden.
/// </summary>
internal static class EsmBaselineShapes
{
    internal static readonly ImmutableArray<uint> Majors = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34];

    static readonly ImmutableHashSet<EsmBaselineShape> _owners =
    [
        EsmBaselineShape.Application, EsmBaselineShape.Concept, EsmBaselineShape.Type, EsmBaselineShape.Property,
        EsmBaselineShape.Module, EsmBaselineShape.Feature, EsmBaselineShape.Slice, EsmBaselineShape.EventContract,
        EsmBaselineShape.Command, EsmBaselineShape.ReadModel, EsmBaselineShape.Projection, EsmBaselineShape.Reaction,
        EsmBaselineShape.Capture, EsmBaselineShape.Specification, EsmBaselineShape.Query, EsmBaselineShape.QueryArgument,
        EsmBaselineShape.QueryKeyPart, EsmBaselineShape.QueryFilter, EsmBaselineShape.Trigger, EsmBaselineShape.EventSourceDeclaration, EsmBaselineShape.EventStream
    ];

    static readonly ImmutableDictionary<EsmBaselineShape, ImmutableDictionary<string, EsmBaselineShape>> _shapes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Envelope"] = "schema schemaVersion languageVersion semanticVersion revision application:Application",
        ["Application"] = "id name concepts:Concept types:Type modules:Module policies:Policy triggers:Trigger eventSources:EventSourceDeclaration reactionProductionMode commandReadMode",
        ["Concept"] = "id name primitive values validations:Validation numbers compliance:Compliance generation:Generation",
        ["Generation"] = "kind pattern checksum min max requirement size count encoding alphabet prefix case",
        ["Compliance"] = "personal:Personal secret:Secret",
        ["Personal"] = "special criminal",
        ["Secret"] = "scope",
        ["Type"] = "id name properties:Property",
        ["Property"] = "id name type:TypeReference identifier generated",
        ["TypeReference"] = "kind primitive target collection optional",
        ["Module"] = "id name features:Feature",
        ["Feature"] = "id name features:Feature slices:Slice",
        ["Slice"] = "id name kind events:EventContract commands:Command readModels:ReadModel projections:Projection reducers:Reducer queries:Query specifications:Specification constraints:Constraint reactions:Reaction captures:Capture direction",
        ["EventContract"] = "id contractId contractRevision name storedName properties:Property tags visibility origin predecessor priorRevisions:PriorRevision",
        ["PriorRevision"] = "contractRevision predecessor properties:Property tags",
        ["Command"] = "id name properties:Property validations:Validation codeValidations:CodeValidation produces:ProducedEvent requirements:Requirement authorization:Authorization destination:Destination response:Response route:CommandRoute handler:Handler reads:DecisionRead",
        ["CodeValidation"] = "requirementId",
        ["Handler"] = "requirementId resultVersion",
        ["DecisionRead"] = "name readModel key presence",
        ["ProducedEvent"] = "eventContract condition:Expression destination:Expression mappings:Mapping when:Condition tags destinationType:TypeReference route:CommandRoute",
        ["Destination"] = "type:TypeReference value:Expression",
        ["Mapping"] = "targetProperty source:Expression",
        ["ReadModel"] = "id name properties:Property",
        ["Projection"] = "id name event readModel transitions:Transition scope:Scope",
        ["Transition"] = "eventContract affectedInstance:AffectedInstance mappings:Mapping",
        ["AffectedInstance"] = "cardinality key:Expression",
        ["Scope"] = "from:From join:Join children:Children nested:Nested every:Every removedWith:Removal removedWithJoin:JoinRemoval",
        ["From"] = "eventContract key:ProjectionKey parentKey:ProjectionKey mappings:ProjectionMapping",
        ["Join"] = "eventContract on mappings:ProjectionMapping key:ProjectionKey",
        ["Children"] = "property identifiedBy scope:Scope",
        ["Nested"] = "property scope:Scope",
        ["Every"] = "includeChildren subscribesToAllEvents mappings:ProjectionMapping",
        ["Removal"] = "eventContract key:ProjectionKey parentKey:ProjectionKey",
        ["JoinRemoval"] = "eventContract key:ProjectionKey",
        ["ProjectionKey"] = "kind value:ProjectionValue parts:KeyPart type",
        ["KeyPart"] = "property value:ProjectionValue",
        ["ProjectionMapping"] = "target operation source:ProjectionValue factor:ProjectionValue",
        ["ProjectionValue"] = "kind value:Value path",
        ["Reducer"] = "name event readModel key initialState result transitions:ReducerTransition from:ObserverFilter",
        ["ReducerTransition"] = "eventContract requirementId",
        ["Query"] = "id name readModel argument:QueryArgument keyProperty keyParts:QueryKeyPart filters:QueryFilter cardinality delivery authorization:Authorization",
        ["QueryArgument"] = "id name type:TypeReference",
        ["QueryKeyPart"] = "id name type:TypeReference keyProperty",
        ["QueryFilter"] = "id name type:TypeReference property",
        ["QueryFilterValue"] = "filter value:Value",
        ["Specification"] = "id name givenEvents:SpecificationEvent givenReadModels:SpecificationReadModel givenCaller:Caller when:SpecificationCommand whenAppended:SpecificationEvent whenRedelivered:Redelivered thenEvents:SpecificationEvent thenNoEvents thenEventsInAnyOrder thenReadModels:SpecificationReadModel thenAbsentReadModels:AbsentReadModel thenQueries:SpecificationQuery thenErrors:SpecificationError thenDenied thenReturns:ThenReturns givenClock givenCaptures:SpecificationCapture givenGenerated:PinnedValue whenClock whenTrigger:WhenTrigger whenCapture:SpecificationCapture thenInvocations:SpecificationCommand",
        ["SpecificationEvent"] = "eventContract values:PropertyValue eventSource:EventSource route:FixtureRoute unrouted",
        ["SpecificationCommand"] = "command values:PropertyValue generatedValues:PropertyValue eventSource:EventSource",
        ["SpecificationReadModel"] = "readModel key:Value values:PropertyValue exactly",
        ["AbsentReadModel"] = "readModel key:Value",
        ["SpecificationQuery"] = "query key:Value filters:QueryFilterValue results:SpecificationReadModel exactly",
        ["SpecificationError"] = "code message",
        ["Redelivered"] = "reaction given",
        ["EventSource"] = "type:TypeReference value:Value",
        ["Validation"] = "property kind operand:Value message severity name requirementId",
        ["Constraint"] = "name kind scope targets:ConstraintTarget releasedBy ignoreCasing message",
        ["ConstraintTarget"] = "eventContract properties",
        ["Expression"] = "kind value:Value contextValue type:TypeReference member read property root source target",
        ["Value"] = "kind value properties:PropertyValue values:Value",
        ["PropertyValue"] = "targetProperty value:Value",
        ["Requirement"] = "condition:Condition message severity",
        ["Condition"] = "kind left:ConditionOperand operator right:ConditionOperand",
        ["ConditionOperand"] = "read property value:Value",
        ["Response"] = "kind source type:TypeReference fields:ResponseField",
        ["ResponseField"] = "name source type:TypeReference",
        ["ThenReturns"] = "kind value:Value fields:ReturnField",
        ["ReturnField"] = "name value:Value",
        ["Policy"] = "name condition:PolicyCondition requirementId",
        ["PolicyCondition"] = "kind role claim targetKind value operand:PolicyCondition operator left:PolicyCondition right:PolicyCondition",
        ["Authorization"] = "kind name operator left:Authorization right:Authorization",
        ["Caller"] = "authenticated roles claims:CallerClaim",
        ["CallerClaim"] = "type value",
        ["EventSourceDeclaration"] = "id name sourceKind identifierType:TypeReference streams:EventStream",
        ["EventStream"] = "id name streamKind streamIdType:TypeReference streamIdParts:StreamIdPart",
        ["StreamIdPart"] = "name type:TypeReference",
        ["CommandRoute"] = "source stream streamId:Expression streamIdParts:RoutePart",
        ["RoutePart"] = "part value:Expression",
        ["FixtureRoute"] = "source stream streamId:Value streamIdParts:FixturePart",
        ["FixturePart"] = "part value:Value",
        ["ObserverFilter"] = "source stream",
        ["Trigger"] = "id name properties:Property",
        ["Reaction"] = "id name triggers:ReactionTrigger runsAs:ReactionIdentity from:ObserverFilter",
        ["ReactionIdentity"] = "kind roles",
        ["ReactionTrigger"] = "kind source every at onDayOfWeek onDayOfMonth where:Condition produces:ProducedEvent invokes:Invocation requirementId",
        ["Invocation"] = "command mappings:Mapping onRefused:RefusalBranch",
        ["RefusalBranch"] = "selector constraint produces:ProducedEvent",
        ["Capture"] = "id name key map:CaptureMap appends:CaptureAppend children:CaptureChildren nested:CaptureNested sourceEvents keyIsEventSource",
        ["CaptureChildren"] = "field identifiedBy map:CaptureMap appends:CaptureAppend",
        ["CaptureNested"] = "field map:CaptureMap appends:CaptureAppend",
        ["CaptureMap"] = "kind targets source template:TemplatePart separator translations:Translation",
        ["TemplatePart"] = "text field",
        ["Translation"] = "from to",
        ["CaptureAppend"] = "eventContract eventSourceType:TypeReference when:CaptureCondition mappings:CaptureMapping tags",
        ["CaptureCondition"] = "kind fields from to expression",
        ["CaptureMapping"] = "targetProperty field value:Expression",
        ["SpecificationCapture"] = "capture record:CaptureRecord",
        ["CaptureRecord"] = "fields:CaptureField",
        ["CaptureField"] = "name kind value:Value record:CaptureRecord records:CaptureRecord",
        ["PinnedValue"] = "command targetProperty value:Value",
        ["WhenTrigger"] = "kind trigger values:PropertyValue"
    }.ToImmutableDictionary(pair => Enum.Parse<EsmBaselineShape>(pair.Key), pair => ParseFields(pair.Value));

    internal static ImmutableDictionary<string, EsmBaselineShape> Fields(EsmBaselineShape shape) => _shapes[shape];

    internal static bool IsOwner(EsmBaselineShape shape) => _owners.Contains(shape);

    internal static EsmBaselineShape Child(EsmBaselineShape shape, string field, JsonElement parent)
    {
        var child = Fields(shape)[field];
        if (shape != EsmBaselineShape.Condition || field is not ("left" or "right")) return child;
        if (IsLogicalCondition(parent))
        {
            return EsmBaselineShape.Condition;
        }

        return child;
    }

    internal static UnknownEsmCategory Category(EsmBaselineShape shape, JsonElement value, UnknownEsmCategory inherited) => shape switch
    {
        EsmBaselineShape.Policy or EsmBaselineShape.PolicyCondition or EsmBaselineShape.Authorization or EsmBaselineShape.Caller or EsmBaselineShape.CallerClaim => UnknownEsmCategory.Authorization,
        EsmBaselineShape.ReactionIdentity or EsmBaselineShape.EventSource or EsmBaselineShape.Destination or EsmBaselineShape.AffectedInstance or EsmBaselineShape.ProjectionKey => UnknownEsmCategory.IdentityMapping,
        EsmBaselineShape.Compliance => UnknownEsmCategory.Compliance,
        EsmBaselineShape.EventSourceDeclaration or EsmBaselineShape.EventStream or EsmBaselineShape.CommandRoute or EsmBaselineShape.FixtureRoute or EsmBaselineShape.ObserverFilter => UnknownEsmCategory.EventStreamRouting,
        EsmBaselineShape.Expression or EsmBaselineShape.ProjectionValue when IsIdentityValue(value) => UnknownEsmCategory.IdentityMapping,
        _ => inherited
    };

    static bool IsLogicalCondition(JsonElement value) => value.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String && (kind.GetString() == "and" || kind.GetString() == "or");

    static bool IsIdentityValue(JsonElement value)
    {
        if (!value.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String) return false;
        return kind.GetString() switch
        {
            "eventSourceIdentity" => true,
            "eventContext" => IsIdentityContext(value),
            _ => false
        };
    }

    static bool IsIdentityContext(JsonElement value) => value.TryGetProperty("contextValue", out var context) && context.ValueKind == JsonValueKind.String && context.GetString() == "eventSourceIdentity";

    static ImmutableDictionary<string, EsmBaselineShape> ParseFields(string fields) => fields.Split(' ').Select(field => field.Split(':')).ToImmutableDictionary(parts => parts[0], parts => parts.Length == 1 ? EsmBaselineShape.Atom : Enum.Parse<EsmBaselineShape>(parts[1]), StringComparer.Ordinal);
}
