// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class LaunchingBotDialog : CancelAndHelpDialog
    {
        private const string DestinationStepMsgText = "Voulez vous vraiment lancer un robot? ";
        private const string OriginStepMsgText = "Quel robot voulez vous que j'exécute ?";

        public LaunchingBotDialog()
            : base(nameof(LaunchingBotDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                OriginStepAsync,
                TravelDateStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            if (LaunchingBotDetails.Destination == null)
            {
                var promptMessage = MessageFactory.Text(DestinationStepMsgText, DestinationStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.Destination, cancellationToken);
        }

        private async Task<DialogTurnResult> OriginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.Destination = (string)stepContext.Result;

            if (LaunchingBotDetails.Origin == null)
            {
                var promptMessage = MessageFactory.Text(OriginStepMsgText, OriginStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.Origin, cancellationToken);
        }

        private async Task<DialogTurnResult> TravelDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.Origin = (string)stepContext.Result;

            if (LaunchingBotDetails.TravelDate == null || IsAmbiguous(LaunchingBotDetails.TravelDate))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), LaunchingBotDetails.TravelDate, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.TravelDate, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.TravelDate = (string)stepContext.Result;

            var messageText = $"Pouvez vous confirmer votre demande ?:\r\n {LaunchingBotDetails.Destination} \r\n Nom du robot: {LaunchingBotDetails.Origin} \r\n le : {LaunchingBotDetails.TravelDate}. Est ce correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(LaunchingBotDetails, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}

  
                    