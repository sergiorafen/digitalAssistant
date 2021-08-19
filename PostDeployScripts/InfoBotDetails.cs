
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples
{
    public class InfoBotDetails
    {
        public string RobotName { get; set; }
        public string DeviceRobot { get; set; }
        public string StatutRobot { get; set; }

        public string mailClient { get; set; }

        public string ConfirmationFirstInfo { get; set; }
        public string ConfirmationSecondInfo { get; set; }

        public TokenResponse tokenResponseUser { get; set; }

    }
}
