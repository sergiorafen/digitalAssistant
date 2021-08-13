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
        private const string RobotNameStepMsgText = "Voulez vous vraiment lancer un robot? ";
        private const string RequeteClientStepMsgText = "Quel robot voulez vous que j'exécute ?";

        public LaunchingBotDialog()
            : base(nameof(LaunchingBotDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                RobotNameStepAsync,
                RequeteClientStepAsync,
                DateDemandeStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> RobotNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            if (LaunchingBotDetails.RobotName == null)
            {
                var promptMessage = MessageFactory.Text(RobotNameStepMsgText, RobotNameStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.RobotName, cancellationToken);
        }

        private async Task<DialogTurnResult> RequeteClientStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.RobotName = (string)stepContext.Result;

            if (LaunchingBotDetails.RequeteClient == null)
            {
                var promptMessage = MessageFactory.Text(RequeteClientStepMsgText, RequeteClientStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.RequeteClient, cancellationToken);
        }

        private async Task<DialogTurnResult> DateDemandeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.RequeteClient = (string)stepContext.Result;

            if (LaunchingBotDetails.DateDemande == null || IsAmbiguous(LaunchingBotDetails.DateDemande))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), LaunchingBotDetails.DateDemande, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.DateDemande, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.DateDemande = (string)stepContext.Result;

            var messageText = $"Pouvez vous confirmer votre demande ?:\r\n {LaunchingBotDetails.RobotName} \r\n Nom du robot: {LaunchingBotDetails.RequeteClient} \r\n le : {LaunchingBotDetails.DateDemande}. Est ce correct?";
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

  
                    