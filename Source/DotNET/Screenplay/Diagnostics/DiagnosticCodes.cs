// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Holds the stable codes every <see cref="Diagnostic"/> is identified by.
/// </summary>
/// <remarks>
/// A code is the only part of a diagnostic a consumer can rely on. Message text is written for a reader and gets
/// reworded whenever a clearer wording is found, so anything matching on it breaks on a copy edit - which is why
/// every diagnostic the compiler produces carries one of these.
/// <para>
/// The prefix is <c>PLAY</c>, after the <c>.play</c> documents this compiler reads. It deliberately shares nothing
/// with the <c>SP</c> codes Cratis Arc reports while <em>generating</em> a document: the two run one after the
/// other and their diagnostics land in one log, so a reader seeing <c>PLAY0034</c> beside <c>SP0034</c> can tell
/// at a glance which tool said it.
/// </para>
/// <para>
/// Codes are permanent. A number is never reused and never renumbered, and a code that is retired leaves its
/// number behind as a gap rather than closing the sequence up - a consumer suppresses and groups on a code, so
/// handing a retired number to something else would silently change what an existing suppression means. New codes
/// are appended at the end of the sequence whatever they are about, because the number says when a code was added
/// rather than where it belongs.
/// </para>
/// </remarks>
public static class DiagnosticCodes
{
    // The document and its top level.

    /// <summary>
    /// A line at the top level of a document opens with a word nothing at that level is declared by.
    /// </summary>
    public const string UnknownTopLevelConstruct = "PLAY0001";

    /// <summary>
    /// A <c>domain</c> line is not <c>domain &lt;Qualified.Name&gt;</c>.
    /// </summary>
    public const string InvalidDomainDeclaration = "PLAY0002";

    /// <summary>
    /// A document declares a domain more than once, and a document has at most one.
    /// </summary>
    public const string DuplicateDomain = "PLAY0003";

    /// <summary>
    /// <c>domain</c> is declared after another construct, and it names what the whole document is about.
    /// </summary>
    public const string DomainNotFirst = "PLAY0004";

    /// <summary>
    /// An <c>import</c> line is not <c>import &lt;Qualified.Name&gt;</c>.
    /// </summary>
    public const string InvalidImportDeclaration = "PLAY0005";

    /// <summary>
    /// A line is indented with tabs, and Screenplay decides nesting from spaces.
    /// </summary>
    public const string TabIndentation = "PLAY0006";

    // Concepts.

    /// <summary>
    /// A <c>concept</c> line is not <c>concept &lt;Name&gt; : &lt;Type&gt;</c>.
    /// </summary>
    public const string InvalidConceptDeclaration = "PLAY0007";

    /// <summary>
    /// A concept is declared over a primitive the language does not have.
    /// </summary>
    public const string UnknownPrimitiveType = "PLAY0008";

    /// <summary>
    /// A value of an enumeration concept is not an identifier.
    /// </summary>
    public const string InvalidEnumerationValue = "PLAY0009";

    /// <summary>
    /// A line in a concept body opens with a word a concept declares nothing by.
    /// </summary>
    public const string UnknownConceptDirective = "PLAY0010";

    /// <summary>
    /// A value of an enumeration is called <c>validate</c>, which the concept body reads as an empty validate block.
    /// </summary>
    public const string ValidateReadAsEnumerationBlock = "PLAY0011";

    /// <summary>
    /// A concept gives the reason for an attribute it does not carry.
    /// </summary>
    public const string AttributeReasonWithoutAttribute = "PLAY0012";

    /// <summary>
    /// A concept gives the reason for one attribute more than once.
    /// </summary>
    public const string DuplicateAttributeReason = "PLAY0013";

    // Types.

    /// <summary>
    /// A <c>type</c> line is not <c>type &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidTypeDeclaration = "PLAY0014";

    /// <summary>
    /// A type declares no properties, and a type is the properties it holds.
    /// </summary>
    public const string TypeWithoutProperties = "PLAY0015";

    /// <summary>
    /// A property line is not <c>&lt;name&gt; &lt;Type&gt;</c>.
    /// </summary>
    public const string InvalidPropertyDeclaration = "PLAY0016";

    /// <summary>
    /// A property outside a command is marked as the identifier, which only a command property can be.
    /// </summary>
    public const string IdentifierOutsideCommand = "PLAY0017";

    // Events.

    /// <summary>
    /// An <c>event</c> line is not <c>event &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidEventDeclaration = "PLAY0018";

    /// <summary>
    /// A property of an event is marked as the identifier, and an event never carries its event source id.
    /// </summary>
    public const string IdentifierOnEventProperty = "PLAY0019";

    /// <summary>
    /// A property called <c>tag</c> is read by the event body as a static tag rather than as a property.
    /// </summary>
    public const string TagPropertyReadAsTag = "PLAY0020";

    // Modules, features, slices and layouts.

    /// <summary>
    /// A <c>module</c> line is not <c>module &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidModuleDeclaration = "PLAY0021";

    /// <summary>
    /// A line in a module body opens with a word a module declares nothing by.
    /// </summary>
    public const string UnknownModuleDirective = "PLAY0022";

    /// <summary>
    /// A <c>feature</c> line is not <c>feature &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidFeatureDeclaration = "PLAY0023";

    /// <summary>
    /// A line in a feature body opens with a word a feature declares nothing by.
    /// </summary>
    public const string UnknownFeatureDirective = "PLAY0024";

    /// <summary>
    /// A slot declared by a layout, screen template or dialog template is not an identifier optionally followed by <c>contributes</c>.
    /// </summary>
    public const string InvalidLayoutSlotName = "PLAY0025";

    /// <summary>
    /// A line in a layout, screen template or dialog template body opens a block none of them declares anything by.
    /// </summary>
    public const string UnknownLayoutDirective = "PLAY0026";

    /// <summary>
    /// A <c>slice</c> line is not <c>slice &lt;Type&gt; &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidSliceDeclaration = "PLAY0027";

    /// <summary>
    /// A slice is declared with a type the language does not have.
    /// </summary>
    public const string UnknownSliceType = "PLAY0028";

    /// <summary>
    /// A line in a slice body opens with a word a slice declares nothing by.
    /// </summary>
    public const string UnknownSliceDirective = "PLAY0029";

    // Personas.

    /// <summary>
    /// A <c>persona</c> line is not <c>persona &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidPersonaDeclaration = "PLAY0030";

    /// <summary>
    /// A policy line in a persona body is not <c>policy &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidPersonaPolicyReference = "PLAY0031";

    /// <summary>
    /// A line in a persona body opens with a word a persona declares nothing by.
    /// </summary>
    public const string UnknownPersonaDirective = "PLAY0032";

    // Commands.

    /// <summary>
    /// A <c>command</c> line is not <c>command &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidCommandDeclaration = "PLAY0033";

    /// <summary>
    /// A line in a command body opens with a word a command declares nothing by.
    /// </summary>
    public const string UnknownCommandDirective = "PLAY0034";

    /// <summary>
    /// A command declares both <c>produces</c> and <c>handler</c>, which say the same thing two ways.
    /// </summary>
    public const string CommandWithProducesAndHandler = "PLAY0035";

    /// <summary>
    /// A command marks more than one property as its identifier.
    /// </summary>
    public const string DuplicateCommandIdentifier = "PLAY0036";

    /// <summary>
    /// A <c>concurrency</c> line carries anything beyond the keyword.
    /// </summary>
    public const string InvalidConcurrencyDeclaration = "PLAY0037";

    /// <summary>
    /// A command declares more than one concurrency block, and a command has at most one.
    /// </summary>
    public const string DuplicateConcurrencyBlock = "PLAY0038";

    /// <summary>
    /// A line in a concurrency block names a dimension the block does not have.
    /// </summary>
    public const string UnknownConcurrencyDimension = "PLAY0039";

    /// <summary>
    /// A dimension of a concurrency block is not written the way that dimension is written.
    /// </summary>
    public const string InvalidConcurrencyDimension = "PLAY0040";

    /// <summary>
    /// A concurrency block states one dimension more than once.
    /// </summary>
    public const string DuplicateConcurrencyDimension = "PLAY0041";

    /// <summary>
    /// A <c>produces</c> line is neither <c>produces &lt;EventType&gt;</c> nor <c>produces when &lt;condition&gt;</c>.
    /// </summary>
    public const string InvalidProducesDeclaration = "PLAY0042";

    /// <summary>
    /// A <c>produces when</c> condition is followed by no event to produce.
    /// </summary>
    public const string ProducesWhenWithoutEvent = "PLAY0043";

    /// <summary>
    /// A mapping line is not <c>&lt;property&gt; = &lt;source&gt;</c>.
    /// </summary>
    public const string InvalidPropertyMapping = "PLAY0044";

    /// <summary>
    /// A handler names neither a <c>file</c> nor an inline code block.
    /// </summary>
    public const string HandlerWithoutImplementation = "PLAY0045";

    /// <summary>
    /// A line in a handler body opens with a word a handler declares nothing by.
    /// </summary>
    public const string UnknownHandlerDirective = "PLAY0046";

    // Queries.

    /// <summary>
    /// A <c>query</c> line is not <c>query &lt;Name&gt; =&gt; [observable] &lt;ReadModel&gt;</c>.
    /// </summary>
    public const string InvalidQueryDeclaration = "PLAY0047";

    /// <summary>
    /// A line in a query body opens with a word a query declares nothing by.
    /// </summary>
    public const string UnknownQueryDirective = "PLAY0048";

    /// <summary>
    /// A <c>by</c> or <c>filter</c> parameter is not <c>&lt;keyword&gt; &lt;name&gt; &lt;Type&gt; [from &lt;source&gt;]</c>.
    /// </summary>
    public const string InvalidQueryParameter = "PLAY0049";

    /// <summary>
    /// A <c>performer</c> line carries anything beyond the keyword.
    /// </summary>
    public const string InvalidPerformerDeclaration = "PLAY0050";

    /// <summary>
    /// A query declares more than one performer, and a query has at most one.
    /// </summary>
    public const string DuplicatePerformer = "PLAY0051";

    /// <summary>
    /// A performer names neither a <c>file</c> nor an inline code block.
    /// </summary>
    public const string PerformerWithoutImplementation = "PLAY0052";

    /// <summary>
    /// A line in a performer body opens with a word a performer declares nothing by.
    /// </summary>
    public const string UnknownPerformerDirective = "PLAY0053";

    // Projections.

    /// <summary>
    /// A projection document holds a top level line that does not open a <c>projection</c>.
    /// </summary>
    public const string ExpectedProjection = "PLAY0054";

    /// <summary>
    /// A projection document declares no projection at all.
    /// </summary>
    public const string ProjectionDocumentWithoutProjection = "PLAY0055";

    /// <summary>
    /// A <c>projection</c> line is not <c>projection &lt;Name&gt; [=&gt; &lt;ReadModel&gt;]</c>.
    /// </summary>
    public const string InvalidProjectionDeclaration = "PLAY0056";

    /// <summary>
    /// A projection declares no directives, so it builds nothing.
    /// </summary>
    public const string EmptyProjection = "PLAY0057";

    /// <summary>
    /// A line in a projection body opens with a word a projection declares nothing by.
    /// </summary>
    public const string UnknownProjectionDirective = "PLAY0058";

    /// <summary>
    /// A projection declares more than one key.
    /// </summary>
    public const string DuplicateProjectionKey = "PLAY0059";

    /// <summary>
    /// A <c>from</c> block declares more than one key.
    /// </summary>
    public const string DuplicateFromKey = "PLAY0060";

    /// <summary>
    /// A <c>from</c> line names no event to read from.
    /// </summary>
    public const string FromWithoutEvent = "PLAY0061";

    /// <summary>
    /// An event reference is not a name the language can read as one.
    /// </summary>
    public const string InvalidEventReference = "PLAY0062";

    /// <summary>
    /// A <c>join</c> line is not <c>join &lt;property&gt; on &lt;key&gt;</c>.
    /// </summary>
    public const string InvalidJoinDeclaration = "PLAY0063";

    /// <summary>
    /// A join block holds a line that is not <c>with &lt;EventType&gt;</c>.
    /// </summary>
    public const string JoinWithoutEvent = "PLAY0064";

    /// <summary>
    /// A <c>children</c> line is not <c>children &lt;collection&gt; identified by &lt;key&gt;</c>.
    /// </summary>
    public const string InvalidChildrenDeclaration = "PLAY0065";

    /// <summary>
    /// A <c>nested</c> line is not <c>nested &lt;property&gt;</c>.
    /// </summary>
    public const string InvalidNestedDeclaration = "PLAY0066";

    /// <summary>
    /// A nested block reads from no event, so nothing ever fills it.
    /// </summary>
    public const string NestedBlockWithoutFrom = "PLAY0067";

    /// <summary>
    /// A <c>remove</c> line is neither <c>remove with &lt;EventType&gt;</c> nor <c>remove via join on &lt;EventType&gt;</c>.
    /// </summary>
    public const string InvalidRemoveDeclaration = "PLAY0068";

    /// <summary>
    /// A remove block holds a line other than <c>parent</c>.
    /// </summary>
    public const string UnknownRemoveDirective = "PLAY0069";

    /// <summary>
    /// A <c>clear</c> line is not <c>clear with &lt;EventType&gt;</c>.
    /// </summary>
    public const string InvalidClearDeclaration = "PLAY0070";

    /// <summary>
    /// <c>clear with</c> is written where there is nothing to clear.
    /// </summary>
    public const string ClearWithOutsideNestedBlock = "PLAY0071";

    /// <summary>
    /// A part of a composite key is not <c>&lt;property&gt; = &lt;expression&gt;</c>.
    /// </summary>
    public const string InvalidCompositeKeyPart = "PLAY0072";

    /// <summary>
    /// A composite key part is a template expression, which a key cannot be.
    /// </summary>
    public const string TemplateInCompositeKey = "PLAY0073";

    /// <summary>
    /// A composite key declares no parts.
    /// </summary>
    public const string EmptyCompositeKey = "PLAY0074";

    /// <summary>
    /// A mapping line in a projection is not one the language can read.
    /// </summary>
    public const string InvalidProjectionMapping = "PLAY0075";

    // Captures.

    /// <summary>
    /// A capture document holds a top level line that does not open a <c>capture</c>.
    /// </summary>
    public const string ExpectedCapture = "PLAY0076";

    /// <summary>
    /// A capture document declares no capture at all.
    /// </summary>
    public const string CaptureDocumentWithoutCapture = "PLAY0077";

    /// <summary>
    /// A <c>capture</c> line is not <c>capture &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidCaptureDeclaration = "PLAY0078";

    /// <summary>
    /// A line in a capture body opens with a word a capture declares nothing by.
    /// </summary>
    public const string UnknownCaptureDirective = "PLAY0079";

    /// <summary>
    /// A map entry is not <c>&lt;property&gt; = &lt;source&gt; [translate]</c>.
    /// </summary>
    public const string InvalidMapEntry = "PLAY0080";

    /// <summary>
    /// A translation is not <c>"&lt;source&gt;" =&gt; &lt;target&gt;</c>.
    /// </summary>
    public const string InvalidTranslation = "PLAY0081";

    /// <summary>
    /// A <c>split</c> line is not <c>split &lt;property&gt; by "&lt;separator&gt;"</c>.
    /// </summary>
    public const string InvalidSplitDeclaration = "PLAY0082";

    /// <summary>
    /// A target of a split is not a property path.
    /// </summary>
    public const string InvalidSplitTarget = "PLAY0083";

    /// <summary>
    /// An <c>append</c> line is not <c>append &lt;EventType&gt;</c>.
    /// </summary>
    public const string InvalidAppendDeclaration = "PLAY0084";

    /// <summary>
    /// A line in an append body opens with a word an append declares nothing by.
    /// </summary>
    public const string UnknownAppendDirective = "PLAY0085";

    /// <summary>
    /// A <c>when</c> line names no trigger.
    /// </summary>
    public const string WhenWithoutTrigger = "PLAY0086";

    /// <summary>
    /// A <c>when</c> clause is not one of the shapes a trigger is written in.
    /// </summary>
    public const string InvalidWhenClause = "PLAY0087";

    /// <summary>
    /// A value transition is not <c>when &lt;Path&gt; from &lt;value&gt; to &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidWhenTransitionClause = "PLAY0088";

    /// <summary>
    /// A <c>when</c> clause combines properties with both <c>and</c> and <c>or</c>.
    /// </summary>
    public const string MixedWhenCombinators = "PLAY0089";

    /// <summary>
    /// A <c>when</c> combinator is followed by no property.
    /// </summary>
    public const string WhenCombinatorWithoutProperty = "PLAY0090";

    /// <summary>
    /// A line in a children or nested block is neither <c>map</c> nor <c>append</c>.
    /// </summary>
    public const string UnknownCaptureBlockDirective = "PLAY0091";

    // Specifications.

    /// <summary>
    /// A specification document holds a top level line that does not open a <c>specification</c>.
    /// </summary>
    public const string ExpectedSpecification = "PLAY0092";

    /// <summary>
    /// A specification document declares no specification at all.
    /// </summary>
    public const string SpecificationDocumentWithoutSpecification = "PLAY0093";

    /// <summary>
    /// A <c>specification</c> line is not <c>specification &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidSpecificationDeclaration = "PLAY0094";

    /// <summary>
    /// A line in a specification body opens with a word a specification declares nothing by.
    /// </summary>
    public const string UnknownSpecificationDirective = "PLAY0095";

    /// <summary>
    /// A <c>when</c> line is not <c>when &lt;CommandType&gt;</c>.
    /// </summary>
    public const string InvalidSpecificationWhen = "PLAY0096";

    /// <summary>
    /// A specification issues more than one command, and a specification is one example.
    /// </summary>
    public const string DuplicateSpecificationWhen = "PLAY0097";

    /// <summary>
    /// A <c>then error</c> line is neither <c>then error</c> nor <c>then error "&lt;reason&gt;"</c>.
    /// </summary>
    public const string InvalidThenError = "PLAY0098";

    /// <summary>
    /// A <c>given readmodel</c> or <c>then readmodel</c> line does not name a read model type.
    /// </summary>
    public const string InvalidReadModelStep = "PLAY0099";

    /// <summary>
    /// A <c>given</c> or <c>then</c> line does not name an event type.
    /// </summary>
    public const string InvalidEventStep = "PLAY0100";

    /// <summary>
    /// A value a specification step states is not <c>&lt;property&gt; = &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidSpecificationValue = "PLAY0101";

    // Screens.

    /// <summary>
    /// A <c>screen</c> line is not <c>screen &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidScreenDeclaration = "PLAY0102";

    /// <summary>
    /// A line in a screen body opens with a word a screen declares nothing by.
    /// </summary>
    public const string UnknownScreenDirective = "PLAY0103";

    /// <summary>
    /// A <c>data</c> line is not <c>data &lt;ReadModel&gt; via query &lt;Query&gt; [by &lt;param&gt;]</c>.
    /// </summary>
    public const string InvalidDataDirective = "PLAY0104";

    /// <summary>
    /// An <c>action</c> line is not <c>action &lt;Command&gt;</c>.
    /// </summary>
    public const string InvalidActionDirective = "PLAY0105";

    /// <summary>
    /// A line in an action body is neither <c>label</c> nor <c>navigate to</c>.
    /// </summary>
    public const string UnknownActionDirective = "PLAY0106";

    /// <summary>
    /// A navigation is not <c>navigate to &lt;Screen&gt; [by &lt;param&gt;]</c>.
    /// </summary>
    public const string InvalidNavigation = "PLAY0107";

    /// <summary>
    /// A line under a screen layout does not name a slot.
    /// </summary>
    public const string InvalidScreenLayoutSlot = "PLAY0108";

    /// <summary>
    /// A <c>title</c> line is not <c>title "&lt;text&gt;"</c>.
    /// </summary>
    public const string InvalidTitleDirective = "PLAY0109";

    /// <summary>
    /// A line in a table body is neither <c>column</c> nor <c>on row-click navigate to</c>.
    /// </summary>
    public const string UnknownTableDirective = "PLAY0110";

    /// <summary>
    /// A line in a summary body is not <c>field &lt;property&gt; label "&lt;text&gt;"</c>.
    /// </summary>
    public const string UnknownSummaryDirective = "PLAY0111";

    // Policies and authorization.

    /// <summary>
    /// A <c>policy</c> line is not <c>policy &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidPolicyDeclaration = "PLAY0112";

    /// <summary>
    /// A line in a policy body is neither <c>require</c> nor an inline code block.
    /// </summary>
    public const string UnknownPolicyDirective = "PLAY0113";

    /// <summary>
    /// A policy states nothing it requires of the caller.
    /// </summary>
    public const string PolicyWithoutRequirement = "PLAY0114";

    /// <summary>
    /// A policy condition holds a token the language has no reading for.
    /// </summary>
    public const string UnexpectedTokenInPolicyCondition = "PLAY0115";

    /// <summary>
    /// A policy requirement states no condition.
    /// </summary>
    public const string ExpectedPolicyCondition = "PLAY0116";

    /// <summary>
    /// A group opened in a policy condition is never closed.
    /// </summary>
    public const string UnclosedPolicyConditionGroup = "PLAY0117";

    /// <summary>
    /// A <c>role</c> requirement names no role.
    /// </summary>
    public const string ExpectedRoleName = "PLAY0118";

    /// <summary>
    /// A <c>claim</c> requirement names no claim.
    /// </summary>
    public const string ExpectedClaimName = "PLAY0119";

    /// <summary>
    /// A claim requirement does not say what the claim is matched against.
    /// </summary>
    public const string ExpectedClaimMatches = "PLAY0120";

    /// <summary>
    /// A claim match states nothing to match the claim to.
    /// </summary>
    public const string ExpectedClaimMatchTarget = "PLAY0121";

    /// <summary>
    /// An <c>authorize</c> clause names no policy.
    /// </summary>
    public const string AuthorizeWithoutPolicy = "PLAY0122";

    /// <summary>
    /// A policy is referred to by something that is not a policy name.
    /// </summary>
    public const string InvalidPolicyReference = "PLAY0123";

    // Authentication.

    /// <summary>
    /// An <c>authentication</c> line carries anything beyond the keyword.
    /// </summary>
    public const string InvalidAuthenticationDeclaration = "PLAY0124";

    /// <summary>
    /// A document declares more than one authentication block, and a document has at most one.
    /// </summary>
    public const string DuplicateAuthentication = "PLAY0125";

    /// <summary>
    /// A <c>provider</c> line is not <c>provider &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidProviderDeclaration = "PLAY0126";

    /// <summary>
    /// A setting of a provider is not <c>&lt;name&gt; &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidProviderSetting = "PLAY0127";

    // Event seeding.

    /// <summary>
    /// A <c>seed</c> line carries anything beyond the keyword.
    /// </summary>
    public const string InvalidSeedDeclaration = "PLAY0128";

    /// <summary>
    /// A seed group is not <c>for "&lt;event source id&gt;"</c>.
    /// </summary>
    public const string InvalidSeedGroup = "PLAY0129";

    /// <summary>
    /// A line in a seed group does not name an event type.
    /// </summary>
    public const string InvalidSeedEvent = "PLAY0130";

    /// <summary>
    /// A value a seeded event carries is not <c>&lt;property&gt; = &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidSeedPropertyAssignment = "PLAY0131";

    // Constraints.

    /// <summary>
    /// A <c>constraint</c> line is not <c>constraint &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidConstraintDeclaration = "PLAY0132";

    /// <summary>
    /// A constraint states nothing it holds the application to.
    /// </summary>
    public const string ConstraintWithoutRule = "PLAY0133";

    /// <summary>
    /// A line in a constraint body is not one the language can read.
    /// </summary>
    public const string InvalidConstraintBody = "PLAY0134";

    /// <summary>
    /// A constraint states more than one rule, and a constraint states one.
    /// </summary>
    public const string DuplicateConstraintBody = "PLAY0135";

    // Reactions.

    /// <summary>
    /// A <c>reaction</c> line is not <c>reaction &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidReactionDeclaration = "PLAY0136";

    /// <summary>
    /// A line in a reaction body is not a trigger the language reads.
    /// </summary>
    public const string InvalidReactionTrigger = "PLAY0137";

    /// <summary>
    /// A reaction states no trigger, so nothing ever sets it off.
    /// </summary>
    public const string ReactionWithoutTrigger = "PLAY0138";

    /// <summary>
    /// A line in a reaction trigger body opens with a word a trigger declares nothing by.
    /// </summary>
    public const string UnknownReactionTriggerDirective = "PLAY0139";

    // Validation rules.

    /// <summary>
    /// A <c>validate</c> line is neither <c>validate</c> nor <c>validate csharp</c>.
    /// </summary>
    public const string InvalidValidateDeclaration = "PLAY0140";

    /// <summary>
    /// A validation rule is not one the language can read.
    /// </summary>
    public const string InvalidValidationRule = "PLAY0141";

    /// <summary>
    /// A validation rule names a rule the language does not have.
    /// </summary>
    public const string UnknownValidationRule = "PLAY0142";

    /// <summary>
    /// A <c>rule</c> line does not name the rule with an identifier.
    /// </summary>
    public const string InvalidRuleName = "PLAY0143";

    /// <summary>
    /// A named rule names neither a <c>file</c> nor an inline code block.
    /// </summary>
    public const string UnknownRuleImplementationDirective = "PLAY0144";

    // Descriptions and tags.

    /// <summary>
    /// A <c>description</c> line is not <c>description "&lt;text&gt;"</c>.
    /// </summary>
    public const string InvalidDescription = "PLAY0145";

    /// <summary>
    /// A fenced description holds no text.
    /// </summary>
    public const string EmptyDescription = "PLAY0146";

    /// <summary>
    /// Something is described more than once, and a description is given once.
    /// </summary>
    public const string DuplicateDescription = "PLAY0147";

    /// <summary>
    /// A <c>tag</c> line carries no value.
    /// </summary>
    public const string TagWithoutValue = "PLAY0148";

    /// <summary>
    /// A tag value is neither an identifier, a string literal nor a context expression.
    /// </summary>
    public const string InvalidTagValue = "PLAY0149";

    // Expressions.

    /// <summary>
    /// A <c>literal</c> expression carries no value.
    /// </summary>
    public const string ExpectedLiteralValue = "PLAY0150";

    /// <summary>
    /// A $causedBy expression names a property the cause does not carry.
    /// </summary>
    public const string UnknownCausedByProperty = "PLAY0151";

    /// <summary>
    /// An expression is not one the language can read.
    /// </summary>
    public const string InvalidExpression = "PLAY0152";

    /// <summary>
    /// A $context path opens with a root the context does not have.
    /// </summary>
    public const string UnknownContextPath = "PLAY0153";

    /// <summary>
    /// A $context.causedBy path names a property the cause does not carry.
    /// </summary>
    public const string UnknownContextCausedByProperty = "PLAY0154";

    /// <summary>
    /// A $context.identity path names a property the identity does not carry.
    /// </summary>
    public const string UnknownContextIdentityProperty = "PLAY0155";

    /// <summary>
    /// A template expression is never closed.
    /// </summary>
    public const string UnterminatedTemplateExpression = "PLAY0156";

    /// <summary>
    /// An interpolation inside a template expression is never closed.
    /// </summary>
    public const string UnterminatedInterpolation = "PLAY0157";

    // Conditions.

    /// <summary>
    /// A condition holds a token the language has no reading for.
    /// </summary>
    public const string UnexpectedTokenInCondition = "PLAY0158";

    /// <summary>
    /// A condition is expected and nothing is written.
    /// </summary>
    public const string ExpectedCondition = "PLAY0159";

    /// <summary>
    /// A group opened in a condition is never closed.
    /// </summary>
    public const string UnclosedConditionGroup = "PLAY0160";

    /// <summary>
    /// A comparison states what is compared and not how.
    /// </summary>
    public const string ExpectedComparisonOperator = "PLAY0161";

    /// <summary>
    /// A comparison states nothing to compare against.
    /// </summary>
    public const string ExpectedComparisonValue = "PLAY0162";

    // Inline code.

    /// <summary>
    /// A construct opening an inline code block is followed by no fence.
    /// </summary>
    public const string ExpectedCodeFence = "PLAY0163";

    /// <summary>
    /// An inline code block is never closed.
    /// </summary>
    public const string UnclosedCodeBlock = "PLAY0164";

    // Names the document does not resolve.

    /// <summary>
    /// A property names a type nothing in the document or its imports declares.
    /// </summary>
    public const string UnknownType = "PLAY0165";

    /// <summary>
    /// An event is referred to that nothing in the document or its imports declares.
    /// </summary>
    public const string UnknownEvent = "PLAY0166";

    /// <summary>
    /// A policy is referred to that nothing in the document declares.
    /// </summary>
    public const string UnknownPolicy = "PLAY0167";

    /// <summary>
    /// A concept and a type, or two of either, are declared under one name, or an inline event repeats a payload property name.
    /// </summary>
    public const string DuplicateDeclaration = "PLAY0168";

    /// <summary>
    /// An authentication block declares two providers under one name.
    /// </summary>
    public const string DuplicateProvider = "PLAY0169";

    /// <summary>
    /// A seed block seeds nothing.
    /// </summary>
    public const string EmptySeed = "PLAY0170";

    /// <summary>
    /// A concurrency block narrows nothing.
    /// </summary>
    public const string EmptyConcurrency = "PLAY0171";

    // A folder compiled as one application.

    /// <summary>
    /// Two files of a folder each declare something the application has at most one of.
    /// </summary>
    public const string RepeatedSingularDeclarationAcrossFiles = "PLAY0172";

    /// <summary>
    /// Two files of a folder declare the same name.
    /// </summary>
    public const string RepeatedDeclarationAcrossFiles = "PLAY0173";

    /// <summary>
    /// Two files of a folder describe the same thing differently, and the first description is kept.
    /// </summary>
    public const string ConflictingDescriptionAcrossFiles = "PLAY0174";

    // What a command or reaction trigger reads to decide.

    /// <summary>
    /// A <c>reads</c> line is not <c>reads &lt;ReadModel&gt; [as &lt;alias&gt;] [by &lt;value&gt;]</c>, or uses a reserved alias.
    /// </summary>
    public const string InvalidReadsDeclaration = "PLAY0175";

    /// <summary>
    /// No longer reported. Repeated reads without aliases are reported as <c>PLAY0410</c>. Retained for compatibility.
    /// </summary>
    public const string DuplicateReads = "PLAY0176";

    /// <summary>
    /// A command or reaction trigger reads a read model no projection in the document produces.
    /// </summary>
    public const string UnknownReadModel = "PLAY0177";

    /// <summary>
    /// The <c>by</c> of a <c>reads</c> declaration does not name a property of the command.
    /// </summary>
    public const string UnknownReadsKey = "PLAY0178";

    // Rules about the whole artifact rather than one of its properties.

    /// <summary>
    /// A <c>require</c> rule carries no condition.
    /// </summary>
    public const string InvalidRequirement = "PLAY0179";

    /// <summary>
    /// The body of a <c>require</c> rule holds something other than its <c>message</c>.
    /// </summary>
    public const string UnknownRequirementDirective = "PLAY0180";

    /// <summary>
    /// A <c>require</c> operand is qualified by something the command does not read.
    /// </summary>
    public const string UnknownRequirementOperandSource = "PLAY0181";

    /// <summary>
    /// A <c>require</c> operand names neither a property of the artifact nor state it reads.
    /// </summary>
    public const string UnknownRequirementOperand = "PLAY0182";

    // How an application signs its users in.

    /// <summary>
    /// An authentication provider carries a configuration body, which belongs where the application runs.
    /// </summary>
    public const string ProviderWithConfiguration = "PLAY0183";

    // What an authorize requires.

    /// <summary>
    /// Tokens are left over after the requirement of an <c>authorize</c>.
    /// </summary>
    public const string UnexpectedTokenInAuthorize = "PLAY0184";

    /// <summary>
    /// A parenthesised group in an <c>authorize</c> is never closed.
    /// </summary>
    public const string UnclosedAuthorizeGroup = "PLAY0185";

    // Read models and the reducers that build them.

    /// <summary>
    /// A <c>readmodel</c> line is not <c>readmodel &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidReadModelDeclaration = "PLAY0186";

    /// <summary>
    /// A <c>reducer</c> line is not <c>reducer &lt;Name&gt; =&gt; &lt;ReadModel&gt;</c>.
    /// </summary>
    public const string InvalidReducerDeclaration = "PLAY0187";

    /// <summary>
    /// A line in a reducer body is not an <c>on &lt;EventType&gt;</c> rule.
    /// </summary>
    public const string InvalidReducerRule = "PLAY0188";

    /// <summary>
    /// A reducer declares no rule, so nothing it observes is stated.
    /// </summary>
    public const string ReducerWithoutRule = "PLAY0189";

    /// <summary>
    /// The body of a reducer rule holds something other than a description, a file or inline code.
    /// </summary>
    public const string UnknownReducerRuleDirective = "PLAY0190";

    /// <summary>
    /// More than one projection or reducer builds the same read model.
    /// </summary>
    public const string ReadModelBuiltMoreThanOnce = "PLAY0191";

    /// <summary>
    /// A document declares the same read model more than once.
    /// </summary>
    public const string DuplicateReadModel = "PLAY0192";

    // Where a produced event lands, and what a reaction does as a consequence.

    /// <summary>
    /// A <c>produces</c> declares more than one <c>for</c>, and an event is appended to one event source.
    /// </summary>
    public const string DuplicateProducesTarget = "PLAY0193";

    /// <summary>
    /// An <c>invokes</c> line is not <c>invokes &lt;Command&gt;</c>.
    /// </summary>
    public const string InvalidInvokesDeclaration = "PLAY0194";

    /// <summary>
    /// A reaction invokes a command the document does not declare.
    /// </summary>
    public const string UnknownCommand = "PLAY0195";

    // What a screen binds to.

    /// <summary>
    /// A screen binds data to a query nothing in scope declares.
    /// </summary>
    public const string UnknownQuery = "PLAY0196";

    /// <summary>
    /// A screen navigates to a screen nothing in scope declares.
    /// </summary>
    public const string UnknownScreen = "PLAY0197";

    /// <summary>
    /// A bare name matches more than one declaration at the same depth, so which one it means is undecided.
    /// </summary>
    public const string AmbiguousReference = "PLAY0198";

    // What a query's results are narrowed to.

    /// <summary>
    /// A <c>scoped</c> line is not <c>scoped to &lt;scope&gt;</c>.
    /// </summary>
    public const string InvalidScopeDeclaration = "PLAY0199";

    /// <summary>
    /// A query declares more than one scope, and results are narrowed one way.
    /// </summary>
    public const string DuplicateScope = "PLAY0200";

    // ui profile - the platform/size/package vocabulary a build targets.

    /// <summary>
    /// A <c>ui profile</c> line is not <c>ui profile &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidUiProfileDeclaration = "PLAY0201";

    /// <summary>
    /// Two <c>ui profile</c> blocks in the same document declare the same name.
    /// </summary>
    public const string DuplicateUiProfile = "PLAY0202";

    /// <summary>
    /// A <c>target</c> line under a <c>ui profile</c> is neither <c>target platform ...</c> nor <c>target size ...</c>.
    /// </summary>
    public const string InvalidTargetDeclaration = "PLAY0203";

    /// <summary>
    /// A <c>ui profile</c> declares <c>target platform</c> or <c>target size</c> more than once.
    /// </summary>
    public const string DuplicateTargetDeclaration = "PLAY0204";

    /// <summary>
    /// A line under a <c>packages</c> block is not a valid package name.
    /// </summary>
    public const string InvalidPackageName = "PLAY0205";

    /// <summary>
    /// A <c>ui profile</c>'s <c>packages</c> block lists the same package more than once.
    /// </summary>
    public const string DuplicatePackageDeclaration = "PLAY0206";

    /// <summary>
    /// A line in a <c>ui profile</c> body is not <c>target</c> or <c>packages</c>, or <c>packages</c> is declared more than once.
    /// </summary>
    public const string UnknownUiProfileDirective = "PLAY0207";

    // form - a named, command-bound input surface declared at module level.

    /// <summary>
    /// A <c>form</c> line is not <c>form &lt;Name&gt; for &lt;Command&gt;</c>.
    /// </summary>
    public const string InvalidFormDeclaration = "PLAY0208";

    /// <summary>
    /// Two <c>form</c> blocks in the same document declare the same name.
    /// </summary>
    public const string DuplicateForm = "PLAY0209";

    /// <summary>
    /// A line in a <c>form</c> body is not <c>populate</c>, <c>field</c> or <c>on submit</c>.
    /// </summary>
    public const string UnknownFormDirective = "PLAY0210";

    /// <summary>
    /// A <c>populate</c> line is neither <c>populate via query ...</c> nor <c>populate from item</c>.
    /// </summary>
    public const string InvalidPopulateDeclaration = "PLAY0211";

    /// <summary>
    /// A <c>form</c> declares <c>populate</c> more than once.
    /// </summary>
    public const string DuplicatePopulate = "PLAY0212";

    /// <summary>
    /// A <c>field</c> line is not <c>field &lt;property&gt; [from &lt;source&gt;|compose using &lt;Callback&gt;] [label "..."]</c>.
    /// </summary>
    public const string InvalidFormField = "PLAY0213";

    /// <summary>
    /// An <c>on submit</c> line is not <c>on submit navigate to &lt;Screen&gt; [by &lt;param&gt;]</c>.
    /// </summary>
    public const string InvalidFormSubmit = "PLAY0214";

    /// <summary>
    /// A <c>form</c> declares <c>on submit</c> more than once.
    /// </summary>
    public const string DuplicateFormSubmit = "PLAY0215";

    /// <summary>
    /// A <c>field</c> binds to a property its form's command does not declare.
    /// </summary>
    public const string UnknownFormFieldProperty = "PLAY0216";

    // contribute to - one item contributed into a named contribution point.

    /// <summary>
    /// A <c>contribute</c> line is not <c>contribute to &lt;ContributionPoint&gt;</c>.
    /// </summary>
    public const string InvalidContributionDeclaration = "PLAY0217";

    /// <summary>
    /// A line in a <c>contribute to</c> body is not <c>navigate</c>, <c>label</c> or <c>order</c>.
    /// </summary>
    public const string UnknownContributionDirective = "PLAY0218";

    /// <summary>
    /// A contribution declares <c>navigate to</c> more than once.
    /// </summary>
    public const string DuplicateContributionNavigate = "PLAY0219";

    /// <summary>
    /// A contribution declares <c>label</c> more than once.
    /// </summary>
    public const string DuplicateContributionLabel = "PLAY0220";

    /// <summary>
    /// A contribution declares <c>order</c> more than once.
    /// </summary>
    public const string DuplicateContributionOrder = "PLAY0221";

    /// <summary>
    /// A contribution's <c>label</c> line is not <c>label "..."</c> or <c>label $strings....</c>.
    /// </summary>
    public const string InvalidContributionLabel = "PLAY0222";

    /// <summary>
    /// A contribution's <c>order</c> line is not <c>order &lt;number&gt;</c>.
    /// </summary>
    public const string InvalidOrderDeclaration = "PLAY0223";

    /// <summary>
    /// A contribution names a contribution point nothing in scope declares.
    /// </summary>
    public const string UnknownContributionPoint = "PLAY0224";

    // theme - a named visual theme and the component packages it is compatible with.

    /// <summary>
    /// A <c>theme</c> line is not <c>theme &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidThemeDeclaration = "PLAY0225";

    /// <summary>
    /// Two <c>theme</c> blocks in the same document declare the same name.
    /// </summary>
    public const string DuplicateTheme = "PLAY0226";

    /// <summary>
    /// A line in a <c>theme</c> body is not <c>compatible with &lt;Package&gt;</c>.
    /// </summary>
    public const string InvalidCompatibleWithDeclaration = "PLAY0227";

    /// <summary>
    /// A <c>theme</c> declares compatibility with the same package more than once.
    /// </summary>
    public const string DuplicateCompatibleWith = "PLAY0228";

    /// <summary>
    /// A <c>ui profile</c>'s <c>theme</c> line is not <c>theme &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidProfileTheme = "PLAY0229";

    /// <summary>
    /// A <c>ui profile</c> declares <c>theme</c> more than once.
    /// </summary>
    public const string DuplicateProfileTheme = "PLAY0230";

    /// <summary>
    /// A <c>ui profile</c> selects a theme nothing in the document declares.
    /// </summary>
    public const string UnknownTheme = "PLAY0231";

    /// <summary>
    /// A <c>ui profile</c> selects a theme not declared compatible with one of the profile's own packages.
    /// </summary>
    public const string ThemeNotCompatibleWithPackage = "PLAY0232";

    // arrangement - flow (responsive row/column/grid) vs. freeform (pixel-precise) placement.

    /// <summary>
    /// An <c>arrangement</c> line is not <c>arrangement flow</c> or <c>arrangement freeform</c>.
    /// </summary>
    public const string InvalidArrangementDeclaration = "PLAY0233";

    /// <summary>
    /// A layout, screen template or dialog template declares <c>arrangement</c> more than once.
    /// </summary>
    public const string DuplicateArrangement = "PLAY0234";

    /// <summary>
    /// A <c>row</c>, <c>column</c> or <c>grid</c> line in an arrangement is malformed.
    /// </summary>
    public const string InvalidArrangementContainer = "PLAY0235";

    /// <summary>
    /// A slot leaf within an arrangement tree has malformed sizing attributes.
    /// </summary>
    public const string InvalidArrangementSlotAttributes = "PLAY0236";

    /// <summary>
    /// A <c>when</c> override line in an arrangement is not a valid width/height size-class condition.
    /// </summary>
    public const string InvalidArrangementOverride = "PLAY0237";

    /// <summary>
    /// An arrangement declares more than one <c>when</c> override for the same width/height size-class combination.
    /// </summary>
    public const string DuplicateArrangementOverride = "PLAY0238";

    /// <summary>
    /// An <c>arrangement</c> block's body does not match its mode - a <c>flow</c> arrangement declares a
    /// <c>variant</c>, or a <c>freeform</c> arrangement declares anything other than one.
    /// </summary>
    public const string ArrangementDirectiveMismatch = "PLAY0239";

    /// <summary>
    /// A <c>variant</c> line is not <c>variant width &lt;compact|regular&gt;, height &lt;compact|regular&gt;</c>.
    /// </summary>
    public const string InvalidVariantDeclaration = "PLAY0240";

    /// <summary>
    /// An arrangement declares more than one <c>variant</c> for the same width/height size-class combination.
    /// </summary>
    public const string DuplicateVariant = "PLAY0241";

    /// <summary>
    /// A <c>place</c> line is not <c>place &lt;Slot&gt; hidden</c> or <c>place &lt;Slot&gt; at x,y size w,h</c>.
    /// </summary>
    public const string InvalidPlaceDeclaration = "PLAY0242";

    /// <summary>
    /// A <c>variant</c> places (or hides) the same slot more than once.
    /// </summary>
    public const string DuplicatePlaceInVariant = "PLAY0243";

    /// <summary>
    /// A <c>freeform</c> arrangement's <c>variant</c> does not mention (place or hide) a slot another variant of the same arrangement places.
    /// </summary>
    public const string VariantMissingSlot = "PLAY0244";

    // Triggers.

    /// <summary>
    /// A <c>trigger</c> line is not <c>trigger &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidTriggerDeclaration = "PLAY0245";

    /// <summary>
    /// A line in a trigger body is neither a description nor a value the trigger provides.
    /// </summary>
    public const string InvalidTriggerData = "PLAY0246";

    /// <summary>
    /// The document declares two triggers by the same name, leaving no answer to which one a reaction means.
    /// </summary>
    public const string DuplicateTrigger = "PLAY0247";

    /// <summary>
    /// A <c>when</c> line names neither an event nor a trigger the document or the compiler knows.
    /// </summary>
    public const string UnknownTrigger = "PLAY0248";

    /// <summary>
    /// An <c>every</c> line is not <c>every &lt;n&gt; &lt;seconds|minutes|hours|days&gt;</c>.
    /// </summary>
    public const string InvalidIntervalTrigger = "PLAY0249";

    /// <summary>
    /// An <c>at</c> line is not <c>at &lt;HH:mm&gt;</c>, optionally followed by <c>on &lt;Weekday&gt;</c> or <c>on day &lt;n&gt;</c>.
    /// </summary>
    public const string InvalidScheduleTrigger = "PLAY0250";

    /// <summary>
    /// A reaction takes a value from an occurrence that the trigger does not provide.
    /// </summary>
    public const string UnknownTriggerData = "PLAY0251";

    /// <summary>
    /// A reaction states more than one <c>where</c>, and a reaction is narrowed by one condition.
    /// </summary>
    public const string DuplicateReactionCondition = "PLAY0252";

    /// <summary>
    /// A reaction declares the same trigger more than once, so the second says nothing the first did not.
    /// </summary>
    public const string DuplicateReactionTrigger = "PLAY0253";

    // The application's layout, and the screen and dialog templates that go inside it.

    /// <summary>
    /// A <c>screen template</c> line is not <c>screen template &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidScreenTemplateDeclaration = "PLAY0254";

    /// <summary>
    /// A <c>dialog template</c> line is not <c>dialog template &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidDialogTemplateDeclaration = "PLAY0255";

    /// <summary>
    /// A <c>fits slot</c> line is not <c>fits slot &lt;name&gt;</c>.
    /// </summary>
    public const string InvalidFitsSlotDeclaration = "PLAY0256";

    /// <summary>
    /// A screen template declares <c>fits slot</c> more than once.
    /// </summary>
    public const string DuplicateFitsSlot = "PLAY0257";

    /// <summary>
    /// A layout or a dialog template declares <c>fits slot</c> - neither fills a slot of a parent structure.
    /// </summary>
    public const string FitsSlotNotAllowed = "PLAY0258";

    /// <summary>
    /// A top level <c>layout</c> line is not <c>layout &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidLayoutDeclaration = "PLAY0259";

    /// <summary>
    /// A document declares more than one layout by the same name.
    /// </summary>
    public const string DuplicateLayout = "PLAY0260";

    /// <summary>
    /// A <c>ui profile</c>'s <c>layout</c> line is not <c>layout &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidProfileLayout = "PLAY0261";

    /// <summary>
    /// A <c>ui profile</c> declares <c>layout</c> more than once.
    /// </summary>
    public const string DuplicateProfileLayout = "PLAY0262";

    /// <summary>
    /// A <c>ui profile</c> selects a layout nothing in the document declares.
    /// </summary>
    public const string UnknownLayout = "PLAY0263";

    /// <summary>
    /// A <c>file</c> directive names an absolute path, and a file reference is relative to the repository root.
    /// </summary>
    public const string AbsoluteFileReference = "PLAY0264";

    /// <summary>
    /// A <c>then query</c> line does not name a query.
    /// </summary>
    public const string InvalidSpecificationQuery = "PLAY0265";

    /// <summary>
    /// A line in a <c>then query</c> body is neither <c>arguments</c> nor <c>result</c>.
    /// </summary>
    public const string UnknownSpecificationQueryDirective = "PLAY0266";

    /// <summary>
    /// A <c>then query</c> assertion declares its <c>arguments</c> block more than once.
    /// </summary>
    public const string DuplicateSpecificationQueryArguments = "PLAY0267";

    /// <summary>
    /// Source syntax carries portable behavior that ESM v1 cannot represent.
    /// </summary>
    public const string UnsupportedSemanticSyntax = "PLAY0268";

    /// <summary>
    /// Source syntax is explicitly deferred from the current backend semantic profile.
    /// </summary>
    public const string DeferredSemanticSyntax = "PLAY0269";

    /// <summary>
    /// Source syntax is realization or operational metadata rather than portable behavior.
    /// </summary>
    public const string ReportOnlySemanticSyntax = "PLAY0270";

    /// <summary>
    /// Source syntax keeps its legacy meaning but cannot be strengthened into ESM v1 implicitly.
    /// </summary>
    public const string PreservedLegacySemanticSyntax = "PLAY0271";

    /// <summary>
    /// Source syntax requires an explicit reviewed semantic migration before binding.
    /// </summary>
    public const string SemanticMigrationRequired = "PLAY0272";

    /// <summary>
    /// The source syntax and identity catalog could not produce a coherent semantic compilation.
    /// </summary>
    public const string InvalidSemanticBinding = "PLAY0273";

    /// <summary>
    /// A syntax location cannot be mapped to one of the supplied semantic source documents.
    /// </summary>
    public const string UnknownSemanticSourceDocument = "PLAY0274";

    /// <summary>
    /// A specification event-source assertion is missing or malformed.
    /// </summary>
    public const string InvalidSpecificationEventSource = "PLAY0275";

    /// <summary>
    /// A specification step declares its event-source assertion more than once.
    /// </summary>
    public const string DuplicateSpecificationEventSource = "PLAY0276";

    // Projection variants - mutually exclusive named read models sharing one projection identity.

    /// <summary>
    /// A <c>variant</c> line is not <c>variant &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidProjectionVariantDeclaration = "PLAY0277";

    /// <summary>
    /// A variant declares no <c>enters on</c> event, so nothing ever activates it.
    /// </summary>
    public const string ProjectionVariantWithoutEntersOn = "PLAY0278";

    /// <summary>
    /// An <c>enters on</c> line is not <c>enters on &lt;EventType&gt; [key &lt;expression&gt;]</c>.
    /// </summary>
    public const string InvalidEntersOnDeclaration = "PLAY0279";

    /// <summary>
    /// A <c>variant</c> is declared inside another variant, and variants do not nest.
    /// </summary>
    public const string NestedProjectionVariantNotAllowed = "PLAY0280";

    /// <summary>
    /// Two variants of the same projection declare the same name.
    /// </summary>
    public const string DuplicateProjectionVariantName = "PLAY0281";

    /// <summary>
    /// A declarative validation rule targets no declared command field.
    /// </summary>
    public const string UnknownValidationTarget = "PLAY0282";

    /// <summary>
    /// A read key's type is incompatible with every declared query parameter of its view.
    /// </summary>
    public const string IncompatibleReadsKey = "PLAY0283";

    /// <summary>
    /// No mapping in a child or nested projection populates a declared element field.
    /// </summary>
    public const string UnpopulatedProjectionField = "PLAY0284";

    /// <summary>
    /// A specification outcome contradicts every applicable declared producer mapping.
    /// </summary>
    public const string UnreachableSpecificationOutcome = "PLAY0285";

    /// <summary>
    /// A specification supplies a value outside its field's declared enumeration.
    /// </summary>
    public const string UnknownSpecificationEnumMember = "PLAY0286";

    /// <summary>
    /// A producer or specification assigns a field the referenced event does not declare.
    /// </summary>
    public const string UnknownEventField = "PLAY0287";

    /// <summary>
    /// A typed authoring change canonicalizes a document and may discard source comments or trivia.
    /// </summary>
    public const string AuthoringSourceNormalization = "PLAY0288";

    /// <summary>
    /// An empty authoring workspace has no source documents and is not executable.
    /// </summary>
    public const string EmptyAuthoringWorkspace = "PLAY0289";

    /// <summary>
    /// An <c>import</c> names something the application declares itself, so it has no effect.
    /// </summary>
    public const string RedundantImport = "PLAY0290";

    /// <summary>
    /// An inline JSON-shaped value is malformed.
    /// </summary>
    public const string InvalidStructuredValue = "PLAY0291";

    /// <summary>
    /// A structured value names no property in its declared composite target.
    /// </summary>
    public const string UnknownStructuredValueMember = "PLAY0292";

    /// <summary>
    /// A structured value has the wrong shape for its declared target.
    /// </summary>
    public const string IncompatibleStructuredValue = "PLAY0293";

    /// <summary>
    /// An inline object declares the same property name more than once.
    /// </summary>
    public const string DuplicateStructuredValueMember = "PLAY0294";

    // Event context paths, checked against Syntax.EventContextCatalog.

    /// <summary>
    /// An <c>$eventContext.&lt;path&gt;</c> opens with a member the event context does not have.
    /// </summary>
    public const string UnknownEventContextMember = "PLAY0295";

    /// <summary>
    /// An <c>$eventContext.&lt;path&gt;</c> continues into something the member before it does not have.
    /// </summary>
    public const string UnknownEventContextPath = "PLAY0296";

    /// <summary>
    /// An <c>$eventContext.&lt;path&gt;</c> continues below a collection - <c>causation</c> or <c>tags</c> - which has
    /// no addressing grammar and never resolves.
    /// </summary>
    public const string EventContextPathBelowCollection = "PLAY0297";

    /// <summary>
    /// An <c>$eventContext</c> reference names no member, or has an empty segment.
    /// </summary>
    public const string MissingEventContextPath = "PLAY0298";

    /// <summary>
    /// A dynamic dictionary key names a <c>$</c> source other than <c>$eventContext</c>, which the runtime never resolves.
    /// </summary>
    public const string UnresolvedDynamicKeySource = "PLAY0299";

    // PLAY0300-PLAY0349 is reserved for the interaction model - behaviors, interaction triggers, actions and
    // continuations. The range is contiguous and allocated up front so the constructs land as one coherent
    // revision rather than accreting codes in three places.

    /// <summary>
    /// A behavior declaration is not of the form <c>behavior &lt;Name&gt;</c>.
    /// </summary>
    public const string InvalidBehaviorDeclaration = "PLAY0300";

    /// <summary>
    /// A behavior name is declared more than once.
    /// </summary>
    public const string DuplicateBehavior = "PLAY0301";

    /// <summary>
    /// A line in a behavior body is not one of description, parameter, order or an <c>on</c> binding.
    /// </summary>
    public const string UnknownBehaviorDirective = "PLAY0302";

    /// <summary>
    /// A behavior parameter declaration is not of the form <c>parameter &lt;name&gt; [&lt;Type&gt;]</c>.
    /// </summary>
    public const string InvalidBehaviorParameter = "PLAY0303";

    /// <summary>
    /// A behavior declares the same parameter name more than once.
    /// </summary>
    public const string DuplicateBehaviorParameter = "PLAY0304";

    /// <summary>
    /// A behavior <c>order</c> is not an integer.
    /// </summary>
    public const string InvalidBehaviorOrder = "PLAY0305";

    /// <summary>
    /// A behavior declares no bindings, so nothing can ever run.
    /// </summary>
    public const string BehaviorWithoutBindings = "PLAY0306";

    /// <summary>
    /// An <c>on</c> clause does not name a built-in interaction kind, an event, an interval or a declared application trigger.
    /// </summary>
    public const string InvalidInteractionTrigger = "PLAY0307";

    /// <summary>
    /// An interaction binding declares no actions, so its trigger has nothing to do.
    /// </summary>
    public const string InteractionBindingWithoutActions = "PLAY0308";

    /// <summary>
    /// An interaction binding declares <c>where</c> more than once.
    /// </summary>
    public const string RepeatedInteractionCondition = "PLAY0309";

    /// <summary>
    /// A line where an action was expected does not name one of the action kinds.
    /// </summary>
    public const string UnknownInteractionAction = "PLAY0310";

    /// <summary>
    /// An <c>execute</c> action is not of the form <c>execute &lt;Command&gt;</c>.
    /// </summary>
    public const string InvalidExecuteAction = "PLAY0311";

    /// <summary>
    /// A <c>navigate</c> action is neither <c>navigate to &lt;Screen&gt;</c> nor <c>navigate back</c>.
    /// </summary>
    public const string InvalidNavigateAction = "PLAY0312";

    /// <summary>
    /// An <c>open dialog</c> action does not name a dialog template.
    /// </summary>
    public const string InvalidOpenDialogAction = "PLAY0313";

    /// <summary>
    /// A <c>refresh</c> action does not name a query.
    /// </summary>
    public const string InvalidRefreshAction = "PLAY0314";

    /// <summary>
    /// A <c>set</c> action is not of the form <c>set &lt;target&gt; to &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidSetAction = "PLAY0315";

    /// <summary>
    /// A <c>notify</c> action is not of the form <c>notify &lt;info|warning|error&gt; "&lt;text&gt;"</c>.
    /// </summary>
    public const string InvalidNotifyAction = "PLAY0316";

    /// <summary>
    /// A <c>confirm</c> action does not carry a message.
    /// </summary>
    public const string InvalidConfirmAction = "PLAY0317";

    /// <summary>
    /// A <c>raise</c> action does not name an application trigger.
    /// </summary>
    public const string InvalidRaiseAction = "PLAY0318";

    /// <summary>
    /// An action argument is not of the form <c>with &lt;name&gt; from &lt;binding&gt;</c>.
    /// </summary>
    public const string InvalidInteractionArgument = "PLAY0319";

    /// <summary>
    /// A continuation is attached to an action that cannot fail, so it could never run.
    /// </summary>
    public const string ContinuationOnNonFailableAction = "PLAY0320";

    /// <summary>
    /// An <c>on result</c> continuation is attached to something other than <c>open dialog</c>.
    /// </summary>
    public const string ResultContinuationOnNonDialogAction = "PLAY0321";

    /// <summary>
    /// A <c>uses</c> clause is not of the form <c>uses &lt;Behavior&gt;</c>.
    /// </summary>
    public const string InvalidUsesDeclaration = "PLAY0322";

    /// <summary>
    /// An argument at a <c>uses</c> site is not of the form <c>&lt;parameter&gt; &lt;value&gt;</c>.
    /// </summary>
    public const string InvalidBehaviorArgument = "PLAY0323";

    /// <summary>
    /// Interaction nesting went deeper than the compiler admits.
    /// </summary>
    public const string InteractionNestingTooDeep = "PLAY0324";

    /// <summary>
    /// An <c>interval</c> trigger is below the floor a client can honour.
    /// </summary>
    public const string IntervalBelowFloor = "PLAY0325";

    /// <summary>
    /// An application trigger is declared with a name reserved as a built-in interaction kind.
    /// </summary>
    public const string ApplicationTriggerCollidesWithInteractionKind = "PLAY0326";

    /// <summary>
    /// An action names a command the document does not declare.
    /// </summary>
    public const string UnknownActionCommand = "PLAY0330";

    /// <summary>
    /// A <c>navigate to</c> action names a screen the document does not declare.
    /// </summary>
    public const string UnknownActionScreen = "PLAY0331";

    /// <summary>
    /// A <c>refresh</c> action names a query the document does not declare.
    /// </summary>
    public const string UnknownActionQuery = "PLAY0332";

    /// <summary>
    /// An <c>open dialog</c> action names a dialog template the document does not declare.
    /// </summary>
    public const string UnknownActionDialogTemplate = "PLAY0333";

    /// <summary>
    /// A <c>raise</c> action or an <c>on</c> clause names an application trigger the document does not declare.
    /// </summary>
    public const string UnknownActionTrigger = "PLAY0334";

    /// <summary>
    /// An <c>on event</c> clause names an event the document does not declare.
    /// </summary>
    public const string UnknownInteractionEvent = "PLAY0335";

    /// <summary>
    /// A <c>uses</c> clause names a behavior the document does not declare.
    /// </summary>
    public const string UnknownUsedBehavior = "PLAY0336";

    /// <summary>
    /// A <c>uses</c> site supplies an argument the behavior does not declare a parameter for.
    /// </summary>
    public const string UnknownBehaviorArgument = "PLAY0337";

    /// <summary>
    /// A <c>uses</c> site leaves a behavior parameter without an argument.
    /// </summary>
    public const string MissingBehaviorArgument = "PLAY0338";

    /// <summary>
    /// Actions follow an unconditional navigation, so they could never run.
    /// </summary>
    public const string UnreachableInteractionContinuation = "PLAY0339";

    /// <summary>
    /// Another file of a folder repeats an attachment of a module or feature - the same behavior with the same
    /// arguments, or an identical inline behavior - so the repeat is ignored.
    /// </summary>
    public const string DuplicateBehaviorAttachment = "PLAY0340";

    /// <summary>
    /// A guarded action child is not an alternative, fallback or navigation.
    /// </summary>
    public const string InvalidActionAlternative = "PLAY0341";

    /// <summary>
    /// A guarded action declares no condition alternatives.
    /// </summary>
    public const string GuardedActionWithoutAlternatives = "PLAY0342";

    /// <summary>
    /// A guarded action repeats its fallback or declares an alternative after it.
    /// </summary>
    public const string MisplacedActionOtherwise = "PLAY0343";

    /// <summary>
    /// A guarded action condition uses an unsupported operand or operator value type.
    /// </summary>
    public const string UnsupportedActionConditionOperand = "PLAY0344";

    /// <summary>
    /// An item path names no subject field or crosses a collection field.
    /// </summary>
    public const string UnknownActionSubjectField = "PLAY0345";

    /// <summary>
    /// A guarded action has no unambiguous data subject in its enclosing containers.
    /// </summary>
    public const string UnresolvedActionSubject = "PLAY0346";

    /// <summary>
    /// Earlier guarded alternatives provably shadow an alternative.
    /// </summary>
    public const string UnreachableActionAlternative = "PLAY0347";

    /// <summary>
    /// An explicit action argument names no property of the chosen command or has mismatched collection cardinality.
    /// </summary>
    public const string UnknownActionArgumentProperty = "PLAY0348";

    /// <summary>
    /// A command or event specification value uses null instead of modeling an optional fact as a separate event.
    /// </summary>
    public const string NullSpecificationFact = "PLAY0350";

    /// <summary>
    /// A read model state does not state its identifier property.
    /// </summary>
    public const string MissingSpecificationReadModelIdentifier = "PLAY0351";

    /// <summary>
    /// A specification without a command has an incompatible or missing outcome.
    /// </summary>
    public const string InvalidWhenlessSpecification = "PLAY0352";

    /// <summary>
    /// A specification value is null where its declared type or role disallows null.
    /// </summary>
    public const string InvalidSpecificationNull = "PLAY0353";

    /// <summary>
    /// A specification composite value omits a required declared property.
    /// </summary>
    public const string MissingStructuredValueMember = "PLAY0354";

    /// <summary>
    /// The declared composite target of a specification value could not be resolved.
    /// </summary>
    public const string UnresolvedStructuredValueType = "PLAY0355";

    /// <summary>
    /// A specification composite value declares the same member more than once in typed syntax.
    /// </summary>
    public const string DuplicateSemanticValueMember = "PLAY0356";

    /// <summary>
    /// A localized semantic message does not name a valid dotted string key.
    /// </summary>
    public const string InvalidSemanticStringKey = "PLAY0357";

    /// <summary>A specification declares more than one action.</summary>
    public const string ConflictingSpecificationActions = "PLAY0358";

    /// <summary>An event-order comparison qualifier is malformed or repeated.</summary>
    public const string InvalidSpecificationEventOrder = "PLAY0359";

    /// <summary>
    /// A named match pattern has no portable definition.
    /// </summary>
    public const string UnknownMatchPattern = "PLAY0366";

    /// <summary>
    /// A quoted match pattern is not a valid ECMAScript regular expression.
    /// </summary>
    public const string InvalidMatchPattern = "PLAY0367";

    /// <summary>
    /// A validation rule names a severity other than information, warning or error.
    /// </summary>
    public const string InvalidValidationSeverity = "PLAY0368";

    /// <summary>
    /// A command requirement has an invalid or repeated severity directive.
    /// </summary>
    public const string InvalidRequirementSeverity = "PLAY0369";

    /// <summary>
    /// A projection construct binds, but Chronicle's projection lowering drops part of it: <c>all</c> inside a
    /// <c>children</c> or <c>nested</c> block loses its subscription to every event type, and an auto-map setting on a
    /// joined event is replaced by the level's.
    /// </summary>
    public const string PartiallyLoweredProjectionSyntax = "PLAY0380";

    /// <summary>
    /// A projection-level key is accepted by the parser but is not used to route events by Chronicle.
    /// </summary>
    public const string UnusedProjectionKey = "PLAY0381";

    /// <summary>
    /// A projection variant has no entering event.
    /// </summary>
    public const string VariantRequiresEnteringEvent = "PLAY0382";

    /// <summary>
    /// A shared projection handler maps a member absent from a known variant shape.
    /// </summary>
    public const string GlobalHandlerPropertyNotOnVariant = "PLAY0383";

    /// <summary>
    /// Two variants of the same projection use the same name.
    /// </summary>
    public const string DuplicateProjectionVariant = "PLAY0384";

    /// <summary>
    /// An entering event activates more than one variant of the same projection.
    /// </summary>
    public const string DuplicateVariantEnteringEvent = "PLAY0385";

    /// <summary>A given caller fixture line is malformed.</summary>
    public const string InvalidSpecificationCaller = "PLAY0386";

    /// <summary>A specification declares more than one caller or denied outcome.</summary>
    public const string DuplicateSpecificationCallerOrDenied = "PLAY0387";

    /// <summary>A denied outcome has an invalid shape or conflicts with another outcome.</summary>
    public const string InvalidSpecificationDenied = "PLAY0388";

    /// <summary>An authorized specification has no explicitly supplied caller fixture.</summary>
    public const string MissingSpecificationCaller = "PLAY0389";

    // Constraints in the semantic model.

    /// <summary>
    /// A <c>unique</c> constraint names an event the application does not declare.
    /// </summary>
    public const string UnknownConstraintEvent = "PLAY0390";

    /// <summary>
    /// A <c>unique &lt;property&gt; on &lt;Event&gt;</c> constraint names a property the event does not declare.
    /// </summary>
    public const string UnknownConstraintProperty = "PLAY0391";

    /// <summary>
    /// Two constraints share a name. The name is a constraint's identity in the event store, so it is unique
    /// across the whole application.
    /// </summary>
    public const string DuplicateConstraintName = "PLAY0392";

    /// <summary>
    /// Ignoring casing is only meaningful for unique property values, not unique event occurrences.
    /// </summary>
    public const string InvalidConstraintCasing = "PLAY0393";

    /// <summary>
    /// Another file repeats an identical authorization gate on the same module or feature.
    /// </summary>
    public const string DuplicateAuthorizationAcrossFiles = "PLAY0394";

    /// <summary>
    /// A file-backed Chronicle constraint can only declare uniqueness; use portable unique syntax instead.
    /// </summary>
    public const string FileConstraintOnlySupportsUniqueness = "PLAY0396";

    /// <summary>
    /// An inline code block uses a legacy language line, or a description uses a bare fence.
    /// </summary>
    public const string LegacyInlineCodeFence = "PLAY0397";

    /// <summary>
    /// A reducer contains both implemented and unimplemented event transitions.
    /// </summary>
    public const string IncompleteReducerTransitions = "PLAY0398";

    /// <summary>
    /// A reducer observes the same resolved event more than once.
    /// </summary>
    public const string DuplicateReducerEvent = "PLAY0399";

    /// <summary>
    /// Multiple reads of one view require an alias on every instance.
    /// </summary>
    public const string MissingReadsAlias = "PLAY0410";

    /// <summary>
    /// Two reads in one command or reaction trigger use the same alias.
    /// </summary>
    public const string DuplicateReadsAlias = "PLAY0411";

    /// <summary>
    /// A reads alias has the same name as a command property or reaction trigger value.
    /// </summary>
    public const string ReadsAliasConflictsWithProperty = "PLAY0412";

    /// <summary>An implementation file path is absolute, escapes the root, or is not portable.</summary>
    public const string AttachmentPathRefused = "PLAY0430";

    /// <summary>An implementation file or one of its parent directories is a symbolic link or reparse point.</summary>
    public const string AttachmentLinkRefused = "PLAY0431";

    /// <summary>An implementation file does not exist beneath the root.</summary>
    public const string AttachmentMissing = "PLAY0432";

    /// <summary>An implementation file exceeds the per-file or aggregate size limit.</summary>
    public const string AttachmentTooLarge = "PLAY0433";

    /// <summary>An implementation file cannot safely be read as UTF-8 text.</summary>
    public const string AttachmentUnreadable = "PLAY0434";

    /// <summary>A policy combines a require condition with a file or inline code implementation.</summary>
    public const string MixedPolicyImplementation = "PLAY0440";

    /// <summary>A policy declares more than one require line.</summary>
    public const string RepeatedPolicyRequirement = "PLAY0441";

    /// <summary>
    /// A reaction reads by a value its trigger does not take.
    /// </summary>
    public const string UnknownReactionReadsKey = "PLAY0442";

    /// <summary>
    /// A clock trigger reads by a value, but clock triggers take no values.
    /// </summary>
    public const string ClockTriggerReadsKey = "PLAY0443";

    /// <summary>
    /// A trigger read uses a primitive type name that could be a trigger value named <c>reads</c>.
    /// </summary>
    public const string AmbiguousReactionReadsValue = "PLAY0444";

    /// <summary>A flat ESM projection transition uses a deprecated optional or many affected-instance cardinality.</summary>
    public const string DeprecatedProjectionTransitionCardinality = "PLAY0445";

    /// <summary>An event generation is zero, out of range, or uses Chronicle's reserved unspecified value.</summary>
    public const string InvalidEventGeneration = "PLAY0446";

    /// <summary>An event declares the same generation twice in one slice.</summary>
    public const string DuplicateEventGeneration = "PLAY0447";

    /// <summary>An event does not declare every generation from 1 to its current generation in one slice.</summary>
    public const string MissingEventGeneration = "PLAY0448";

    /// <summary>The executable model does not yet admit event generations without migration semantics.</summary>
    public const string UnsupportedEventGenerationSemantics = "PLAY0449";

    /// <summary>A clock trigger takes a value, but clock occurrences have no values.</summary>
    public const string ClockTriggerValue = "PLAY0450";

    /// <summary>A reads declaration has child lines, but reads takes no body.</summary>
    public const string ReadsWithChildren = "PLAY0451";

    /// <summary>More than one automap setting appears in a projection block; only the last setting applies.</summary>
    public const string RepeatedProjectionAutoMap = "PLAY0452";

    /// <summary>An absent read-model assertion is malformed or has child mappings.</summary>
    public const string InvalidAbsentReadModelStep = "PLAY0453";

    /// <summary>An <c>import</c> inside a module or feature does not name files as <c>import "&lt;path or glob&gt;"</c>.</summary>
    public const string InvalidFileImport = "PLAY0454";

    /// <summary>A file import pattern with wildcards matches no <c>.play</c> file.</summary>
    public const string FileImportMatchesNothing = "PLAY0455";

    /// <summary>A file import names one file, without wildcards, and that file does not exist.</summary>
    public const string ImportedFileNotFound = "PLAY0456";

    /// <summary>Two imports place the same file in scopes where neither lies inside the other.</summary>
    public const string ConflictingImportPlacement = "PLAY0457";

    /// <summary>Imports place a file inside itself, so its placement never settles.</summary>
    public const string ImportCycle = "PLAY0458";

    /// <summary>A file imported into a module or feature declares a module other than the one it is placed in.</summary>
    public const string ModuleInPlacedFile = "PLAY0459";

    /// <summary>The top level of a file imported into a module or feature holds something that scope cannot.</summary>
    public const string UnexpectedInPlacedFile = "PLAY0460";

    /// <summary>A <c>given clock</c> or <c>when clock</c> does not state one ISO 8601 instant, or <c>given clock</c> is repeated.</summary>
    public const string InvalidSpecificationClock = "PLAY0461";

    /// <summary>A <c>when trigger</c> line is not <c>when trigger &lt;Trigger&gt;</c>.</summary>
    public const string InvalidSpecificationTrigger = "PLAY0462";

    /// <summary>A <c>given capture</c> or <c>when capture</c> line is not <c>&lt;given|when&gt; capture &lt;Capture&gt;</c>.</summary>
    public const string InvalidSpecificationCapture = "PLAY0463";

    /// <summary>A <c>when query</c> line is not <c>when query &lt;Query&gt;</c>, or a <c>then result</c> line is not <c>then result [exactly]</c>.</summary>
    public const string InvalidSpecificationQueryAction = "PLAY0464";

    /// <summary>A query result is asserted without <c>when query</c>, or <c>when query</c> asserts no result, no empty result and no denial.</summary>
    public const string MismatchedSpecificationQueryResult = "PLAY0465";

    /// <summary>A <c>when trigger</c> names a trigger nothing declares or registers, or a value its declaration does not carry.</summary>
    public const string UnknownSpecificationTrigger = "PLAY0466";

    /// <summary>A <c>given capture</c> or <c>when capture</c> names a capture the application does not declare.</summary>
    public const string UnknownSpecificationCapture = "PLAY0467";

    /// <summary>A <c>when query</c> argument is not a <c>by</c> or <c>filter</c> parameter of the query.</summary>
    public const string UnknownSpecificationQueryArgument = "PLAY0468";

    /// <summary>
    /// The command identifier is also copied into its same-source event payload.
    /// </summary>
    public const string EventSourceIdInPayload = "PLAY0469";

    /// <summary>
    /// Mixed event sources require explicit destinations on every production.
    /// </summary>
    public const string ExplicitProducesTargetsRequired = "PLAY0470";

    /// <summary>
    /// An event identity pin repeats the current name.
    /// </summary>
    public const string RedundantEventId = "PLAY0471";

    /// <summary>
    /// An event identity pin is malformed or duplicated.
    /// </summary>
    public const string InvalidEventId = "PLAY0472";

    /// <summary>
    /// An inline event collides with another declaration or import.
    /// </summary>
    public const string InlineEventCollision = "PLAY0473";

    /// <summary>
    /// An inline event is declared outside a command.
    /// </summary>
    public const string InlineEventOutsideCommand = "PLAY0474";

    /// <summary>
    /// An inline event declares a generation instead of being extracted first.
    /// </summary>
    public const string InlineEventGeneration = "PLAY0475";

    /// <summary>
    /// A production supplies system-assigned metadata or an inline origin.
    /// </summary>
    public const string ReservedProductionMetadata = "PLAY0476";

    /// <summary>
    /// Event documentation is not a single nonempty fenced Markdown block.
    /// </summary>
    public const string InvalidEventDocumentation = "PLAY0477";

    /// <summary>A plain production omits its destination although the command has an identifier.</summary>
    public const string OmittedProductionDestination = "PLAY0478";

    /// <summary>
    /// A type reference uses the legacy optional suffix.
    /// </summary>
    public const string LegacyOptionalSuffix = "PLAY0479";

    /// <summary>
    /// The optional modifier follows identifier instead of the type.
    /// </summary>
    public const string InvalidOptionalModifierOrder = "PLAY0480";

    /// <summary>
    /// An optional read requests absence semantics that are not yet supported.
    /// </summary>
    public const string OptionalReadsNotSupported = "PLAY0481";

    /// <summary>
    /// A generated property is declared outside a command.
    /// </summary>
    public const string GeneratedPropertyOutsideCommand = "PLAY0482";

    /// <summary>
    /// A generated property is not a required scalar Uuid-backed concept.
    /// </summary>
    public const string InvalidGeneratedType = "PLAY0483";

    /// <summary>
    /// Property modifiers are repeated or out of order.
    /// </summary>
    public const string InvalidGeneratedModifierOrder = "PLAY0484";

    /// <summary>
    /// A generated property is supplied as request or form input.
    /// </summary>
    public const string GeneratedPropertySuppliedAsInput = "PLAY0485";

    /// <summary>
    /// A response is malformed, empty, repeated or conditional.
    /// </summary>
    public const string InvalidCommandResponse = "PLAY0486";

    /// <summary>
    /// A response source is unknown or is not a direct command property.
    /// </summary>
    public const string InvalidResponseSource = "PLAY0487";

    /// <summary>
    /// A record response repeats a field name.
    /// </summary>
    public const string DuplicateResponseField = "PLAY0488";

    /// <summary>
    /// A response has an unsupported shape or mismatched explicit type.
    /// </summary>
    public const string InvalidResponseShape = "PLAY0489";

    /// <summary>
    /// A generated fixture is invalid.
    /// </summary>
    public const string InvalidGeneratedFixture = "PLAY0490";

    /// <summary>
    /// A return expectation is invalid or conflicts with another outcome.
    /// </summary>
    public const string InvalidReturnExpectation = "PLAY0491";

    /// <summary>
    /// Implementation wrapper syntax is invalid.
    /// </summary>
    public const string InvalidImplementationBlock = "PLAY0492";

    /// <summary>
    /// An implementation hint is not one nonblank quoted string.
    /// </summary>
    public const string InvalidImplementationHint = "PLAY0493";

    /// <summary>
    /// Wrapped and direct sources conflict, or multiple payloads were supplied.
    /// </summary>
    public const string ConflictingImplementationSources = "PLAY0494";

    /// <summary>
    /// A system declaration is malformed.
    /// </summary>
    public const string InvalidSystemDeclaration = "PLAY0495";

    /// <summary>
    /// An operation declaration is malformed.
    /// </summary>
    public const string InvalidOperationDeclaration = "PLAY0496";

    /// <summary>
    /// A production reference is unresolved, ambiguous or qualifies an event.
    /// </summary>
    public const string InvalidProductionReference = "PLAY0497";

    /// <summary>
    /// Event and operation declarations collide in a slice.
    /// </summary>
    public const string ProductionDeclarationCollision = "PLAY0498";

    /// <summary>
    /// An operation production appears outside a command.
    /// </summary>
    public const string OperationOutsideCommand = "PLAY0499";

    /// <summary>
    /// An operation's system reference is missing, repeated or unknown.
    /// </summary>
    public const string InvalidSystemReference = "PLAY0500";

    /// <summary>
    /// An operation input mapping is invalid.
    /// </summary>
    public const string InvalidOperationMapping = "PLAY0501";

    /// <summary>
    /// An operation specification step is invalid.
    /// </summary>
    public const string InvalidOperationSpecification = "PLAY0502";

    /// <summary>
    /// A numeric mode directive has an unknown or malformed spelling.
    /// </summary>
    public const string InvalidNumericDirective = "PLAY0508";

    /// <summary>
    /// A document repeats its numeric preamble.
    /// </summary>
    public const string DuplicateNumericDirective = "PLAY0509";

    /// <summary>
    /// A numeric preamble follows domain, imports or declarations.
    /// </summary>
    public const string LateNumericDirective = "PLAY0510";

    /// <summary>
    /// A complete numeric literal is outside the exact Decimal domain.
    /// </summary>
    public const string InexactNumericLiteral = "PLAY0511";

    /// <summary>
    /// Physical documents disagree on their numeric interpretation.
    /// </summary>
    public const string MixedNumericModes = "PLAY0512";

    /// <summary>
    /// Syntax options or inserted numeric values disagree with the owning mode.
    /// </summary>
    public const string IncompatibleNumericSource = "PLAY0513";

    /// <summary>
    /// A slice uses an event or reads a read model from a slice drawn after it on the timeline.
    /// Reads built from events produced on the reader's side of the lowest common container are excluded.
    /// </summary>
    public const string EventFromLaterSlice = "PLAY0516";

    /// <summary>
    /// A timeline group depends on each other's events or non-feedback read models;
    /// reordering cannot make every dependency flow left to right.
    /// </summary>
    public const string TimelineCycleGroup = "PLAY0517";

    /// <summary>
    /// A typed specification example declaration is malformed.
    /// </summary>
    public const string InvalidSpecificationExample = "PLAY0518";

    /// <summary>
    /// A specification fixture assigns the same property more than once.
    /// </summary>
    public const string DuplicateSpecificationAssignment = "PLAY0519";

    /// <summary>
    /// A specification example type or reference is unknown, ambiguous, or unsupported.
    /// </summary>
    public const string UnresolvedSpecificationExampleType = "PLAY0520";

    /// <summary>
    /// A specification example name collides with a type or another example in its scope.
    /// </summary>
    public const string SpecificationExampleNameCollision = "PLAY0521";

    /// <summary>
    /// A specification step uses an example of another kind.
    /// </summary>
    public const string SpecificationExampleKindMismatch = "PLAY0522";

    /// <summary>
    /// A specification example supplies a value not allowed by its current type.
    /// </summary>
    public const string InvalidSpecificationExampleValue = "PLAY0523";

    /// <summary>
    /// An exact-shape specification step omits a required property after example expansion.
    /// </summary>
    public const string MissingSpecificationProperty = "PLAY0524";

    /// <summary>
    /// A stated example value or destination cannot be admitted by its semantic type, even if unused.
    /// </summary>
    public const string UnadmittedSpecificationExampleValue = "PLAY0525";

    /// <summary>
    /// A typed specification example declares route metadata instead of stating it on a step.
    /// </summary>
    public const string InvalidSpecificationExampleBody = "PLAY0526";

    /// <summary>
    /// A specification routing directive is malformed, duplicated or misplaced.
    /// </summary>
    public const string InvalidSpecificationStream = "PLAY0547";

    /// <summary>
    /// A command occurrence cannot declare its own route.
    /// </summary>
    public const string SpecificationStreamOnCommand = "PLAY0548";

    /// <summary>
    /// A specification route or stream id cannot resolve or is incompatible.
    /// </summary>
    public const string InvalidSpecificationStreamRoute = "PLAY0549";

    /// <summary>
    /// A routed occurrence lacks a concrete compatible source identity.
    /// </summary>
    public const string InvalidSpecificationStreamEventSource = "PLAY0550";

    /// <summary>
    /// An exclusive producer contradicts its expected occurrence route.
    /// </summary>
    public const string SpecificationStreamContradictsCommand = "PLAY0551";

    /// <summary>An event source or stream declaration is invalid or ambiguous.</summary>
    public const string InvalidEventSourceDeclaration = "PLAY0503";

    /// <summary>A command stream reference or mapping is invalid.</summary>
    public const string InvalidCommandStream = "PLAY0504";

    /// <summary>A command header has viable property and route interpretations.</summary>
    public const string AmbiguousCommandStream = "PLAY0505";

    /// <summary>A stream id type is neither text/UUID (or their concepts) nor an integer-backed concept.</summary>
    public const string UnsupportedStreamIdType = "PLAY0506";

    /// <summary>A rename pin repeats a source or stream's current name.</summary>
    public const string RedundantSourceStreamId = "PLAY0507";

    /// <summary>A projection maps to a property absent from the declared read-model shape.</summary>
    public const string UnknownReadModelProperty = "PLAY0514";

    /// <summary>
    /// A concept marked @pii or @sensitive is used as an event source identifier.
    /// </summary>
    public const string PiiNotSupportedOnIdentifier = "PLAY0515";

    /// <summary>
    /// A no-event assertion is malformed or conflicts with another outcome or an append action.
    /// </summary>
    public const string InvalidNoEventsExpectation = "PLAY0545";

    /// <summary>
    /// A refusal branch header is malformed or appears outside a command invocation.
    /// </summary>
    public const string InvalidRefusalBranch = "PLAY0538";

    /// <summary>
    /// A refusal branch does not contain either acknowledge alone or event productions.
    /// </summary>
    public const string InvalidRefusalBranchBody = "PLAY0539";

    /// <summary>
    /// A refusal branch is shadowed by an earlier branch or cannot target the invoked command's events.
    /// </summary>
    public const string UnreachableRefusalBranch = "PLAY0540";

    /// <summary>
    /// A refusal value has an invalid member, scope or target type.
    /// </summary>
    public const string InvalidRefusalValue = "PLAY0541";

    /// <summary>
    /// A named refusal constraint is not declared.
    /// </summary>
    public const string UnknownRefusalConstraint = "PLAY0542";

    /// <summary>
    /// A redelivery action is malformed or does not identify exactly one given event occurrence.
    /// </summary>
    public const string UnmatchedRedeliveredOccurrence = "PLAY0543";

    /// <summary>
    /// A redelivery reaction is unknown, ambiguous or does not observe the stated event.
    /// </summary>
    public const string UnknownRedeliveryReaction = "PLAY0544";

    /// <summary>
    /// A negated claim comparison uses a target that can be absent, null, or is not a string type.
    /// </summary>
    public const string IndeterminateNegatedClaimTarget = "PLAY0546";

    /// <summary>
    /// Visible screen data bindings share a name but disagree on query, cardinality or parameter.
    /// </summary>
    public const string ConflictingScreenDataBinding = "PLAY0530";

    /// <summary>
    /// A screen data binding disagrees with the resolved query return shape.
    /// </summary>
    public const string ScreenDataQueryMismatch = "PLAY0531";

    /// <summary>
    /// A screen action has no command-bound input surface.
    /// </summary>
    public const string ActionWithoutInputSurface = "PLAY0532";

    /// <summary>
    /// A StateChange command has no UI issuer and is not reaction-invoked.
    /// </summary>
    public const string CommandWithoutInputSurface = "PLAY0533";

    /// <summary>
    /// A read model has no builder or a declared field has no projection origin.
    /// </summary>
    public const string ReadModelFieldWithoutOrigin = "PLAY0534";

    /// <summary>
    /// A query parameter cannot be held by its view's structurally known identity or fields.
    /// </summary>
    public const string QueryParameterNotHeldByView = "PLAY0535";

    /// <summary>
    /// A locally declared event has no declared application consumer.
    /// </summary>
    public const string UnconsumedEvent = "PLAY0536";

    /// <summary>
    /// A screen is unreachable from contributions or attached shell-level behaviors.
    /// </summary>
    public const string UnreachableScreen = "PLAY0537";

    /// <summary>
    /// A declared dependency target is unresolved, self, an ancestor, or a descendant.
    /// </summary>
    public const string InvalidDependencyTarget = "PLAY0554";

    /// <summary>
    /// A dependency is declared more than once on the same container.
    /// </summary>
    public const string RepeatedDependencyDeclaration = "PLAY0555";

    /// <summary>
    /// An opted-in container uses a producer it did not declare.
    /// </summary>
    public const string UndeclaredDependency = "PLAY0552";

    /// <summary>
    /// No counted explicit reference uses a declared dependency.
    /// </summary>
    public const string UnusedDependencyDeclaration = "PLAY0553";

    /// <summary>
    /// Two containers declare each other as dependencies.
    /// </summary>
    public const string MutualDependencyDeclarations = "PLAY0556";

    /// <summary>
    /// A documentation directive requires one nonempty fenced markdown block.
    /// </summary>
    public const string InvalidDocumentation = "PLAY0558";

    /// <summary>
    /// Files give conflicting documentation for the same module or feature.
    /// </summary>
    public const string ConflictingDocumentationAcrossFiles = "PLAY0559";
}
