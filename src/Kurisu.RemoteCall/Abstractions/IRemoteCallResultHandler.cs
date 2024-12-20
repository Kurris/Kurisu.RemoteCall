using System.Net;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 处理
/// </summary>
public interface IRemoteCallResultHandler
{
    /// <summary>
    /// Handle
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="responseBody"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody);
}