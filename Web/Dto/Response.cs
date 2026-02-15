using System.Text.Json.Serialization;

namespace Web.Dto;

public class Response<TData>
{
    public Response(
        TData? data,
        string? message = null)
    {
        Data = data;
        Message = message;
        Errors = null;
    }
    
    public Response(string message, List<string> errors)
    {
        Data = default; 
        Message = message;
        Errors = errors;
    }

    public TData? Data { get; set; }
    public string? Message { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }
}