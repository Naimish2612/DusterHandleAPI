using DUSTER.EComm.Data.CommonClass;
using DUSTER.EComm.Data.Helpers.Logger;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DUSTER.EComm.Data.Helpers.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly IErrorLogger _log;
        public GlobalExceptionFilter(IErrorLogger log)
        {
            _log = log;
        }

        public void OnException(ExceptionContext context)
        {
            //var error = new
            //{
            //    success = false,
            //    message = context.Exception.Message
            //};

            //context.Result = new JsonResult(error) { StatusCode = 500 };
            _log.Exception(context.Exception);
            context.Result = ResponseEntity<string>.Error(null);

        }
    }
}
