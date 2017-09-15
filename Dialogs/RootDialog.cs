
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Web.Http;
using System.Net.Http;


namespace Bot_Application1.Dialogs
{

    [LuisModel("INSERT LUIS APP ID", "INSERT LUIS ENDPOINT KEY")]
    [Serializable]
    public class RootDialog : LuisDialog<object>
    {
       [LuisIntent("")]
       [LuisIntent("None")]
       public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Beklager, jeg skjønner ikke '{result.Query}' Skriv 'hjelp' om du trenger assistanse";


            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]

        public async Task Help(IDialogContext context, LuisResult result)
        {

            var reply = context.MakeMessage();
            reply.Text = "Prøv å stille spørsmål som 'Hvor lenge kan jeg få arbeidsavklaringspenger?' eller 'Kan jeg få uføretrygd?'";
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                             {
                             new CardAction(){ Title = "AAP", Type=ActionTypes.ImBack, Value="Hvor lenge kan jeg få arbeidsavklaringspenger?" },
                             new CardAction(){ Title = "Uføretrygd", Type=ActionTypes.ImBack, Value="Kan jeg få uføretrygd?" },
                            // new CardAction(){ Title = "Annet", Type=ActionTypes.ImBack, Value="hjelp annet" }

                             }
            };
            await context.PostAsync(reply);

            context.Wait(this.MessageReceived);


        }

        [LuisIntent("GetDisabilityBenefits")]
        public async Task GetDisabilityBenefits(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            var message = await item;
            message.Text = message.Summary;
            await context.Forward(new GetBenefitsDialog(), AfterQnADialog, message, CancellationToken.None);

        }

        [LuisIntent("LearnAboutLegalRights")]
        public async Task LearnAboutLegalRights(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            var message = await item;
            //message.Text = "Hva gjør jeg hvis jeg ikke finner studiet jeg vil søke på?";

            //forwards the dialog to QnADialog.cs, when QnADialog is finished the dialog gets sendt to AfterQnADialog
            //message is the query that is sendt to QnADialog.cs
            await context.Forward(new LegalInfoDialog(), AfterQnADialog, message, CancellationToken.None);
            context.Done(item);

        }

        [LuisIntent("QnAQuestions")]
        public async Task QnAQuestions(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            var message = await item;
            message.Text = message.Summary;

            System.Diagnostics.Debug.WriteLine($"Final message: {message.Text}");

            await context.Forward(new QnADialog(), AfterQnADialog, message, CancellationToken.None);

        }

   

        public async Task AfterQnADialog(IDialogContext context, IAwaitable<object> result)
        {

            await context.PostAsync("Er det noe annet du lurer på?");
            var messageHandled = await result;
          context.Wait(MessageReceived);
           

        }

        protected virtual async Task AfterDialog(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("Er det noe annet du lurer på?");
            var messageHandled = await result;
            context.Wait(MessageReceived);
            
            

        }
    }
}