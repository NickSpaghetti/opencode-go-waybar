using System.Net;

namespace OpencodeGoWaybar.Models.Usages;

/// <summary>An unread response from the usage API, exactly as it arrived.</summary>
internal sealed record UsageApiBrokerResponse(HttpStatusCode StatusCode, string Body);
