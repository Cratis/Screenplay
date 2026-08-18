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
    /// A concept and a type, or two of either, are declared under one name.
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

    // What a command reads to decide.

    /// <summary>
    /// A <c>reads</c> line is not <c>reads &lt;ReadModel&gt;</c> or <c>reads &lt;ReadModel&gt; by &lt;property&gt;</c>.
    /// </summary>
    public const string InvalidReadsDeclaration = "PLAY0175";

    /// <summary>
    /// A command declares that it reads the same read model more than once.
    /// </summary>
    public const string DuplicateReads = "PLAY0176";

    /// <summary>
    /// A command reads a read model no projection in the document produces.
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
}
