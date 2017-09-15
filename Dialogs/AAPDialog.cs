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
    public class AAPDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.PostAsync("Du er på AAPDialog");
            context.Wait(MessageRecieved);
            context.Done<object>(null);
            return Task.CompletedTask;

        }

        protected virtual async Task MessageRecieved(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            context.Done(item);

        }
    }
}