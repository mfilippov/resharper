// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharper.Nuke.Highlightings;

[assembly: RegisterConfigurableHighlightingsGroup(NukeHighlightingGroupIds.Nuke, "Nuke")]

namespace ReSharper.Nuke.Highlightings
{
    public static class NukeHighlightingGroupIds
    {
        public const string Nuke = "Nuke";
    }
}