using System;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Net.Http;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class UTDialog : IDialog<object>
    {
        public  async Task StartAsync(IDialogContext context)
        {
            //await context.PostAsync("Du er på UTDialog");
            //var prompt = new PromptDialog.PromptString("Relevant spørsmål angående uføretrygd", "lulu", 3);
            // context.Call(prompt, ReturnAfter);
            string[] options = { "Ja", "Nei" };
            PromptDialog.Confirm(
               context,
               AfterResetAsync,
               "Har din inntektsevne varig redusert med minst 50 % ?",
               "Jeg skjønner bare 'ja' og 'nei'",
               promptStyle: PromptStyle.Keyboard, options: options);
            
            //context.Wait(MessageRecieved);
            //return Task.CompletedTask;

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
                string[] options = { "Ja", "Nei" };
                PromptDialog.Confirm(
               context,
               AfterResetAsync2,
               "Har du hatt 40 % arbeidsavklaringspenger før du innvilges uføretrygd?",
               "Jeg skjønner bare 'ja' og 'nei'",
               promptStyle: PromptStyle.Keyboard, options:options);

            }
            else
            {
                await context.PostAsync("Da er du ikke kvalifisert til uføretrygd.");
                context.Done<object>(null);
            }

        }

        public async Task AfterResetAsync2(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Da kan du få uføretrygd!");
                context.Done<object>(null);

            }
            else
            {
                await context.PostAsync("Da er du ikke kvalifisert til uføretrygd.");
                context.Done<object>(null);
            }

        }
    }
}