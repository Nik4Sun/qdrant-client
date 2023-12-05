﻿// ReSharper disable MemberCanBeInternal
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Aer.QdrantClient.Http.Models.Requests.Public;

/// <summary>
/// Represents the set points payload operation.
/// </summary>
internal sealed class SetPointsPayloadOperation : BatchUpdatePointsOperationBase
{
    /// <summary>
    /// Set points payload request.
    /// </summary>
    /// <remarks>
    /// We don't use generic request here due to System.Text.Json limitations.
    /// But this class is internal and never created by end user so we are relatively safe.
    /// </remarks>
    public required object SetPayload { set; get; }
}
