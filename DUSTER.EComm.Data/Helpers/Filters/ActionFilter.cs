using Microsoft.AspNetCore.Mvc.Filters;

namespace DUSTER.EComm.Data.Helpers.Filters
{
    public class ActionFilter : IActionFilter
    {
        public ActionFilter() { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Console.WriteLine($" -> Request: {context.HttpContext.Request.Path} at {DateTime.Now}");
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            Console.WriteLine($" <- Response: {context.HttpContext.Response.StatusCode} at {DateTime.Now}");
        }
    }
}
