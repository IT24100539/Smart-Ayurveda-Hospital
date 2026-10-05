using System.Net;
using Hospital.Application.Abstractions;

namespace Hospital.Api.Security;

public sealed class HttpClientAddress : IClientAddress
{
    private readonly IHttpContextAccessor _http;

    public HttpClientAddress(IHttpContextAccessor http) => _http = http;

    public string? IpAddress
    {
        get
        {
            var address = _http.HttpContext?.Connection.RemoteIpAddress;
            if (address is null)
            {
                return null;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            var text = address.ToString();
            return text.Length <= 64 ? text : text[..64];
        }
    }
}
