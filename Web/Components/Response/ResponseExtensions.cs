using MudBlazor;
using Web.Dto;

namespace Web.Components.Response;

public static class ResponseExtensions
{
    public static async Task<T?> HandleAsync<T>(
        this Task<(Response<T> Response, short StatusCode)> task,
        ISnackbar snackbar)
    {
        try
        {
            // Desestrutura a Tupla
            var (response, statusCode) = await task;

            bool isSuccess = statusCode is >= 200 and <= 299;

            // 1. Tratamento de Erros de Validação (Lista de Erros)
            if (response.Errors != null && response.Errors.Any())
            {
                foreach (var error in response.Errors)
                {
                    snackbar.Add(error, Severity.Error);
                }
            }
            // 2. Mensagem única (Sucesso ou Erro Genérico)
            else if (!string.IsNullOrEmpty(response.Message))
            {
                var severity = isSuccess ? Severity.Success : Severity.Error;
                snackbar.Add(response.Message, severity);
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            snackbar.Add($"Erro crítico: {ex.Message}", Severity.Error);
            return default;
        }
    }
}