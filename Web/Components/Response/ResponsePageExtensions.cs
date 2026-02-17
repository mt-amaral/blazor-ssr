using MudBlazor;
using Web.Dto;

namespace Web.Components.Response;

public static class ResponsePageExtensions
{
    public static async Task<ResponsePage<T>?> HandlePageAsync<T>(
        this Task<(ResponsePage<T> ResponsePage, short StatusCode)> task,
        ISnackbar snackbar)
    {
        try
        {
            var (responsePage, statusCode) = await task;

            bool isSuccess = statusCode >= 200 && statusCode <= 299;

            // Exibe erros (lista)
            if (responsePage.Errors != null && responsePage.Errors.Any())
            {
                foreach (var error in responsePage.Errors)
                {
                    snackbar.Add(error, Severity.Error);
                }
            }
            // Exibe mensagem única
            else if (!string.IsNullOrEmpty(responsePage.Message))
            {
                var severity = isSuccess ? Severity.Success : Severity.Error;
                snackbar.Add(responsePage.Message, severity);
            }

            // Se não foi sucesso, retorna null para indicar falha
            return isSuccess ? responsePage : null;
        }
        catch (Exception ex)
        {
            snackbar.Add($"Erro crítico: {ex.Message}", Severity.Error);
            return null;
        }
    }
}