using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 通用类
/// </summary>
internal static class Common
{
    /// <summary>
    /// 判断请求是否使用授权
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    internal static async Task<(bool, string, string)> UseAuthAsync(this IProxyInvocation invocation)
    {
        var authAttribute = invocation.GetCustomAttribute<AuthAttribute>();
        if (authAttribute == null)
        {
            return (false, string.Empty, string.Empty);
        }

        var headerName = authAttribute.HeaderName;
        string token;
        if (authAttribute.TokenHandler != null && authAttribute.TokenHandler.IsAssignableTo(typeof(IAsyncAuthTokenHandler)))
        {
            token = await ((IAsyncAuthTokenHandler)Activator.CreateInstance(authAttribute.TokenHandler))!.GetTokenAsync(invocation.ServiceProvider);
            return (true, headerName, token);
        }

        var httpContext = invocation.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
        if (httpContext == null)
        {
            throw new NullReferenceException("Use request token error,  httpContext is null");
        }

        token = httpContext.Request.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new NullReferenceException($"Use request token error,  httpContext header[{headerName}] is null");
        }

        return (true, headerName, token);
    }

    /// <summary>
    /// 判断请求输出日志
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    internal static bool UseLog(this IProxyInvocation invocation)
    {
        return invocation.GetCustomAttribute<RequestLogAttribute>() != null;
    }
}