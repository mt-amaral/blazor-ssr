using MudBlazor;

namespace Web;

public static class Configuration
{
    public static bool IsDarkMode { get; set; } = false;
    public static MudTheme Theme = new()
    {
        Typography = new()
        { 
            Default = new DefaultTypography()
            {
                FontFamily = new[] { "Ubuntu", "sans-serif" },
                FontWeight = "300"
            }
        },
PaletteLight = new()
{
    Primary = "#1565C0",          // Azul mais profundo e moderno
    Secondary = "#E91E63",        // Rosa vibrante com melhor contraste
    Tertiary = "#EEEEEE",         // claro
    Success = "#388E3C",          // Verde mais escuro para melhor leitura
    Warning = "#F57C00",          // Laranja mais visível para alertas
    Error = "#D32F2F",            // Vermelho mais intenso
    Dark = "#212121",             // Mantido para consistência
    Background = "#FAFAFA",       // Fundo levemente mais quente
    Surface = "#FFFFFF",          // Superfície pura com sombras mais definidas
    AppbarBackground = "#1565C0", // Coerente com Primary
    DrawerBackground = "#F5F5F5", // Contraste sutil com o fundo
    DrawerText = "#424242",       // Cinza escuro para melhor legibilidade
    DrawerIcon = "#616161",       // Contraste moderado
    TextPrimary = "#212121",      // Alto contraste
    TextSecondary = "#616161",    // Contraste AA adequado
    /*ActionDefault = "#E0E0E0",    // Mantido para consistência
    ActionDisabled = "#9E9E9E",   // Mais visível que o anterior
    ActionDisabledBackground = "#EEEEEE", // Fundo mais claro*/
},

PaletteDark = new()
{
    Primary = "#42A5F5",          // Azul mais claro para destaque
    Secondary = "#F06292",        // Rosa pastel para melhor legibilidade
    Tertiary = "#BA68C8",         // Roxo suave
    Success = "#66BB6A",          // Verde mais brilhante
    Warning = "#FFA726",          // Laranja mais vibrante
    Error = "#EF5350",            // Vermelho mais claro
    Dark = "#121212",             // Base mantida
    Background = "#121212",       // Fundo escuro profundo
    Surface = "#1E1E1E",          // Contraste suficiente com fundo
    AppbarBackground = "#1E1E1E", // Integração com surface
    DrawerBackground = "#212121", // Leve diferenciação do surface
    DrawerText = "#E0E0E0",       // Texto mais suave
    DrawerIcon = "#B0BEC5",       // Ícones com contraste seguro
    TextPrimary = "#FFFFFF",      // Máximo contraste
    TextSecondary = "#B0BEC5",    // Contraste AA para texto secundário
    ActionDefault = "#757575",    // Ações discretas
    ActionDisabled = "#424242",   // Desabilitado mais visível
    ActionDisabledBackground = "#303030", // Fundo mais escuro
}
    };
}