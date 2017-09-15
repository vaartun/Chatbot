using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Bot_Application1.Dialogs
{

    [Serializable]
    [QnAMakerService("INSERT QNA MAKER SUBSCRIPTION KEY", "INSERT QNA MAKER KNOWLEDGE BASE KEY")]
    public class QnADialog : QnAMakerDialog<object>
    {
        /// <summary>
        /// Handler used when the QnAMaker finds no appropriate answer
        /// </summary>

        public override async Task NoMatchHandler(IDialogContext context, string originalQueryText)
        {
            await context.PostAsync($"Finner ingen svar for spørsmålet: '{originalQueryText}'.");
            context.Wait(MessageRecieved);
        }

        /// <summary>
        /// This is the default handler used if no specific applicable score handlers are found
        /// </summary>
        public override async Task DefaultMatchHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            // ProcessResultAndCreateMessageActivity will remove any attachment markup from the results answer
            // and add any attachments to a new message activity with the message activity text set by default
            // to the answer property from the result

            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            //messageActivity.Text = $"ingen score på denne: {result.Answer}.";
            messageActivity.Text = result.Answer;

            await context.PostAsync(messageActivity);

           // context.Wait(MessageRecieved);
        }

       
        //Handler to respond when QnAMakerResult score is a maximum of 100
        [QnAMakerResponseHandler(100)]
        public async Task veryHighScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            // messageActivity.Text = $"Her er svaret på spørsmålet ditt... {result.Answer}.";
            messageActivity.Text = result.Answer;
            await context.PostAsync(messageActivity);

            context.Wait(MessageRecieved);
        }
        //Handler to respond when QnAMakerResult score is a maximum of 80
        [QnAMakerResponseHandler(80)]
        public async Task HighScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            //messageActivity.Text = $"Ganske sikker på at dette svaret vil hjelpe... {result.Answer}.";
            messageActivity.Text = result.Answer;
            await context.PostAsync(messageActivity);

            context.Wait(MessageRecieved);
        }
        //Handler to respond when QnAMakerResult score is a maximum of 50
        [QnAMakerResponseHandler(50)]
        public async Task LowScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            messageActivity.Text = $"Usikker på spørsmålet, fant et svar som kanskje kan hjelpe... {result.Answer}.";
           // messageActivity.Text = result.Answer;
            await context.PostAsync(messageActivity);

            context.Wait(MessageRecieved);
        }

        //Handler to respond when QnAMakerResult score is a maximum of 20
        [QnAMakerResponseHandler(20)]
        public async Task veryLowScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result)
        {
            var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
            messageActivity.Text = $"Er veldig usikker på spørsmålet, er dette hjelpsomt?... {result.Answer}.";
            //messageActivity.Text = result.Answer;
            await context.PostAsync(messageActivity);

            context.Wait(MessageRecieved);
        }

    }
}