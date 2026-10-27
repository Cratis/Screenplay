// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Frozen C(M.0) structural vocabulary for the shipped majors 1–34. The strict per-major
/// reader/writer supplies baseline field ordering, optionality, variants and array ordering.
/// These tables identify extension boundaries independently of future semantic knowledge.
/// The @major and @first-last annotations freeze the strict reader's sinceVersion gates
/// and the published writer's version-specific vocabulary, not the union of later majors.
/// Never add minor-promoted fields here: they remain in the opaque canonical region.
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

    static readonly ImmutableDictionary<EsmBaselineShape, string> _definitions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Envelope"] = "schema schemaVersion languageVersion semanticVersion revision application:Application",
        ["Application"] = "id name concepts:Concept types:Type modules:Module policies:Policy triggers@6:Trigger eventSources@8:EventSourceDeclaration reactionProductionMode@22 commandReadMode@25",
        ["Concept"] = "id name primitive values validations:Validation numbers@12 compliance@13:Compliance generation@15:Generation",
        ["Generation"] = "kind pattern checksum min max requirement size count encoding alphabet prefix case",
        ["Compliance"] = "personal:Personal secret:Secret",
        ["Personal"] = "special criminal",
        ["Secret"] = "scope",
        ["Type"] = "id name properties:Property",
        ["Property"] = "id name type:TypeReference identifier generated@7",
        ["TypeReference"] = "kind primitive target collection optional",
        ["Module"] = "id name features:Feature",
        ["Feature"] = "id name features:Feature slices:Slice",
        ["Slice"] = "id name kind events:EventContract commands:Command readModels:ReadModel projections:Projection reducers:Reducer queries:Query specifications:Specification constraints:Constraint reactions@6:Reaction captures@6:Capture direction@9",
        ["EventContract"] = "id contractId contractRevision name storedName@16 properties:Property tags visibility@9 origin@9 predecessor@4 priorRevisions@4:PriorRevision",
        ["PriorRevision"] = "contractRevision predecessor properties:Property tags",
        ["Command"] = "id name properties:Property validations:Validation codeValidations:CodeValidation produces:ProducedEvent requirements:Requirement authorization:Authorization destination:Destination response@7:Response route@8:CommandRoute handler@23:Handler reads@25:DecisionRead",
        ["CodeValidation"] = "requirementId",
        ["Handler"] = "requirementId resultVersion",
        ["DecisionRead"] = "name readModel key presence",
        ["ProducedEvent"] = "eventContract condition:Expression destination:Expression mappings:Mapping when:Condition tags destinationType@6:TypeReference route@10:CommandRoute",
        ["Destination"] = "type:TypeReference value:Expression",
        ["Mapping"] = "targetProperty source:Expression",
        ["ReadModel"] = "id name properties:Property",
        ["Projection"] = "id name event@9 readModel transitions:Transition scope:Scope",
        ["Transition"] = "eventContract affectedInstance:AffectedInstance mappings:Mapping",
        ["AffectedInstance"] = "cardinality@1-3 key:Expression",
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
        ["Reducer"] = "name event@9 readModel key initialState result transitions:ReducerTransition from@10:ObserverFilter",
        ["ReducerTransition"] = "eventContract requirementId",
        ["Query"] = "id name readModel argument:QueryArgument keyProperty keyParts@27:QueryKeyPart filters@14:QueryFilter cardinality delivery authorization:Authorization",
        ["QueryArgument"] = "id name type:TypeReference",
        ["QueryKeyPart"] = "id name type:TypeReference keyProperty",
        ["QueryFilter"] = "id name type:TypeReference property",
        ["QueryFilterValue"] = "filter value:Value",
        ["Specification"] = "id name givenEvents:SpecificationEvent givenReadModels:SpecificationReadModel givenCaller:Caller when:SpecificationCommand whenAppended:SpecificationEvent whenRedelivered@17:Redelivered thenEvents:SpecificationEvent thenNoEvents@17 thenEventsInAnyOrder thenReadModels:SpecificationReadModel thenAbsentReadModels@5:AbsentReadModel thenQueries:SpecificationQuery thenErrors:SpecificationError thenDenied thenReturns@7:ThenReturns givenClock@6 givenCaptures@6:SpecificationCapture givenGenerated@30:PinnedValue whenClock@6 whenTrigger@6:WhenTrigger whenCapture@6:SpecificationCapture thenInvocations@34:SpecificationCommand",
        ["SpecificationEvent"] = "eventContract values:PropertyValue eventSource:EventSource route@8:FixtureRoute unrouted@8",
        ["SpecificationCommand"] = "command values:PropertyValue generatedValues@7:PropertyValue eventSource:EventSource",
        ["SpecificationReadModel"] = "readModel key:Value values:PropertyValue exactly",
        ["AbsentReadModel"] = "readModel key:Value",
        ["SpecificationQuery"] = "query key:Value filters@14:QueryFilterValue results:SpecificationReadModel exactly",
        ["SpecificationError"] = "code message",
        ["Redelivered"] = "reaction given",
        ["EventSource"] = "type:TypeReference value:Value",
        ["Validation"] = "property kind operand:Value message severity name requirementId",
        ["Constraint"] = "name kind scope targets:ConstraintTarget releasedBy ignoreCasing message",
        ["ConstraintTarget"] = "eventContract properties",
        ["Expression"] = "kind value:Value contextValue type:TypeReference member read@25 property@25 root source target",
        ["Value"] = "kind value properties:PropertyValue values:Value",
        ["PropertyValue"] = "targetProperty value:Value",
        ["Requirement"] = "condition:Condition message severity",
        ["Condition"] = "kind left:ConditionOperand operator right:ConditionOperand",
        ["ConditionOperand"] = "read@25 property value:Value",
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
        ["Reaction"] = "id name triggers:ReactionTrigger runsAs@10:ReactionIdentity from@10:ObserverFilter",
        ["ReactionIdentity"] = "kind roles",
        ["ReactionTrigger"] = "kind source every at onDayOfWeek onDayOfMonth where:Condition produces:ProducedEvent invokes:Invocation requirementId",
        ["Invocation"] = "command mappings:Mapping onRefused@19:RefusalBranch",
        ["RefusalBranch"] = "selector constraint produces:ProducedEvent",
        ["Capture"] = "id name key map:CaptureMap appends:CaptureAppend children:CaptureChildren nested:CaptureNested sourceEvents@9 keyIsEventSource@33",
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
    }.ToImmutableDictionary(pair => Enum.Parse<EsmBaselineShape>(pair.Key), pair => pair.Value);

    static readonly ImmutableDictionary<uint, ImmutableDictionary<EsmBaselineShape, ImmutableDictionary<string, EsmBaselineShape>>> _baselines =
        Majors.ToImmutableDictionary(major => major, major => _definitions.ToImmutableDictionary(pair => pair.Key, pair => ParseFields(pair.Value, major)));

    internal static ImmutableDictionary<string, EsmBaselineShape> Fields(uint major, EsmBaselineShape shape) => _baselines[major][shape];

    internal static bool IsOwner(EsmBaselineShape shape) => _owners.Contains(shape);

    internal static EsmBaselineShape Child(uint major, EsmBaselineShape shape, string field, JsonElement parent)
    {
        var child = Fields(major, shape)[field];
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

    static ImmutableDictionary<string, EsmBaselineShape> ParseFields(string fields, uint major) => fields.Split(' ')
        .Select(field => field.Split(':'))
        .Where(parts => IncludesMajor(parts[0], major))
        .ToImmutableDictionary(parts => parts[0].Split('@')[0], parts => parts.Length == 1 ? EsmBaselineShape.Atom : Enum.Parse<EsmBaselineShape>(parts[1]), StringComparer.Ordinal);

    static bool IncludesMajor(string field, uint major)
    {
        var gate = field.Split('@');
        if (gate.Length == 1) return true;
        var bounds = gate[1].Split('-');
        if (major < uint.Parse(bounds[0], CultureInfo.InvariantCulture)) return false;
        return bounds.Length == 1 || major <= uint.Parse(bounds[1], CultureInfo.InvariantCulture);
    }
}
