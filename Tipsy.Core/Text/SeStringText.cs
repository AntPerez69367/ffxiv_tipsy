using System.Text;

namespace Tipsy.Core.Text;

/// <summary>Reads the visible text out of raw SeString bytes, skipping every payload.</summary>
public static class SeStringText
{
    private const byte PayloadStart = 0x02;
    private const byte IntegerMarker = 0xF0;

    public static string Plain(ReadOnlySpan<byte> bytes)
    {
        var text = new List<byte>(bytes.Length);
        var i = 0;
        while (i < bytes.Length)
        {
            if (bytes[i] != PayloadStart)
            {
                text.Add(bytes[i++]);
                continue;
            }

            i += 2;
            var length = ReadInteger(bytes, ref i);
            i += length + 1;
        }

        return Encoding.UTF8.GetString(text.ToArray());
    }

    private static int ReadInteger(ReadOnlySpan<byte> bytes, ref int i)
    {
        var marker = bytes[i++];
        if (marker < IntegerMarker)
            return marker - 1;

        var flags = (marker + 1) & 0b1111;
        var value = 0;
        for (var shift = 3; shift >= 0; shift--)
        {
            if ((flags & (1 << shift)) != 0)
                value |= bytes[i++] << (8 * shift);
        }

        return value;
    }
}
