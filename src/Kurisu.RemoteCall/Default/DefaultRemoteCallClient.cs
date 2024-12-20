using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认远程调用
/// </summary>
internal class DefaultRemoteCallClient : Aop
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DefaultRemoteCallClient> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="configuration"></param>
    /// <param name="logger"></param>
    public DefaultRemoteCallClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DefaultRemoteCallClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        await RequestAsync<Task>(invocation);
    }

    /// <inheritdoc/>
    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        return await RequestAsync<TResult>(invocation);
    }

    /// <summary>
    /// 请求处理
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FileLoadException"></exception>
    private async Task<TResult> RequestAsync<TResult>(IProxyInvocation invocation)
    {
        var defineHttpMethodAttribute = invocation.Method.GetCustomAttribute<HttpMethodAttribute>() ?? throw new NullReferenceException("请定义请求方式");
        var defineRemoteClientAttribute = invocation.InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;

        //参数和值
        var methodParameters = invocation.Method.GetParameters();
        var methodParameterValues = methodParameters.Select((t, i) => new ParameterValue(t, invocation.Parameters[i])).ToList();

        //验证
        foreach (var item in methodParameterValues)
        {
            ValidateObject(item.Value);
        }

        (HttpMethodEnumType? httpMethodType, string url) = UrlParseUtils.GetRequestUrl(_configuration, defineRemoteClientAttribute, defineHttpMethodAttribute, methodParameterValues);

        //请求方法
        var callMethod = (httpMethodType == HttpMethodEnumType.Get
                             ? typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string) })
                             : typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string), typeof(HttpContent) }))
                         ?? throw new NotSupportedException($"不支持{httpMethodType}的请求方式");

        //请求方法的参数
        var requestParameters = new List<object>(2) { url };
        HttpContent content = null;
        if (httpMethodType != HttpMethodEnumType.Get)
        {
            var requestContent = invocation.GetCustomAttribute<RequestContentHandlerAttribute>();
            content = requestContent == null
                ? ContentUtils.Create(invocation, methodParameterValues)
                : ((IContentHandler)Activator.CreateInstance(requestContent.Handler))!.Create(methodParameterValues);

            requestParameters.Add(content);
        }

        var httpClient = _httpClientFactory.CreateClient(defineRemoteClientAttribute.Name);

        //鉴权
        (bool useAuth, string headerName, string token) = await invocation.UseAuthAsync();
        if (useAuth)
        {
            httpClient.DefaultRequestHeaders.Add(headerName, token);
        }

        var task = (Task<HttpResponseMessage>)callMethod.Invoke(httpClient, requestParameters.ToArray())!;
        var response = await task.ConfigureAwait(false);

        var responseJson = await response.Content.ReadAsStringAsync();
        //日志
        if (invocation.UseLog())
        {
            var body = requestParameters.Count > 1 ? await content!.ReadAsStringAsync() : string.Empty;
            _logger.LogInformation("{method} {url}\r\nBody:{body}\r\nResponse:{response} .", httpMethodType, url, body, responseJson);
        }

        IRemoteCallResultHandler resultHandler;

        var resultHandlerDefined = invocation.Method.GetCustomAttribute<RequestResultHandlerAttribute>() ?? invocation.InterfaceType.GetCustomAttribute<RequestResultHandlerAttribute>();
        if (resultHandlerDefined != null)
        {
            resultHandler = Activator.CreateInstance(resultHandlerDefined.HandlerType) as IRemoteCallResultHandler;
        }
        else
        {
            resultHandler = new DefaultRemoteCallResultHandler();
        }

        return resultHandler!.Handle<TResult>(response.StatusCode, responseJson);
    }

    /// <summary>
    /// 验证实体
    /// </summary>
    /// <param name="obj"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateObject(object obj)
    {
        var type = obj.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableTo(typeof(List<>)))
        {
            var list = (IEnumerable<object>)obj;
            foreach (var item in list)
            {
                ValidateObject(item);
            }
        }
        else
        {
            Validator.ValidateObject(obj, new ValidationContext(obj), true);
        }
    }
}