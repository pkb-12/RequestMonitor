using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace RequestMonitor.Filters
{
    public class GlobalExceptionHandler : IExceptionFilter
    {
        ILogger logger;
        public GlobalExceptionHandler(ILoggerFactory loggerFactor)
        {
            logger = loggerFactor.CreateLogger<GlobalExceptionHandler>();
        }
        
        //Log exceptions
        public void OnException(ExceptionContext context)
        {
            HttpResponse response = context.HttpContext.Response;
            response.StatusCode = 500;
            response.ContentType = "application/json";

            logger.LogCritical(context.Exception.ToString());

            context.Result = new ObjectResult(new
            {
                Message = "Internal Server Error"
            });
        }
    }
}
