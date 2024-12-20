using System.Net;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;
using Mapster;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Default;

internal class DefaultRemoteCallResultHandler : IRemoteCallResultHandler
{
    /// <inheritdoc />
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        var type = typeof(TResult);

        if (type.IsClass && type != typeof(string))
            return JsonConvert.DeserializeObject<TResult>(responseBody);

        return responseBody.Adapt<TResult>();
    }
}