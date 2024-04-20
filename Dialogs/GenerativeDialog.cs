using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CoreBot.Dialogs
{
    public class GenerativeDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GenerativeDialog(IHttpClientFactory httpClientFactory) : base(nameof(GenerativeDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            var waterfallDialogId = $"{nameof(GenerativeDialog)}.{nameof(WaterfallDialog)}";
            var waterfallSteps = new WaterfallStep[]
            {
                StartConversationAsync,
                GetInputStepAsync,
                ShowSuggestionAsync
            };

            AddDialog(new WaterfallDialog(waterfallDialogId, waterfallSteps));        
            InitialDialogId = waterfallDialogId;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<DialogTurnResult> StartConversationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string message = "Please enter your question here";
            object suggestion = "";
            if (stepContext.Options is Suggestion) {
                var suggestionObj = stepContext.Options as Suggestion;
                message = suggestionObj.Name;
            }
            
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(message) }, cancellationToken);
        }

        private async Task<DialogTurnResult> GetInputStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["query"] = (string)stepContext.Result;

            var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"http://localhost:3000/completion?query={stepContext.Values["query"]}"){};
            var httpClient = _httpClientFactory.CreateClient();

            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
            var responseFromAssistant = await httpResponseMessage.Content.ReadAsStringAsync();

            stepContext.Values["suggestion"] = responseFromAssistant;

            return await stepContext.NextAsync(null);
        }

        private async Task<DialogTurnResult> ShowSuggestionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var suggestion = new Suggestion() { Name = stepContext.Values["suggestion"] as string };
            return await stepContext.ReplaceDialogAsync(InitialDialogId, suggestion);   
        }
    }

    public class Suggestion
    {
        public string Name { get; set; }
    }
}