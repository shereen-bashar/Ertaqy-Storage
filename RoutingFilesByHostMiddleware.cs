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
    private readonly string _urlpath;
    private readonly string _physicalFolder;

    public RoutingFilesByHostMiddleware(RequestDelegate next, string urlpath, string physicalFolder)
    {
        _next = next;
        _urlpath = urlpath;
        _physicalFolder = physicalFolder;
    }

    public async Task Invoke(HttpContext context)
    {
        // Extract the requested path
        string requestedPath = context.Request.Path;

        // Extract the hostname from the request URL
        string hostname = context.Request.Host.Host;

        // Check if the requested URL contains the routing folder
        if (requestedPath.StartsWith(_urlpath))
        {
            // Prepend the hostname to the requested path, excluding the routing folder
            string newPath = $"/{_physicalFolder}/{hostname}{requestedPath.Substring(_urlpath.Length)}";

            // Update the request path
            context.Request.Path = newPath;
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
