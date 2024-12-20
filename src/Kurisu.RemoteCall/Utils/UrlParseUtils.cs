using System.Reflection;
using Kurisu.RemoteCall.Attributes;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.RemoteCall.Utils;

internal static class UrlParseUtils
{
    /// <summary>
    /// 获取生成的请求url
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="setting"></param>
    /// <param name="methodTemplate"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static (HttpMethodEnumType? httpMethodType, string url) GetRequestUrl(IConfiguration configuration,
        EnableRemoteClientAttribute setting,
        HttpMethodAttribute methodTemplate,
        List<ParameterValue> parameters)
    {
        var url = methodTemplate?.Template ?? string.Empty;
        var baseUrl = setting.BaseUrl ?? string.Empty;

        baseUrl = FixFromConfiguration(configuration, baseUrl);
        url = FixFromConfiguration(configuration, url);
        url = FixQuery(url, parameters);

        var httpMethod = methodTemplate?.HttpMethod;
        return string.IsNullOrEmpty(baseUrl) || url.StartsWith("http")
            ? (httpMethod, url)
            : (httpMethod, baseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url));
    }


    /// <summary>
    /// 从配置中转换
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="template"></param>
    /// <returns></returns>
    private static string FixFromConfiguration(IConfiguration configuration, string template)
    {
        if (!template.StartsWith("${") || !template.EndsWith("}"))
            return template;

        var path = template.Replace("${", string.Empty).TrimEnd('}');
        return configuration.GetSection(path).Value;
    }

    /// <summary>
    /// 处理url地址
    /// </summary>
    /// <param name="url"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private static string FixQuery(string url, List<ParameterValue> parameters)
    {
        //表单提交
        if (parameters.Any(x => x.Parameter.IsDefined(typeof(RequestFormAttribute))))
            return url;

        //处理请求url的地址
        List<string> items = new(parameters.Count);

        //存在定义RequestQueryAttribute
        var p = parameters.FirstOrDefault(x => x.Parameter.IsDefined(typeof(RequestQueryAttribute)) && x.Parameter.ParameterType != typeof(string));
        if (p != null)
        {
            //对象转字典
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(p.Value))!;
            items.AddRange(d.Select(pair => $"{pair.Key}={pair.Value}"));
        }
        else
        {
            foreach (var item in parameters.Where(x => !x.Parameter.IsDefined(typeof(IgnoreQueryAttribute))))
            {
                if (url.Contains($"{{{item.Parameter.Name}}}"))
                {
                    url = url.Replace($"{{{item.Parameter.Name}}}", item.Value.ToString());
                }
                else
                {
                    if (!item.Parameter.ParameterType.IsClass || item.Parameter.ParameterType == typeof(string))
                    {
                        items.Add($"{item.Parameter.Name}={item.Value}");
                    }
                }
            }
        }

        return items.Any()
            ? url + "?" + string.Join("&", items)
            : url;
    }
}