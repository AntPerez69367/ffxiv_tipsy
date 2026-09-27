using System;
using System.Collections.Generic;
using Tipsy.Core.Layout;
using Tipsy.Core.Tooltips;
using Tipsy.Game;

namespace Tipsy.Windows;

/// <summary>One tooltip addon the overlay can draw: its reader, its layout, and whether the window narrows to fit short text.</summary>
internal sealed record TooltipSource(TooltipReader Reader, Func<TooltipSnapshot, List<TooltipBlock>> Layout, bool FitToContent);
