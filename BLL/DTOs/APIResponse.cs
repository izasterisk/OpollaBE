using System.Net;

namespace BLL.DTOs;

public class APIResponse
{
    public bool Status { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public dynamic? Data { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public static APIResponse Success(object? data = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new APIResponse
        {
            Status = true,
            StatusCode = statusCode,
            Data = data ?? "Success",
            Errors = new List<string>()
        };
    }

    public static APIResponse Error(string error, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new APIResponse
        {
            Status = false,
            StatusCode = statusCode,
            Data = null,
            Errors = new List<string> { error }
        };
    }

    public static APIResponse Error(List<string> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new APIResponse
        {
            Status = false,
            StatusCode = statusCode,
            Data = null,
            Errors = errors
        };
    }

    public static APIResponse ValidationError(List<string> errors)
    {
        return Error(errors, HttpStatusCode.BadRequest);
    }
}
