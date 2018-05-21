// Copyright Matthias Koch, Sebastian Karasek 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Psi.ExtensionsAPI.ExternalAnnotations;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;

#pragma warning disable 618
[assembly: TestDataPathBase("Test_Data/data")]
#pragma warning restore 618

namespace ReSharper.Nuke.Tests
{
    [ShellComponent]
    public class AnnotationsLoader : IExternalAnnotationsFileProvider
    {
        private readonly OneToSetMap<string, FileSystemPath> _annotations;

        public AnnotationsLoader()
        {
            _annotations = new OneToSetMap<string, FileSystemPath>(StringComparer.OrdinalIgnoreCase);
            var testDataPath = TestUtil.GetTestDataPathBase(GetType().Assembly);
            var annotationsPath = testDataPath.Parent / "annotations";
            Assertion.Assert(annotationsPath.ExistsDirectory, $"Cannot find annotations: {annotationsPath}");
            var annotationFiles = annotationsPath.GetChildFiles();
            foreach (var annotationFile in annotationFiles)
            {
                _annotations.Add(annotationFile.NameWithoutExtension, annotationFile);
            }
        }

        public IEnumerable<FileSystemPath> GetAnnotationsFiles(AssemblyNameInfo assemblyName = null, FileSystemPath assemblyLocation = null)
        {
            if (assemblyName == null)
                return _annotations.Values;
            return _annotations[assemblyName.Name];
        }
    }
}