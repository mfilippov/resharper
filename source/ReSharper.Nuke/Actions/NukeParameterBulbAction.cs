// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.TextControl;

namespace ReSharper.Nuke.Actions
{
    public class NukeParameterBulbAction : BulbActionBase

    {
        private readonly string _parameterName;

        public NukeParameterBulbAction(string parameterName)
        {
            _parameterName = parameterName;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            throw new NotImplementedException();
        }

        public override string Text => "Fu";
    }
}