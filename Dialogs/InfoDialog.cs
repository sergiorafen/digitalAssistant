// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Text;
using System;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class InfoDialog : CancelAndHelpDialog
    {
        private const string ConfirmrRequestStepMsgText = "Voulez vous vraiment une information sur un robot? (Taper oui ou non)";
        private const string RobotNameStepMsgText = "Quel est le nom du robot dont vous voulez avoir une information ?";
        private const string ConfirmrRequestSecondMsgText = "Voulez vous avoir des informations par rapport à un robot en particulier?";
        // private const string RobotDeviceStepMsgText = "Sur quel ordinateur le robot est il lancé?";
        private const string didntUnderstandMessageText = "Désolé je n'ai pas compris.\r\nPourriez vous reformler votre demande ?";


        public InfoDialog()
            : base(nameof(InfoDialog))
        {
            /*AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());*/
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ConfirmFirstInfoStepAsync,
                AllClientBotInfoStepAsync,
                ConfirmSecondInfoStepAsync,
                RobotNameStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ConfirmFirstInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            if (InfoBotDetails.ConfirmationFirstInfo== null)
            {
                var promptMessage = MessageFactory.Text(ConfirmrRequestStepMsgText, ConfirmrRequestStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.ConfirmationFirstInfo, cancellationToken);
        }

        private async Task<DialogTurnResult> AllClientBotInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;
            InfoBotDetails.ConfirmationFirstInfo = (string)stepContext.Result;

            if (InfoBotDetails.ConfirmationFirstInfo == "oui")
            {
                ChatBotLaunching SqlChatbot = new ChatBotLaunching();
                var infoRobotMessageText = SqlChatbot.allBotClient("1706");

                string strVoici = "Voici la liste de vos robots";
                var infoRobotAllMessage = MessageFactory.Text(strVoici, strVoici, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(infoRobotAllMessage, cancellationToken);

                foreach (var a in infoRobotMessageText)
                {
                    var infoRobotMessage = MessageFactory.Text(a.ToString(), a.ToString(), InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(infoRobotMessage, cancellationToken);
                }

            }
            else if (InfoBotDetails.ConfirmationFirstInfo == "non")
            {
                return await stepContext.EndDialogAsync();
            }
            else
            {
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                return await stepContext.EndDialogAsync();
            }

            return await stepContext.NextAsync(InfoBotDetails.ConfirmationFirstInfo, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmSecondInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            if (InfoBotDetails.ConfirmationSecondInfo == null)
            {
                var promptMessage = MessageFactory.Text(ConfirmrRequestSecondMsgText, ConfirmrRequestSecondMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.ConfirmationFirstInfo, cancellationToken);
        }

        private async Task<DialogTurnResult> RobotNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.ConfirmationFirstInfo = (string)stepContext.Result;

            if (InfoBotDetails.ConfirmationFirstInfo == "oui")
            {
                if (InfoBotDetails.RobotName == null)
                {
                    var promptMessage = MessageFactory.Text(RobotNameStepMsgText, RobotNameStepMsgText, InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }
            else if (InfoBotDetails.ConfirmationFirstInfo == "non")
            {
                return await stepContext.EndDialogAsync();
            }
            else
            {
                var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                return await stepContext.EndDialogAsync();
            }

            return await stepContext.NextAsync(InfoBotDetails.RobotName, cancellationToken);
        }

        /*private async Task<DialogTurnResult> DeviceRobotStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.RobotName = (string)stepContext.Result;

            if (InfoBotDetails.DeviceRobot == null)
            {
                var promptMessage = MessageFactory.Text(RobotDeviceStepMsgText, RobotDeviceStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(InfoBotDetails.DeviceRobot, cancellationToken);
        }
            */
       /* private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;

            InfoBotDetails.DeviceRobot = (string)stepContext.Result;

            var messageText = $"Pouvez vous confirmer votre demande ?:\r\n Nom du robot: {InfoBotDetails.RobotName} \r\n Confirmation : {InfoBotDetails.ConfirmationFirstInfo}, Device du Robot: {InfoBotDetails.DeviceRobot} . Est ce correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }*/

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var InfoBotDetails = (InfoBotDetails)stepContext.Options;
            InfoBotDetails.RobotName = (string)stepContext.Result;

            /*vaInfoBotDetails.RobotName = (string)stepContext.Result;*/
            if (InfoBotDetails.RobotName != null)
            {
                string myRobot = InfoBotDetails.RobotName;
                //var infoRobotMessageText = getData(myRobot);

                ChatBotLaunching SqlChatbot = new ChatBotLaunching();
                var infoRobotMessageText= SqlChatbot.getData(myRobot,702);
                var infoRobotMessage = MessageFactory.Text(infoRobotMessageText, infoRobotMessageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(infoRobotMessage, cancellationToken);
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


