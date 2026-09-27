using System.Buffers;
using System.Text;

namespace Tipsy.Core.Text;

/// <summary>
/// Reads the visible text out of raw SeString bytes, skipping every payload. Malformed or truncated payloads end the
/// text there instead of throwing, because these bytes come from the game and from other plugins.
/// </summary>
public static class SeStringText
{
    private const byte PayloadStart = 0x02;
    private const byte IntegerMarker = 0xF0;
    private const byte NewLineType = 0x10;
    private const byte Space = 0x20;

    public static string Plain(ReadOnlySpan<byte> bytes)
    {
        var text = ArrayPool<byte>.Shared.Rent(bytes.Length);
        var count = 0;
        var i = 0;
        while (i < bytes.Length)
        {
            if (bytes[i] != PayloadStart)
            {
                text[count++] = bytes[i++];
                continue;
            }

            i += 2;
            if (!TryReadLength(bytes, ref i, out var length) || length > bytes.Length - i)
                break;
            i += (int)length + 1;
        }

        var plain = Encoding.UTF8.GetString(text, 0, count);
        ArrayPool<byte>.Shared.Return(text);
        return plain;
    }

    /// <summary>
    /// Returns the SeString with its line-break payloads replaced by a space, keeping every other payload, so text the
    /// game broke for its own tooltip width can wrap to Tipsy's. Malformed payloads are copied as they are.
    /// </summary>
    public static SeText SingleLine(SeText text)
    {
        var bytes = text.Bytes;
        var result = new List<byte>(bytes.Length);
        var i = 0;
        while (i < bytes.Length)
        {
            if (bytes[i] != PayloadStart)
            {
                result.Add(bytes[i++]);
                continue;
            }

            var start = i;
            i += 2;
            if (!TryReadLength(bytes, ref i, out var length) || length > bytes.Length - i)
            {
                result.AddRange(bytes[start..]);
                break;
            }

            i += (int)length + 1;
            if (bytes[start + 1] != NewLineType)
                result.AddRange(bytes[start..Math.Min(i, bytes.Length)]);
            else if (result.Count > 0 && result[^1] != Space)
                result.Add(Space);
        }

        return new SeText(result.ToArray());
    }

    private static bool TryReadLength(ReadOnlySpan<byte> bytes, ref int i, out uint value)
    {
        value = 0;
        if (i >= bytes.Length)
            return false;
        var marker = bytes[i++];
        if (marker < IntegerMarker)
        {
            value = marker == 0 ? 0u : marker - 1u;
            return true;
        }

        var flags = (marker + 1) & 0b1111;
        for (var shift = 3; shift >= 0; shift--)
        {
            if ((flags & (1 << shift)) == 0)
                continue;
            if (i >= bytes.Length)
                return false;
            value |= (uint)bytes[i++] << (8 * shift);
        }

        return true;
    }
}
