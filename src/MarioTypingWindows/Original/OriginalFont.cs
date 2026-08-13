namespace MarioTypingWindows.Original;

internal sealed class OriginalFont
{
    private readonly OriginalSpriteBank bank;
    private readonly string prefix;

    public OriginalFont(OriginalSpriteBank bank, string prefix, int advance, int lineHeight)
    {
        this.bank = bank;
        this.prefix = prefix;
        Advance = advance;
        LineHeight = lineHeight;
    }

    public int Advance { get; }
    public int LineHeight { get; }

    public void Draw(
        NativeRaster raster,
        VgaPalette palette,
        string text,
        int x,
        int y,
        int? maximumCharacters = null,
        byte? foregroundIndex = null,
        byte? secondaryIndex = null)
    {
        var cursorX = x;
        var cursorY = y;
        var drawnOnLine = 0;
        foreach (var character in text)
        {
            if (character == '\n')
            {
                cursorX = x;
                cursorY += LineHeight;
                drawnOnLine = 0;
                continue;
            }

            if (maximumCharacters.HasValue && drawnOnLine >= maximumCharacters.Value)
            {
                continue;
            }

            if (bank.TryGet(prefix + (int)character, out var glyph))
            {
                if (foregroundIndex.HasValue)
                {
                    raster.DrawSpriteRemapped(
                        glyph, palette, cursorX, cursorY, foregroundIndex.Value, secondaryIndex);
                }
                else
                {
                    raster.DrawSprite(glyph, palette, cursorX, cursorY);
                }
            }
            cursorX += Advance;
            drawnOnLine++;
        }
    }
}
