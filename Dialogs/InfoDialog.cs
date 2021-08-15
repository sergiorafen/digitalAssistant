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
    public class InfoDialog : CancelAndHelpDialog
    {
        private const string ConfirmrRequestStepMsgText = "Voulez vous vraiment une information sur un robot? ";
        private const string RobotNameStepMsgText = "Quel est le nom du robot dont vous voulez avoir des informations ?";

        public InfoDialog()
            : base(nameof(LaunchingBotDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                RobotNameStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            })); ;

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> RobotNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            if (InfoBotDetails.RobotName == null)
            {
                var promptMessage = MessageFactory.Text(ConfirmrRequestStepMsgText, ConfirmrRequestStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.RobotName, cancellationToken);
        }

       private async Task<DialogTurnResult> RobotNamStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.RobotName = (string)stepContext.Result;

            if (InfoBotDetails.RobotName == null)
            {
                var promptMessage = MessageFactory.Text(RobotNameStepMsgText, RobotNameStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.RobotName, cancellationToken);
        }

        private async Task<DialogTurnResult> DeviceRobotStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.RobotName = (string)stepContext.Result;

            if (InfoBotDetails.DeviceRobot == null || IsAmbiguous(InfoBotDetails.DeviceRobot))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), InfoBotDetails.DeviceRobot, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.DeviceRobot, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.RobotName = (string)stepContext.Result;

            var messageText = $"Voulez vous vraiment une information sur un robot?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var InfoBotDetails = (InfoBotDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(InfoBotDetails, cancellationToken);
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


