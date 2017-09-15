using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Linq;

namespace Bot_Application1
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user, translates it to english, and sends to RootDialog
        /// </summary>
        /// 
        //ApiKey for microsoft translator
        string ApiKey = "INSERT TRANSLATOR TEXT APIKEY HERE";
        //the language you want to translate to, ie "en", "no", etc. 
        string targetLang = "en";
        string sourceLang = "no";

        public async Task<string> translate(string input)
        {
            var accessToken = await GetAuthenticationToken(ApiKey);
            var output = await TranslateText(input, targetLang,sourceLang, accessToken);
            return output;
        }

        internal static IDialog<object> MakeRoot()
        {


            return Chain.From(() => new Dialogs.RootDialog());
        }

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {


            if (activity.Type == ActivityTypes.Message)
            {
                try
                {   
                    //get user input
                    var msg = activity.Text;
                    //translate user input
                    var transMsg = translate(msg);
                    //swap user input for translated text
                    activity.Text = await transMsg;
                    //store original input for later use
                    activity.Summary = msg;
                    //send translated query to RootDialog
                    activity.Text = activity.Text.Replace("Disability", "Uføretrygd");
                    activity.Text = activity.Text.Replace("The avklarings money", "Arbeidsavklaringspenger");
                    activity.Text = activity.Text.Replace("Cancel", "Avbryt");
                    System.Diagnostics.Debug.WriteLine($"translated text: {activity.Text}");
                    await Conversation.SendAsync(activity, MakeRoot);
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels

                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if (update.MembersAdded != null && update.MembersAdded.Any())
                {
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != message.Recipient.Id)
                        {
                            var reply = message.CreateReply();
                            reply.Text = $"Hei jeg er Kreftforeningens nye Chatbot, hva kan jeg hjelpe deg med?";
                            client.Conversations.ReplyToActivityAsync(reply);
                           

                        }
                    }
                }
                
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
                
            }
            else if (message.Type == ActivityTypes.Ping)
            {
                
            }

            return null;
        }

        static async Task<string> TranslateText(string inputText, string language, string source, string accessToken)
        {
            string url = "http://api.microsofttranslator.com/v2/Http.svc/Translate";
            string query = $"?text={System.Net.WebUtility.UrlEncode(inputText)}&from={source}&to={language}&contentType=text/plain";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync(url + query);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "Hata: " + result;

                var translatedText = XElement.Parse(result).Value;
                return translatedText;
            }
        }

        static async Task<string> GetAuthenticationToken(string key)
        {
            string endpoint = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var response = await client.PostAsync(endpoint, null);
                var token = await response.Content.ReadAsStringAsync();
                return token;
            }
        }

    }
}