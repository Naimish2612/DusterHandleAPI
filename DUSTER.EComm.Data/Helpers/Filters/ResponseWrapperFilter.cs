using Microsoft.AspNetCore.Mvc.Filters;

namespace DUSTER.EComm.Data.Helpers.Filters
{
    public class ResponseWrapperFilter : IResultFilter
    {
        public ResponseWrapperFilter()
        {

        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            // This method is called before the result is executed
            //Console.WriteLine($"Response: {context.HttpContext.Response.StatusCode} at {DateTime.Now}");

            //if (context.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 300)
            //{
            //    context.Result = new JsonResult(new
            //    {
            //        success = true,
            //        data = objectResult.Value
            //    })
            //    {
            //        StatusCode = objectResult.StatusCode
            //    };
            //}

            //if (context.Result is ObjectResult objResult && objResult.StatusCode is >= 200 and < 300)
            //{
            //    context.Result = new ResponseEntity<object>(objResult.StatusCode ?? 200, "Request successful", objResult.Value);
            //}
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }
}
