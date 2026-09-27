using System;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace Tipsy.Windows;

/// <summary>The three game-font handles the tooltip draws with, built once at load.</summary>
internal sealed class TooltipFonts : IDisposable
{
    private const float BodySize = 16;
    private const float TitleScale = 1.25f;
    private const float SmallScale = 0.88f;

    public TooltipFonts(IFontAtlas atlas)
    {
        Title = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, BodySize * TitleScale) { Bold = true });
        Body = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, BodySize));
        Small = atlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, BodySize * SmallScale));
    }

    public IFontHandle Title { get; }

    public IFontHandle Body { get; }

    public IFontHandle Small { get; }

    public void Dispose()
    {
        Title.Dispose();
        Body.Dispose();
        Small.Dispose();
    }
}
