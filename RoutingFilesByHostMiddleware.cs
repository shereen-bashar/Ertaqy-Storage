//public class RoutingMiddleware
//{
//    private readonly RequestDelegate _next;

//    public RoutingMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    public async Task Invoke(HttpContext context)
//    {
//        // Extract the requested path
//        string requestedPath = context.Request.Path;

//        // Extract the hostname from the request URL
//        string hostname = context.Request.Host.Host;

//        // Check if the requested URL contains '/files'
//        if (requestedPath.StartsWith("/files"))
//        {
//            // Prepend the hostname to the requested path, excluding the '/files' segment
//            string newPath = $"/_files/{hostname}{requestedPath.Substring("/files".Length)}";

//            // Update the request path
//            context.Request.Path = newPath;
//        }

//        // Call the next middleware in the pipeline
//        await _next(context);
//    }
//}

public class RoutingFilesByHostMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _singleDomainUrlPath;
    private readonly string _multiDomainsUrlPath;
    private readonly string _physicalFolder;

    public RoutingFilesByHostMiddleware(RequestDelegate next, string singleDomainUrlPath, string multiDomainsUrlPath, string physicalFolder)
    {
        _next = next;
        _singleDomainUrlPath = singleDomainUrlPath;
        _multiDomainsUrlPath = multiDomainsUrlPath;
        _physicalFolder = physicalFolder;
    }

    public async Task Invoke(HttpContext context)
    {
        // Extract the requested path
        string requestedPath = context.Request.Path;
        //// Extract the hostname from the request URL

        //bool rewrotePath = false;

        // Check if the requested URL contains the routing folder
        if (requestedPath.StartsWith("/" + _singleDomainUrlPath, StringComparison.OrdinalIgnoreCase))
        {
            string hostname = context.Request.Host.Host;
            // Prepend the hostname to the requested path, excluding the routing folder
            string newPath = $"/{_physicalFolder}/{hostname}{requestedPath.Substring(_singleDomainUrlPath.Length)}";

            // Update the request path
            context.Request.Path = newPath;
            //rewrotePath = true;
        }


        if (requestedPath.StartsWith("/" + _multiDomainsUrlPath, StringComparison.OrdinalIgnoreCase))
        {
            string tmp = requestedPath.Remove(0, _multiDomainsUrlPath.Length + 1); // +1 for the slash
            string urlPathHost = tmp.Substring(0, tmp.IndexOf("/"));
            string fileUrlPath = tmp.Remove(0, tmp.IndexOf("/") + 1);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = $"https://{urlPathHost}";
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
                return Task.CompletedTask;
            });

            // handle old urls
            if (fileUrlPath.StartsWith("files/style"))
                fileUrlPath = fileUrlPath.Remove(0, 6); // 6 characters files/

            string newPath = $"/{_physicalFolder}/{urlPathHost}/{fileUrlPath}";
            context.Request.Path = newPath;

        }




        if (requestedPath.StartsWith("/content/", StringComparison.OrdinalIgnoreCase)
            || requestedPath.StartsWith("/static/", StringComparison.OrdinalIgnoreCase))
        {

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
                return Task.CompletedTask;
            });

        }


        // read RoutingTenant by url

        // set allow origin to routingTenant

        // rest of urlpath

        // urlpath startwith files/style => style



        //// Handle CORS headers conditionally
        //if (rewrotePath)
        //{
        //    context.Response.OnStarting(() =>
        //    {
        //        context.Response.Headers["Access-Control-Allow-Origin"] = $"https://{hostname}";
        //        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        //        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        //        return Task.CompletedTask;
        //    });
        //}
        //else
        //{
        //    context.Response.OnStarting(() =>
        //    {
        //        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        //        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        //        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        //        return Task.CompletedTask;
        //    });
        //}

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
