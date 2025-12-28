using Microsoft.AspNetCore.Mvc;
using BLL.DTOs;
using System.Net;

namespace OpollaBE.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<APIResponse>? ValidateModel()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(APIResponse.ValidationError(errors));
        }
        return null;
    }

    protected ActionResult<APIResponse> HandleException(Exception ex)
    {
        string safeMsg = string.IsNullOrWhiteSpace(ex.Message) 
            ? "Đã xảy ra lỗi" 
            : ex.Message;

        return ex switch
        {
            UnauthorizedAccessException =>
                Unauthorized(APIResponse.Error(
                    string.IsNullOrWhiteSpace(ex.Message) ? "Truy cập bị từ chối" : ex.Message, 
                    HttpStatusCode.Unauthorized)),

            KeyNotFoundException =>
                NotFound(APIResponse.Error(safeMsg, HttpStatusCode.NotFound)),

            ArgumentNullException =>
                BadRequest(APIResponse.Error("Thiếu dữ liệu bắt buộc.", HttpStatusCode.BadRequest)),

            ArgumentException =>
                BadRequest(APIResponse.Error(safeMsg, HttpStatusCode.BadRequest)),

            InvalidOperationException =>
                BadRequest(APIResponse.Error(safeMsg, HttpStatusCode.BadRequest)),

            HttpRequestException =>
                StatusCode(503, APIResponse.Error("Dịch vụ phụ trợ không khả dụng.", HttpStatusCode.ServiceUnavailable)),

            _ => StatusCode(500, APIResponse.Error("Lỗi máy chủ nội bộ", HttpStatusCode.InternalServerError))
        };
    }

    protected ActionResult<APIResponse> SuccessResponse(object? data = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = APIResponse.Success(data ?? new object(), statusCode);
        return statusCode switch
        {
            HttpStatusCode.Created   => Created("", response),
            HttpStatusCode.NoContent => NoContent(),
            _                        => Ok(response)
        };
    }

    protected ActionResult<APIResponse> ErrorResponse(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        var response = APIResponse.Error(message, statusCode);
        return statusCode switch
        {
            HttpStatusCode.NotFound      => NotFound(response),
            HttpStatusCode.Unauthorized  => Unauthorized(response),
            HttpStatusCode.Forbidden     => StatusCode(403, response),
            HttpStatusCode.BadRequest    => BadRequest(response),
            HttpStatusCode.Conflict      => StatusCode(409, response),
            _                            => StatusCode((int)statusCode, response)
        };
    }
}