using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Documentation.Templates
{
    public partial class IndexPage
    {
        private IInterrogationResponse _interrogationResponse;
        
        public IndexPage(IInterrogationResponse interrogationResponse)
        {
            _interrogationResponse = interrogationResponse;
        }
    }
}
