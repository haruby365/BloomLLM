// © 2022 Jong-il Hong

namespace Haruby.LlmClient;

public class InputPacket
{
    public string Prompt { get; set; }
    public int Seed { get; set; }
    public int MaxNewTokens { get; set; }

    public InputPacket(string prompt, int seed, int maxNewTokens)
    {
        Prompt = prompt;
        Seed = seed;
        MaxNewTokens = maxNewTokens;
    }
}
