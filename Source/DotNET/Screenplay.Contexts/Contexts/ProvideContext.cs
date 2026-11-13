// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything the code behind a <c>provide</c> is given when it runs - the arguments of the
/// command or query being served, the circumstances it runs under and the values resolved before it.
/// </summary>
/// <param name="Arguments">The arguments of the command or query the value is resolved for.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the request is executing for.</param>
/// <param name="Identity">The <see cref="Contexts.Identity">caller</see> making the request.</param>
/// <param name="Occurred">When the request was received.</param>
/// <remarks>
/// This is the type an inline <c>provide</c> code block and any imported C# file compile against - it is in
/// scope as <c>context</c>. A provided value is resolved lazily, once per request, so <see cref="Values"/>
/// holds only the values this one reads and that were resolved before it; a value that depends on itself
/// is a model error reported by the compiler. An absent key is unknown to the model, and code sees
/// <c>null</c> for an optional value.
/// </remarks>
public record ProvideContext(
    dynamic Arguments,
    TenantId Tenant,
    Identity Identity,
    DateTimeOffset Occurred)
{
    /// <summary>
    /// Gets the values already resolved for this request, each readable by name.
    /// </summary>
    public dynamic Values { get; init; } = new ExpandoObject();

    /// <summary>
    /// Gets the arguments as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">The type supplied by the realization for the arguments.</typeparam>
    /// <returns>The arguments as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextPayloadTypeMismatch">The arguments are not a <typeparamref name="T"/>.</exception>
    public T ArgumentsAs<T>() => ContextPayload.As<T>((object?)Arguments, nameof(Arguments));

    /// <summary>
    /// Gets the resolved values as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">The type supplied by the realization for the values.</typeparam>
    /// <returns>The values as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextPayloadTypeMismatch">The values are not a <typeparamref name="T"/>.</exception>
    public T ValuesAs<T>() => ContextPayload.As<T>((object?)Values, nameof(Values));
}
