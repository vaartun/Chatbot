// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Gary Pretty Github:
// https://github.com/GaryPretty
// 
// Code derived from existing dialogs within the Microsoft Bot Framework
// https://github.com/Microsoft/BotBuilder
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Bot.Builder.Internals.Fibers;
using System.Text.RegularExpressions;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    
    public class QnAMakerDialog<T> : IDialog<T>
    {
        private string _subscriptionKey;
        private string _knowledgeBaseId;

        public string SubscriptionKey { get => _subscriptionKey; set => _subscriptionKey = value; }

        public string KnowledgeBaseID { get => _knowledgeBaseId; set => _knowledgeBaseId = value; }

        [NonSerialized]
        protected Dictionary<QnAMakerResponseHandlerAttribute, QnAMakerResponseHandler> HandlersByMaximumScore;
        
        public virtual async Task StartAsync(IDialogContext context)
        {
            var type = this.GetType();
            var QnAServiceAttribute = type.GetCustomAttributes<QnAMakerServiceAttribute>().FirstOrDefault();

            if (string.IsNullOrEmpty(KnowledgeBaseID) && QnAServiceAttribute != null)
                KnowledgeBaseID = QnAServiceAttribute.KnowledgeBaseID;

            if (string.IsNullOrEmpty(SubscriptionKey) && QnAServiceAttribute != null)
                SubscriptionKey = QnAServiceAttribute.SubscriptionKey;

            if(string.IsNullOrEmpty(KnowledgeBaseID) || string.IsNullOrEmpty(SubscriptionKey))
            {
                throw new Exception("Valid KnowledgeBaseID and SubsriptionKey not provided. Use QnAMakeServiceAttribute or set fields on QnAMAkerDialog");
            }

            context.Wait(MessageRecieved);
        }

        protected virtual async Task MessageRecieved(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            
            try {
                var message = await item;
                await HandleMessage(context, message.Text);
            }
            catch
            {
               await context.PostAsync("A problem occured");
                
            }
            finally
            {
                context.Done(item);
            }
            
        }

        private async Task HandleMessage(IDialogContext context, string queryText)
        {
            var response = await GetQnAMakerResponse(queryText, KnowledgeBaseID, SubscriptionKey);

            if (HandlersByMaximumScore == null)
            {
                HandlersByMaximumScore =
                    new Dictionary<QnAMakerResponseHandlerAttribute, QnAMakerResponseHandler>(GetHandlersByMaximumScore());
            }

            if (response.Score == 0)
            {
                await NoMatchHandler(context, queryText);
            }
            else
            {
                var applicableHandlers = HandlersByMaximumScore.OrderBy(h => h.Key.MaximumScore).Where(h => h.Key.MaximumScore > response.Score);
                var handler = applicableHandlers.Any() ? applicableHandlers.First().Value : null;

                if (handler !=null)
                {
                    await handler.Invoke(context, queryText, response);
                }
                else
                {
                    await DefaultMatchHandler(context, queryText, response);
                }
            }
        }

        private async Task<QnAMakerResult> GetQnAMakerResponse(string query, string knowledgeBaseID, string subscriptionKey)
        {
            string responseString = string.Empty;

            var knowledgebaseID = knowledgeBaseID; // Use knowledge base id created.
            var qnamakerSubscriptionKey = subscriptionKey; //Use subscription key assigned to you.

            //Build the URI

            Uri qnamakerUriBase = new Uri("https://westus.api.cognitive.microsoft.com/qnamaker/v1.0");
            var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{knowledgeBaseID}/generateAnswer");

            //Add the question as part of the body
            var postBody = $"{{\"question\": \"{query}\"}}";

            //Send the POST request
            using (WebClient client = new WebClient())
            {
                //Set the encoding to UTF8
                client.Encoding = System.Text.Encoding.UTF8;

                //Add the subscription key header
                client.Headers.Add("Ocp-Apim-Subscription-Key", qnamakerSubscriptionKey);
                client.Headers.Add("Content-Type", "application/json");
                responseString = client.UploadString(builder.Uri, postBody);
            }

            //De-serialize the response
            QnAMakerResult response;
            try
            {
                response = JsonConvert.DeserializeObject<QnAMakerResult>(responseString);
                return response;
            }
            catch
            {
                throw new Exception("Unable to deserialize QnA Maker response string.");
            }
        }
        
        public virtual async Task DefaultMatchHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            messageActivity.Text = result.Answer;
            await context.PostAsync(messageActivity);
            context.Wait(MessageRecieved);
        }

        public virtual async Task NoMatchHandler(IDialogContext context, string originalQueryText)
        {
            throw new Exception("Sorry, I cannot find an answer to your question.");
        }

        protected virtual IDictionary<QnAMakerResponseHandlerAttribute, QnAMakerResponseHandler> GetHandlersByMaximumScore()
        {
            return EnumerateHandlers(this).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        internal static IEnumerable<KeyValuePair<QnAMakerResponseHandlerAttribute, QnAMakerResponseHandler>> EnumerateHandlers(object dialog)
        {
            var type = dialog.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var QnAResponseHandelerAttributes = method.GetCustomAttributes<QnAMakerResponseHandlerAttribute>(inherit: true).ToArray();
                Delegate created = null;
                try
                {
                    created = Delegate.CreateDelegate(typeof(QnAMakerResponseHandler), dialog, method, throwOnBindFailure: false);
                }
                catch(ArgumentException)
                {
                    // "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type."
                    // https://github.com/Microsoft/BotBuilder/issues/634
                    // https://github.com/Microsoft/BotBuilder/issues/435
                }

                var qNaResponseHanlder = (QnAMakerResponseHandler)created;
                if (qNaResponseHanlder != null)
                {
                    foreach (var qNaResponseAttribute in QnAResponseHandelerAttributes)
                    {
                        if (qNaResponseAttribute != null && QnAResponseHandelerAttributes.Any())
                            yield return new KeyValuePair<QnAMakerResponseHandlerAttribute, QnAMakerResponseHandler>(qNaResponseAttribute, qNaResponseHanlder);
                    }
                }
            }
        }

        protected static IMessageActivity ProcessResultAndCreateMessageActivity(IDialogContext context, ref QnAMakerResult result)
        {
            var message = context.MakeMessage();

            //var attachmentsItemRegex = new Regex("((&lt;attachment){1}((?:\\s+)|(?:(contentType=&quot;[\\w\\/]+&quot;))(?:\\s+)|(?:(contentUrl=&quot;[\\w:/.]+&quot;))(?:\\s+)|(?:(name=&quot;[\\w\\s]+&quot;))(?:\\s+)|(?:(thumbnailUrl=&quot;[\\w:/.]+&quot;))(?:\\s+))+(/&gt;))", RegexOptions.IgnoreCase);
            var attachmentsItemRegex = new Regex("((&lt;attachment){1}((?:\\s+)|(?:(contentType=&quot;[\\w\\/-]+&quot;))(?:\\s+)|(?:(contentUrl=&quot;[\\w:/.=?-]+&quot;))(?:\\s+)|(?:(name=&quot;[\\w\\s&?\\-.@%$!£\\(\\)]+&quot;))(?:\\s+)|(?:(thumbnailUrl=&quot;[\\w:/.=?-]+&quot;))(?:\\s+))+(/&gt;))", RegexOptions.IgnoreCase);
            var matches = attachmentsItemRegex.Matches(result.Answer);

            foreach (var attachmentMatch  in matches)
            {
                result.Answer = result.Answer.Replace(attachmentMatch.ToString(), string.Empty);

                var match = attachmentsItemRegex.Match(attachmentMatch.ToString());
                string contentType = string.Empty;
                string name = string.Empty;
                string contentUrl = string.Empty;
                string thumbnailUrl = string.Empty;

                foreach (var group in match.Groups)
                {
                    if(group.ToString().ToLower().Contains("contenttype="))
                    {
                        contentType = group.ToString().ToLower().Replace(@"contenttype=&quot;", string.Empty).Replace("&quot;", string.Empty);
                    }
                    if (group.ToString().ToLower().Contains("contenturl="))
                    {
                        contentUrl = group.ToString().ToLower().Replace(@"contenturl=&quot;", string.Empty).Replace("&quot;", string.Empty);
                    }
                    if (group.ToString().ToLower().Contains("name="))
                    {
                        name = group.ToString().ToLower().Replace(@"name=&quot;", string.Empty).Replace("&quot;", string.Empty);
                    }
                    if (group.ToString().ToLower().Contains("thumbnailurl="))
                    {
                        thumbnailUrl = group.ToString().ToLower().Replace(@"thumbnailurl=&quot;", string.Empty).Replace("&quot;", string.Empty);
                    }
                }

                var attachment = new Attachment(contentType, contentUrl, name: !string.IsNullOrEmpty(name) ? name : null, thumbnailUrl: !string.IsNullOrEmpty(thumbnailUrl) ? thumbnailUrl : null);
                message.Attachments.Add(attachment);
            }
            return message;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class QnAMakerResponseHandlerAttribute : Attribute
    {
        public readonly double MaximumScore;

        public QnAMakerResponseHandlerAttribute(double maximumScore)
        {
            MaximumScore = maximumScore;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    [Serializable]
    public class QnAMakerServiceAttribute : Attribute
    {
        private readonly string subscriptionKey;
        public string SubscriptionKey => subscriptionKey;

        private readonly string knowledgeBaseID;
        public string KnowledgeBaseID => knowledgeBaseID;

        public QnAMakerServiceAttribute(string subscriptionKey, string knowledgeBaseID)
        {
            SetField.NotNull(out this.subscriptionKey, nameof(subscriptionKey), subscriptionKey);
            SetField.NotNull(out this.knowledgeBaseID, nameof(knowledgeBaseID), knowledgeBaseID);
        }
    }

    public delegate Task QnAMakerResponseHandler(IDialogContext context, string originalQueryText, QnAMakerResult result);
}