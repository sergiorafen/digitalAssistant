// <auto-generated>
// Code generated by LUISGen .\FlightBooking.json -cs Luis.FlightBooking -o .
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Microsoft.BotBuilderSamples
{
    public partial class ChatBotLaunching: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            Salutation,
            Todorobot,
            Cancel,
            Aide,
            Information,
            None
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {

            // Built-in entities
            public DateTimeSpec[] datetime;

            // Lists
            public string[][] Airport;

            // Composites
            public class _InstanceFrom
            {
                public InstanceData[] Airport;
            }
            public class FromClass
            {
                public string[][] Airport;
                [JsonProperty("$instance")]
                public _InstanceFrom _instance;
            }
            public FromClass[] From;

            public class _InstanceTo
            {
                public InstanceData[] Airport;
            }
            public class ToClass
            {
                public string[][] Airport;
                [JsonProperty("$instance")]
                public _InstanceTo _instance;
            }
            public ToClass[] To;

            // Instance
            public class _Instance
            {
                public InstanceData[] datetime;
                public InstanceData[] Airport;
                public InstanceData[] From;
                public InstanceData[] To;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<ChatBotLaunching>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }

        public string verifUser(string email)
        {
            string nameClient="string";
            int idCLient,droitUser;
            string error;

            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    String sql = "SELECT ID_Client,Droit,Client FROM[ALPHEDRA_DB].[dbo].[Chatbot_User] WHERE email = @emailU";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@emailU", email);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                //Console.WriteLine("{0} {1} {2} {3}", reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                                /*idCLient = int.Parse(reader.GetString(0));
                                droitUser = int.Parse(reader.GetString(1));*/
                                nameClient = reader.GetString(2);
                            }
                            else
                            {
                                nameClient =null;
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                error = e.ToString();
                nameClient = null;
            }


            return nameClient;
        }

        public string getData(string robot ,int clientID)
        {
            string robotName, robotDevice, robotstatut, robotdescription, result;
            result = "init";
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    String sql = "select A.ID_Robot,B.Robot,A.Device,A.Statut,A.Desciption " +
                        "FROM [ALPHEDRA_DB].[dbo].[Chatbot_Historique_Des_Taches] as A " +
                        "INNER JOIN [ALPHEDRA_DB].[dbo].[Chatbot_Robot] as B on A.ID_Robot=B.ID_Robot where B.Robot=@Robot and A.ID_Client=@IdClient";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@Robot", robot);
                        command.Parameters.AddWithValue("@IdClient", clientID);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                //Console.WriteLine("{0} {1} {2} {3}", reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                                robotName = reader.GetString(1);
                                robotDevice = reader.GetString(2);
                                robotstatut = reader.GetString(3);
                                robotdescription = reader.GetString(4);
                                result = "Voici les informations que vous avez demand� :\r\n Nom:" + robotName + " ,\r\n lanc� sur le device " + robotDevice + ",\r\n son statut:" + robotstatut + ",\r\n description:" + robotdescription;
                            }
                            else
                            {
                                result = "Le robot"+ robot +"a �t� soit mal �crit soit n'existe pas en base de donn�es";
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                result = e.ToString();
            }


            return result;
        }

        public string GetConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "212.114.16.130";
            builder.UserID = "chatbotdev";
            builder.Password = "384E7nV#2!mPzA";
            builder.InitialCatalog = "ALPHEDRA_DB";

            return builder.ConnectionString;
        }
    }
}
