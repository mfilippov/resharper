// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;

namespace ReSharper.Nuke.CodeCompletion
{
    public class NukeTasksBehaviour : CSharpMethodsBehavior<MethodsInfo>
    {
        public NukeTasksBehaviour(MethodsInfo info)
            : base(info)
        {
        }
    }
}