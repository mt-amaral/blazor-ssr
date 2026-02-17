namespace Web.Dto;

public class ResponsePage<T>
{
    // Dados paginados
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    // Metadados calculados (opcionais, mas úteis)
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    // Propriedades de feedback (iguais ao Response original)
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    // Construtores
    public ResponsePage() { }

    public ResponsePage(IEnumerable<T> items, int totalCount, int currentPage, int pageSize, string? message = null, List<string>? errors = null)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
        Message = message;
        Errors = errors;
    }

    // Construtor para cenários de erro (sem itens)
    public ResponsePage(string message, List<string> errors)
    {
        Message = message;
        Errors = errors;
    }
}