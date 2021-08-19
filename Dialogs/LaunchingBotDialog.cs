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
        private const string ConfirmFirstStepMsgText = "Voulez vous vraiment lancer un robot? "; 
        private const string ConfirmrRequestSecondMsgText = "Voulez vous que j'exécute un robot en particulier ?";
        private const string RequeteClientStepMsgText = "Quel robot voulez vous que j'exécute ?";
        private const string didntUnderstandMessageText = "Désolé je n'ai pas compris.\r\nPourriez vous reformler votre demande ?";
        private const string DeviceRobotMessageText = "Sur quel ordinateur voulez vous le lancer?";

        public LaunchingBotDialog()
            : base(nameof(LaunchingBotDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ConfirmFirstStepAsync,
                AllClientBotInfoStepAsync,
                RequeteClientStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ConfirmFirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            if (LaunchingBotDetails.ConfirmationFirstInfo == null)
            {
                var promptMessage = MessageFactory.Text(ConfirmFirstStepMsgText, ConfirmFirstStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.RobotName, cancellationToken);
        }

        private async Task<DialogTurnResult> AllClientBotInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;
            LaunchingBotDetails.ConfirmationFirstInfo = (string)stepContext.Result;

            if (LaunchingBotDetails.ConfirmationFirstInfo == "oui")
            {
                ChatBotLaunching SqlChatbot = new ChatBotLaunching();
                var infoRobotMessageText = SqlChatbot.allBotClient("1706");

                string strVoici = "Avant de lancer un robot en particulier ,voici la liste de vos robots";
                var infoRobotAllMessage = MessageFactory.Text(strVoici, strVoici, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(infoRobotAllMessage, cancellationToken);

                foreach (var a in infoRobotMessageText)
                {
                    var infoRobotMessage = MessageFactory.Text(a.ToString(), a.ToString(), InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(infoRobotMessage, cancellationToken);
                }

            }
            else if (LaunchingBotDetails.ConfirmationFirstInfo == "non")
            {
                return await stepContext.EndDialogAsync();
            }
            else
            {
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                return await stepContext.EndDialogAsync();
            }

            return await stepContext.NextAsync(LaunchingBotDetails.ConfirmationFirstInfo, cancellationToken);
        }
        private async Task<DialogTurnResult> ConfirmSecondInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            if (LaunchingBotDetails.ConfirmationSecondInfo == null)
            {
                var promptMessage = MessageFactory.Text(ConfirmrRequestSecondMsgText, ConfirmrRequestSecondMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.ConfirmationFirstInfo, cancellationToken);
        }

        private async Task<DialogTurnResult> RequeteClientStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;
            LaunchingBotDetails.ConfirmationFirstInfo = (string)stepContext.Result;

            if (LaunchingBotDetails.ConfirmationFirstInfo == "oui")
            {
                if (LaunchingBotDetails.RobotName == null)
                {
                    var promptMessage = MessageFactory.Text(RequeteClientStepMsgText, RequeteClientStepMsgText, InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }
            else if (LaunchingBotDetails.ConfirmationFirstInfo == "non")
            {
                return await stepContext.EndDialogAsync();
            }
            else
            {
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                return await stepContext.EndDialogAsync();
            }

            return await stepContext.NextAsync(LaunchingBotDetails.RobotName, cancellationToken);
        }

      /*  private async Task<DialogTurnResult> DeviceRobotStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.RobotName = (string)stepContext.Result;

            if (LaunchingBotDetails.DeviceRobot == null)
            {
                var promptMessage = MessageFactory.Text(DeviceRobotMessageText, DeviceRobotMessageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.DeviceRobot, cancellationToken);
        }*/

        /*private async Task<DialogTurnResult> DateDemandeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.RequeteClient = (string)stepContext.Result;

            if (LaunchingBotDetails.DateDemande == null || IsAmbiguous(LaunchingBotDetails.DateDemande))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), LaunchingBotDetails.DateDemande, cancellationToken);
            }

            return await stepContext.NextAsync(LaunchingBotDetails.DateDemande, cancellationToken);
        }*/

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var LaunchingBotDetails = (LaunchingBotDetails)stepContext.Options;

            LaunchingBotDetails.RobotName = (string)stepContext.Result;

            var messageText = $"Pouvez vous confirmer votre demande ?: \r\n lancer le robot: {LaunchingBotDetails.RobotName}. Est ce correct?";
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

  
                    