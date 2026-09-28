using System.Text;

namespace Tipsy.Core.Text;

/// <summary>Raw SeString bytes as the game or another plugin wrote them, compared by content.</summary>
public readonly record struct SeText(byte[] Bytes)
{
    public static SeText Empty { get; } = new([]);

    public int Length => Bytes.Length;

    public static SeText Utf8(string text) => new(Encoding.UTF8.GetBytes(text));

    public static implicit operator ReadOnlySpan<byte>(SeText text) => text.Bytes;

    public bool Equals(SeText other) => Bytes.AsSpan().SequenceEqual(other.Bytes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(Bytes);
        return hash.ToHashCode();
    }
}
