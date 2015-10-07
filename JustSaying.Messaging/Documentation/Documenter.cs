using System;
using System.IO;
using JustSaying.Messaging.Documentation.Templates;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Documentation
{
    public class Documenter : IAmJustDocumenting
    {
        public void CreateIndexPage(string path, IInterrogationResponse interrogationResponse)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (interrogationResponse == null) throw new ArgumentNullException("interrogationResponse");

            var indexPage = new IndexPage(interrogationResponse);
            var indexPageContent = indexPage.TransformText();
            File.WriteAllText(path, indexPageContent);
        }
    }
}
