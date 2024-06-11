using HlidacStatu.MLUtil.Splitter;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.SignalR
{
    public class OllamaSignalRHub : Hub
    {
        // Method to send data to
        public async Task AskLLM(string smlouvaId, string numPriloha)
        {
            string content = "";


            int poradiPriloha = Devmasters.ParseText.ToInt(numPriloha, 1).Value-1;
            var s = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: true);
            if (s != null)
            {
                var priloha = s.Prilohy.Skip(poradiPriloha).FirstOrDefault();
                if (priloha != null)
                {
                    var smlSplit = SplitSmlouva.Create(s.Id, priloha.UniqueHash(), priloha.PlainTextContent);
                    var smlTxt = smlSplit.ToText();
                    var ptW = Devmasters.TextUtil.CountWords(priloha.PlainTextContent);
                    var smlptW = Devmasters.TextUtil.CountWords(smlTxt);


                    var debugMe = "";
                }
            }


            Uri ollamaUri = new Uri("http://10.10.100.113:18080/ollama/api");
            HlidacStatu.AI.LLM.PrivateLLM llm = new(new HlidacStatu.AI.LLM.PrivateLLM.OllamaOpenWebUI(ollamaUri.AbsoluteUri, "sk-f53cf4b315c144528110af15cc315c11"));


            var llmRes = await llm.JustQueryStreamedAsync(
                async t => {
                    System.Diagnostics.Debug.WriteLine(t);
                    await Clients.All.SendAsync("ReceiveMessage", t);
                    },

                HlidacStatu.AI.LLM.PrivateLLM.Profiles.AYA_Pravnik,
                "Je provozovatel oprávněn tyto licence vypovědět?", content);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            System.Diagnostics.Debug.WriteLine("A client connected to MyChatHub: " + Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
            System.Diagnostics.Debug.WriteLine("A client disconnected from MyChatHub: " + Context.ConnectionId);
        }
    }
}
