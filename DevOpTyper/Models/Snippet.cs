namespace DevOpTyper.Models;

public sealed class Snippet
{
    public string Id { get; set; } = "";
    public string Language { get; set; } = "";
    public int Difficulty { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Code { get; set; } = "";
    public string[] Topics { get; set; } = Array.Empty<string>();
    public string[] Explain { get; set; } = Array.Empty<string>();
}
