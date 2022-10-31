// © 2022 Jong-il Hong

namespace Haruby.LlmClient;

public class OutputPacket
{
    public string Prompt { get; set; } = string.Empty;
    public int Seed { get; set; }
    public int MaxNewTokens { get; set; }
    public string Message { get; set; } = string.Empty;
}
