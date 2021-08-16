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
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ChatbotRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ChatbotRecognizer luisRecognizer, LaunchingBotDialog bookingDialog, InfoDialog informationDialog ,ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(informationDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
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

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            var messageText = stepContext.Options?.ToString() ?? $"Je peux vous aider :\r\n" +
                $"- pour lancer un robot \r\n" +
                $"- vous donner des informations par rapport à votre robot \r\n" +
                $"- vous mettre en relation avec un consultant Alphedra \r\n" +
                $"" +
                $"Je vous écoute ?";



            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
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
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(LaunchingBotDialog), LaunchingBotDetails, cancellationToken);

                case ChatBotLaunching.Intent.Aide:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "Votre demande a bien été prise en compte un consultant Alphedra viendra pour vous aider dans les plus brefs délais ,\r\n , Vous pouvez également nous contacter par email : email@alphedra.com, ou par téléphone au + 33 (0) 6 27 12 36 64  ";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                case ChatBotLaunching.Intent.Information:
                    await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                    /*var getInformationMessageText = getData("Clartan VNI66");
                    var getInformationMessage = MessageFactory.Text(getInformationMessageText, getInformationMessageText, InputHints.IgnoringInput);

                    await stepContext.Context.SendActivityAsync(getInformationMessage, cancellationToken);*/
                    var InfoBotDetails = new InfoBotDetails()
                    {
                        // Get RobotName and RequeteClient from the composite entities arrays.
                        RobotName = luisResult.ToEntities.Airport,
                        DeviceRobot = luisResult.FromEntities.Airport,
                        StatutRobot = luisResult.TravelDate,
                    };
                    return await stepContext.BeginDialogAsync(nameof(InfoDialog), InfoBotDetails, cancellationToken);

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Désolé je n'ai pas compris.\r\nPourriez vous reformler votre demande ? (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
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
                var messageText = $"Votre demande de lancer le robot  {result.RobotName} \r\n a bien été enregistré";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
                InsertData(result.RobotName);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Que puis je faire d'autre pour vous?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        public void InsertData(string robot)
        {
            string result = "init";
            DateTime now = DateTime.Now;
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "212.114.16.130";
                builder.UserID = "chatbotdev";
                builder.Password = "384E7nV#2!mPzA";
                builder.InitialCatalog = "ALPHEDRA_DB";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    String sql = "insert into [ALPHEDRA_DB].[dbo].[Chatbot_Robot_Client] (Client,Robot,Date_Demande)  values (@Client,@Robot,@Date_Demande)";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Client", "Clartan");
                        command.Parameters.AddWithValue("@Robot", robot);
                        command.Parameters.AddWithValue("@Date_Demande", now);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException e)
            {
                result = e.ToString();
            }
        }
    }
}
