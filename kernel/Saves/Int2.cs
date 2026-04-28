using System.Text.Json.Serialization;

namespace MegaCrit.Sts2.Core.Saves;

public readonly struct Int2(int x, int y)
{
	[JsonPropertyName("x")]
	public int X { get; init; } = x;

	[JsonPropertyName("y")]
	public int Y { get; init; } = y;

	public override string ToString()
	{
		return $"({X}, {Y})";
	}
}
