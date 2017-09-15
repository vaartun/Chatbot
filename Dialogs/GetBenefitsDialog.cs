using System;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class GetBenefitsDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {

            context.Wait(MessageRecieved);
            return Task.CompletedTask;

        }
       
        protected virtual async Task MessageRecieved(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            //await context.PostAsync("Du er på GetBenefitsDialog");
            var PromptOptions = new string[] { "Uføretrygd", "Arbeidsavklaringspenger", "Avbryt" };
            string Prompt = "Hva kan jeg hjelpe deg med?";
            PromptDialog.Choice(context, OnOptionSelected, PromptOptions, Prompt, "Prøv igjen", 2, PromptStyle.Keyboard);
  
            //var message = await item;
            //context.Done(item);

        }

        private async Task OnOptionSelected(IDialogContext context, IAwaitable<string> result)
        {
            
            try
            {

                string optionSelected = await result;
                System.Diagnostics.Debug.WriteLine($"Option text: {optionSelected}");
                switch (optionSelected)

                {
                    case "Uføretrygd":
                        context.Call(new UTDialog(), ResumeAfterChoose);
                        break;
                    case "Arbeidsavklaringspenger":
                        context.Call(new AAPDialog(), ResumeAfterChoose);
                        break;
                    case "Avbryt":
                        context.Done < object > (null); break;
                   
                    default: { context.Wait(ResumeAfterChoose); break; }
                }
            }
            catch (TooManyAttemptsException ex)
            {
                await context.PostAsync($"Ooops! Too many attemps :(. But don't worry, I'm handling that exception and you can try again!");

                context.Wait(MessageRecieved);
            }
        }

        private Task ResumeAfterChoose(IDialogContext context, IAwaitable<object> result)
        {
            // context.PostAsync("Fikk du svar på spørsmålet ditt?");
            string[] options = { "Ja", "Nei" };
            PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "Fikk du svar på spørsmålet ditt?",
                "Jeg skjønner bare 'ja' og 'nei'",2,
                promptStyle: PromptStyle.Keyboard, options: options);
                return Task.CompletedTask;

        }

        protected virtual async Task MessageDone(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            
            var message = await item;
            context.Done(item);


        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Så bra!");
                context.Done<object>(null);

            }
            else
            {
                string[] options = { "Ja", "Nei" };
                PromptDialog.Confirm(
               context,
               AfterResetAsync2,
               "Så dumt, vil du prøve på nytt?",
               "Jeg skjønner bare 'ja' og 'nei'",
               promptStyle: PromptStyle.Keyboard, options: options);

            }
            
        }

        private async Task AfterResetAsync2(IDialogContext context, IAwaitable<bool> result)
        {
            var confirm = await result;
            if (confirm)
            {

                context.Wait(MessageRecieved);
                var reply = context.MakeMessage();
                reply.Text = "Da prøver vi en gang til";
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                             {
                             new CardAction(){ Title = "Prøv på nytt", Type=ActionTypes.ImBack, Value="Prøv på nytt" },

                             }
                };
                await context.PostAsync(reply);

            }
            else
            {

                context.Done<object>(null);

            }
        }
    }
}

