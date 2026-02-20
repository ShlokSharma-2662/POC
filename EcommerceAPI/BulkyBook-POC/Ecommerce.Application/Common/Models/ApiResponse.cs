using System.ComponentModel;

namespace Ecommerce.Application.Common.Models
{
    public class ApiResponse<T>
    {
        [Description("Indicates whether the operation was successful")]
        public bool IsSuccessful { get; set; }
        
        [Description("The status of the operation (Success, Error, Exception, etc.)")]
        public string Status { get; set; } = string.Empty;
        
        [Description("Detailed reason for the status")]
        public string StatusReason { get; set; } = string.Empty;
        
        [Description("The actual response data")]
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string status = "Success", string statusReason = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = true,
                Status = status,
                StatusReason = statusReason,
                Data = data
            };
        }

        public static ApiResponse<T> Failure(string status = "Error", string statusReason = "An error occurred during the operation")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                Status = status,
                StatusReason = statusReason,
                Data = default
            };
        }

        public static ApiResponse<T> Exception(string statusReason = "An exception has been raised that is likely due to a transient failure.")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                Status = "Exception",
                StatusReason = statusReason,
                Data = default
            };
        }

        public static ApiResponse<T> ValidationError(string statusReason = "Validation failed")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                Status = "ValidationError",
                StatusReason = statusReason,
                Data = default
            };
        }

        public static ApiResponse<T> NotFound(string statusReason = "The requested resource was not found")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                Status = "NotFound",
                StatusReason = statusReason,
                Data = default
            };
        }

        public static ApiResponse<T> Unauthorized(string statusReason = "Access denied")
        {
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                Status = "Unauthorized",
                StatusReason = statusReason,
                Data = default
            };
        }
    }

    // Non-generic version for responses without data
    public class ApiResponse
    {
        [Description("Indicates whether the operation was successful")]
        public bool IsSuccessful { get; set; }
        
        [Description("The status of the operation (Success, Error, Exception, etc.)")]
        public string Status { get; set; } = string.Empty;
        
        [Description("Detailed reason for the status")]
        public string StatusReason { get; set; } = string.Empty;
        
        [Description("The actual response data")]
        public object? Data { get; set; }

        public static ApiResponse Success(string status = "Success", string statusReason = "Operation completed successfully", object? data = null)
        {
            return new ApiResponse
            {
                IsSuccessful = true,
                Status = status,
                StatusReason = statusReason,
                Data = data
            };
        }

        public static ApiResponse Failure(string status = "Error", string statusReason = "An error occurred during the operation")
        {
            return new ApiResponse
            {
                IsSuccessful = false,
                Status = status,
                StatusReason = statusReason,
                Data = null
            };
        }

        public static ApiResponse Exception(string statusReason = "An exception has been raised that is likely due to a transient failure.")
        {
            return new ApiResponse
            {
                IsSuccessful = false,
                Status = "Exception",
                StatusReason = statusReason,
                Data = null
            };
        }

        public static ApiResponse ValidationError(string statusReason = "Validation failed")
        {
            return new ApiResponse
            {
                IsSuccessful = false,
                Status = "ValidationError",
                StatusReason = statusReason,
                Data = null
            };
        }

        public static ApiResponse NotFound(string statusReason = "The requested resource was not found")
        {
            return new ApiResponse
            {
                IsSuccessful = false,
                Status = "NotFound",
                StatusReason = statusReason,
                Data = null
            };
        }

        public static ApiResponse Unauthorized(string statusReason = "Access denied")
        {
            return new ApiResponse
            {
                IsSuccessful = false,
                Status = "Unauthorized",
                StatusReason = statusReason,
                Data = null
            };
        }
    }
}
