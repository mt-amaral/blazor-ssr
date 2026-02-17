namespace Web.Dto.Account;

public class UserFilterInDto : PaginationFilter
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}