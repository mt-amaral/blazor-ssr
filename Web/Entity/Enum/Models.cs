using System.ComponentModel;

namespace Web.Entity.Enum;

public enum Models
{
    [Description("qwen2.5:3b")]
    Qwen2_5=1,
    [Description("qwen3:14b")]
    Qwen3= 2,
    [Description("llama3.1:8b")]
    Qwen3_1= 3
} 