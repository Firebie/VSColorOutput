using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable 649

namespace BlueOnionSoftware
{
    [ContentType("output")]
    [Export(typeof(IClassifierProvider))]
    public class OutputClassifierProvider : IClassifierProvider
    {
        [Import] internal IClassificationTypeRegistryService ClassificationRegistry;
        [Import] internal SVsServiceProvider ServiceProvider;

        public static OutputClassifier OutputClassifier { get; private set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            try
            {
                if (OutputClassifier == null)
                {
                    OutputClassifier = new OutputClassifier(ClassificationRegistry);
                    BuildEventsProvider.ConstructBuildEvents(ServiceProvider); // todo Get MEF to load this
                }
            }
            catch (Exception ex)
            {
                OutputClassifier.LogError(ex.ToString());
                throw;
            }
            return OutputClassifier;
        }
    }
}