// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Data.SqlClient;
using System.Text;
using Microsoft.Graph;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : LogoutDialog
    {
        private readonly ChatbotRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public ChatBotLaunching chatBotADO = new ChatBotLaunching();
        public TokenResponse tokenResponse;
        public InfoBotDetails InfoBotDetails;


        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ChatbotRecognizer luisRecognizer, LaunchingBotDialog bookingDialog, InfoDialog informationDialog , IConfiguration configuration,ILogger<MainDialog> logger)
            : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            string textLogin = "Pour pouvoir utiliser ce chatbot, vous devez ?tre connect?";
            string titleLogin = "Se connecter";


            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = textLogin,
                    Title = titleLogin,
                    Timeout = 300000, // User has 5 minutes to login
                }));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(informationDialog);
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                IntroStepAsync,
                CommandStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            //Check token
             tokenResponse  = (TokenResponse)stepContext.Result;

            if (tokenResponse != null)
            {
                // Use the text provided in FinalStepAsync or the default if it is the first time.
                var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
                await OAuthHelpers.ListMeAsync(stepContext.Context, tokenResponse);
                var messageText = stepContext.Options?.ToString() ?? $"Je peux vous aider :\r\n" +
                    $"- pour lancer un robot \r\n" +
                    $"- vous donner des informations par rapport ? votre robot \r\n" +
                    $"- vous mettre en relation avec un consultant Alphedra \r\n" +
                    $"" +
                    $"Je vous ?coute ?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync();

        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty LaunchingBotDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(LaunchingBotDialog), new LaunchingBotDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<ChatBotLaunching>(stepContext.Context, cancellationToken);
            string emailClient = "emailClient";
            string promptMessageRessource = "promptMessageRessource";

            var tokenResponse = stepContext.Result as TokenResponse;
            var client = new SimpleGraphClient(tokenResponse.Token);
             await client.GetMeAsync();
            User tmpUSer = await client.GetMeAsync();
            string emailUser = tmpUSer.Mail;

            emailClient = chatBotADO.verifUser(emailUser);

            if (emailClient != null)
            {
                switch (luisResult.TopIntent().intent)
                {
                    case ChatBotLaunching.Intent.Todorobot:
                        await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                        // Initialize LaunchingBotDetails with any entities we may have found in the response.
                        var LaunchingBotDetails = new LaunchingBotDetails()
                        {
                            // Get RobotName and RequeteClient from the composite entities arrays.
                            RobotName = luisResult.ToEntities.Airport,
                            RequeteClient = luisResult.FromEntities.Airport,
                            DateDemande = luisResult.TravelDate,
                            mailClient = emailUser,
                        };

                        // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                        //nameClient = chatBotADO.verifUser(emailUser);
                        if (emailClient != null)
                        {
                            return await stepContext.BeginDialogAsync(nameof(LaunchingBotDialog), LaunchingBotDetails, cancellationToken);
                        }

                        promptMessageRessource = "Vous n'?tes pas autoris? ? lancer un robot";
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(promptMessageRessource), cancellationToken);

                        break;


                    case ChatBotLaunching.Intent.Aide:
                        // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                        var getWeatherMessageText = "Votre demande a bien ?t? prise en compte un consultant Alphedra viendra pour vous aider dans les plus brefs d?lais ,\r\n , Vous pouvez ?galement nous contacter par email : email@alphedra.com, ou par t?l?phone au + 33 (0) 6 27 12 36 64  ";
                        var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                        break;

                    case ChatBotLaunching.Intent.Information:
                        await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                        /*var getInformationMessageText = getData("Clartan VNI66");
                        var getInformationMessage = MessageFactory.Text(getInformationMessageText, getInformationMessageText, InputHints.IgnoringInput);

                        await stepContext.Context.SendActivityAsync(getInformationMessage, cancellationToken);*/
                        InfoBotDetails = new InfoBotDetails()
                        {
                            // Get RobotName and RequeteClient from the composite entities arrays.
                            RobotName = luisResult.ToEntities.Airport,
                            DeviceRobot = luisResult.FromEntities.Airport,
                            StatutRobot = luisResult.TravelDate,
                            tokenResponseUser = tokenResponse,
                            mailClient = emailUser,
                        };

                        //nameClient=chatBotADO.verifUser("sergio.enomanana@alphedra.com");
                        if (emailClient != null)
                        {
                            return await stepContext.BeginDialogAsync(nameof(InfoDialog), InfoBotDetails, cancellationToken);
                        }

                        promptMessageRessource = "Vous n'?tes pas autoris? ? utiliser cette ressource";
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(promptMessageRessource), cancellationToken);
                        break;

                    default:
                        // Catch all for unhandled intents
                        var didntUnderstandMessageText = $"D?sol? je n'ai pas compris.\r\nPourriez vous reformuler votre demande ?";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        break;
                }
            }
            else {
                promptMessageRessource = "Vous n'?tes pas autoris? ? utiliser ce chatbot . \r\nVous pouvez ?galement nous contacter par email : email@alphedra.com, ou par t?l?phone au + 33 (0) 6 27 12 36 64";
                await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessageRessource, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, ChatBotLaunching luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is LaunchingBotDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.


                /*var timeProperty = new TimexProperty(result.DateDemande);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);*/
                var messageText = $"Votre demande de lancer le robot  {result.RobotName} \r\n a bien ?t? enregistr?";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);

                var client = new SimpleGraphClient(tokenResponse.Token);
                await client.GetMeAsync();
                User tmpUSer = await client.GetMeAsync();

                string ncompanyUser = chatBotADO.companyName(tmpUSer.Mail);

                chatBotADO.InsertData(result.RobotName, ncompanyUser);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Que puis je faire d'autre pour vous?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                await OAuthHelpers.ListMeAsync(stepContext.Context, tokenResponse);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Vous ?tes actuellement connect?"), cancellationToken);
                //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to do? (type 'me', or 'email')") }, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> CommandStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result != null)
            {

                var tokenResponse = stepContext.Result as TokenResponse;

                // If we have the token use the user is authenticated so we may use it to make API calls.
                if (tokenResponse?.Token != null)
                {
                    var command = ((string)stepContext.Values["command"] ?? string.Empty).Trim().ToLowerInvariant();

                    if (command == "me")
                    {
                        await OAuthHelpers.ListMeAsync(stepContext.Context, tokenResponse);
                    }
                    else if (command.StartsWith("email"))
                    {
                        await OAuthHelpers.ListEmailAddressAsync(stepContext.Context, tokenResponse);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your token is: {tokenResponse.Token}"), cancellationToken);
                    }
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("We couldn't log you in. Please try again later."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
