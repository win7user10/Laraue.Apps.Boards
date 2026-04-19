using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Reflection;
using Laraue.Apps.StructuredMessages.WebApiHost;
using Laraue.Telegram.NET.Abstractions.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using FromQueryAttribute = Microsoft.AspNetCore.Mvc.FromQueryAttribute;

namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public class Proxy<TController>(HttpClient client, WebApiTestHost host) where TController : ControllerBase
{
    public async Task<T?> Execute<T>(Expression<Func<TController, Task<T>>> makeCall)
    {
        // Get methods from reflection
        // Check that called method exists and public
        // Check attribute (post / get / put)
        // Check route path 
        // Build path base on [FromPath]
        // Serialize based on [FromBody]
        // Serialize based on [FromQuery]
        var controller = typeof(TController);
        var routeAttribute = controller.GetCustomAttribute<RouteAttribute>()
            ?? throw new InvalidOperationException($"Route attribute on {controller} excepted");
        var controllerPath = routeAttribute.Template;

        var methodExpr = (MethodCallExpression)makeCall.Body;
        var method = methodExpr.Method;
        var httpAttribute = method.GetCustomAttribute(typeof(HttpMethodAttribute), true);
        if (httpAttribute == null)
            throw new InvalidOperationException($"Method {method} should be marked as HTTP attribute, e.g. [HttpGet] to be called.");

        var methodArguments = method.GetParameters();
        var methodArgumentTypes = new Dictionary<ParameterInfo, BindType?>();
        foreach (var argument in methodArguments)
        {
            BindType? bindType = argument.GetCustomAttribute<FromQueryAttribute>() != null
                ? BindType.FromQuery
                : argument.GetCustomAttribute<FromPathAttribute>() != null
                    ? BindType.FromQuery
                    : argument.GetCustomAttribute<FromBodyAttribute>() != null
                        ? BindType.FromBody
                        : null;
            
            methodArgumentTypes.Add(argument, bindType);
        }
        
        var query = new Dictionary<string, object>();
        var body = new Dictionary<string, object>();
        
        foreach (var arg in methodExpr.Arguments)
        {
            if (arg is MemberInitExpression initExpr)
            {
                var bindType = methodArgumentTypes
                    .FirstOrDefault(x => x.Key.ParameterType == initExpr.Type)
                    .Value;

                if (bindType is BindType.FromQuery or BindType.FromBody)
                {
                    foreach (var binding in initExpr.Bindings)
                    {
                        var assignment = (MemberAssignment)binding;
                        var value = Expression.Lambda(assignment.Expression).Compile().DynamicInvoke()!;
                        if (bindType  is BindType.FromQuery)
                            query[binding.Member.Name] = value;
                        else
                            body[binding.Member.Name] = value;
                    }
                }
            }
        }

        var fullPath = controllerPath;
        if (query.Any())
            fullPath += "?" + string.Join("&", query.Select(x => $"{x.Key}={x.Value}"));

        var responseTask = httpAttribute switch
        {
            HttpGetAttribute => client.GetAsync(fullPath),
            HttpPostAttribute => client.PostAsJsonAsync(fullPath, body),
            _ => throw new InvalidOperationException($"Requests with {httpAttribute} are not supported")
        };
        
        var response = await responseTask;
        await HandleNonSuccessCode(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public Proxy<TController> WithAuthorization(Guid userId)
    {
        var authService = host.Services.GetRequiredService<IAuthService>();
        var bearer = authService.CreateToken(userId);
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearer}");
        return this;
    }

    private async Task HandleNonSuccessCode(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var requestContent = await response.RequestMessage!.Content!.ReadAsStringAsync();
            throw new Exception($"[{response.RequestMessage?.Method}] {response.RequestMessage?.RequestUri} ({response.StatusCode:D}) \nRequest Content: {requestContent}\nResponse Content:{responseContent}");
        }
    }

    internal enum BindType
    {
        FromQuery,
        FromPath,
        FromBody,
    }
}