using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DUSTER.EComm.Data.CommonClass
{
    public class ResponseEntity<T> : IActionResult
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ResponseEntity(int statusCode, string message, T? data = default, string? requestId = null)
        {
            StatusCode = statusCode;
            Message = message;
            Data = data;
            RequestId = requestId ?? Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var objectResult = new ObjectResult(new
            {
                statusCode = StatusCode,
                message = Message,
                data = Data,
                requestId = context.HttpContext.TraceIdentifier,
                timestamp = Timestamp
            })
            {
                StatusCode = StatusCode
            };

            await objectResult.ExecuteResultAsync(context);
        }

        /// <summary>
        /// Creates a successful response entity with the specified data and an HTTP 200 (OK) status code.
        /// </summary>
        /// <param name="data">The data to include in the response. Can be null if no data is required.</param>
        /// <returns>A ResponseEntity<T> representing a successful response containing the specified data.</returns>
        public static ResponseEntity<T> Success(T? data) => new((int)HttpStatusCode.OK, "Success", data);

        /// <summary>
        /// Creates a successful response entity with the specified data and an optional message.
        /// </summary>
        /// <param name="data">The data to include in the response. Can be null if no data is required.</param>
        /// <param name="message">The message to include in the response. The default is "Success".</param>
        /// <returns>A response entity representing a successful operation, containing the specified data and message.</returns>
        public static ResponseEntity<T> Success(T? data, string message = "Success") => new((int)HttpStatusCode.OK, message, data);

        /// <summary>
        /// Creates a response entity representing an internal server error with the specified data payload.
        /// </summary>
        /// <param name="data">The data to include in the response body. This value can be null.</param>
        /// <returns>A <see cref="ResponseEntity{T}"/> with a 500 Internal Server Error status code, a standard error message,
        /// and the specified data.</returns>
        public static ResponseEntity<T> Error(T? data) => new((int)HttpStatusCode.InternalServerError, "Internal Server Error.", data);

        /// <summary>
        /// Creates a response entity representing an error with the specified data and message.
        /// </summary>
        /// <param name="data">The data to include in the error response. This value can be null if no additional data is available.</param>
        /// <param name="message">The error message to include in the response. The default is "An error occurred".</param>
        /// <returns>A response entity with an HTTP 500 Internal Server Error status, containing the specified data and message.</returns>
        public static ResponseEntity<T> Error(T? data, string message = "An error occurred") => new((int)HttpStatusCode.InternalServerError, message, data);

        /// <summary>
        /// Creates a new error response with the specified data, message, and HTTP status code.
        /// </summary>
        /// <param name="data">The payload to include in the error response. This value can be null if no additional data is provided.</param>
        /// <param name="message">The error message to include in the response. If not specified, a default message is used.</param>
        /// <param name="statusCode">The HTTP status code to associate with the error response. The default is InternalServerError (500).</param>
        /// <returns>A ResponseEntity<T> representing an error response with the specified data, message, and status code.</returns>
        public static ResponseEntity<T> Error(T? data, string message = "An error occurred", HttpStatusCode statusCode = HttpStatusCode.InternalServerError) => new((int)statusCode, message, data);

    }
}
