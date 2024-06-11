using HlidacStatu.MLUtil.Splitter;
using Microsoft.AspNetCore.SignalR;
using NPOI.SS.Formula.Functions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.SignalR
{
    public class OllamaSignalRHub : Hub
    {
        // Method to send data to
        public async Task AskLLM(string instruction, string smlouvaId, string prilohaId, string pocetbodu)
        {
            switch (instruction.ToLower())
            {
                case "summary":
                    await Summary(smlouvaId, prilohaId, pocetbodu);
                    break;
                case "summaryjson":
                    await SummaryJson(smlouvaId, prilohaId, pocetbodu);
                    break;
                default:
                    break;
            }
        }

        public async Task SummaryJson( string smlouvaId, string prilohaId, string pocetbodu)
        {

            string content = "";
            var s = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: true);
            if (s != null)
            {
                var priloha = s.Prilohy.FirstOrDefault();// (m=>m.UniqueHash() == prilohaId);
                if (priloha != null)
                {
                    var smlSplit = SplitSmlouva.Create(s.Id, priloha.UniqueHash(), priloha.PlainTextContent);
                    var smlTxt = smlSplit.ToText();
                    var ptW = Devmasters.TextUtil.CountWords(priloha.PlainTextContent);
                    var smlptW = Devmasters.TextUtil.CountWords(smlTxt);

                    content = smlTxt;
                    var debugMe = "";
                }
            }


            Uri ollamaUri = new Uri("http://10.10.100.113:18080/ollama/api");
            HlidacStatu.AI.LLM.PrivateLLM llm = new(new HlidacStatu.AI.LLM.PrivateLLM.OllamaOpenWebUI(ollamaUri.AbsoluteUri, "sk-f53cf4b315c144528110af15cc315c11"));


            System.Collections.Generic.List<AI.LLM.PrivateLLM.SumarizaceJSON.Item> llmRes = await llm.SummarizeToJsonAsync(
                HlidacStatu.AI.LLM.PrivateLLM.Profiles.AYA_Pravnik, int.Parse(pocetbodu), content, 1024 * 4);

            await Clients.All.SendAsync("ReceiveJsonSummaryMessage", llmRes.ToArray());

        }



        public async Task Summary(string smlouvaId, string prilohaId, string pocetbodu)
        {

            string content = "";
            var s = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: true);
            if (s != null)
            {
                var priloha = s.Prilohy.FirstOrDefault();// (m=>m.UniqueHash() == prilohaId);
                if (priloha != null)
                {
                    var smlSplit = SplitSmlouva.Create(s.Id, priloha.UniqueHash(), priloha.PlainTextContent);
                    var smlTxt = smlSplit.ToText();
                    var ptW = Devmasters.TextUtil.CountWords(priloha.PlainTextContent);
                    var smlptW = Devmasters.TextUtil.CountWords(smlTxt);

                    content = smlTxt;
                    var debugMe = "";
                }
            }


            Uri ollamaUri = new Uri("http://10.10.100.113:18080/ollama/api");
            HlidacStatu.AI.LLM.PrivateLLM llm = new(new HlidacStatu.AI.LLM.PrivateLLM.OllamaOpenWebUI(ollamaUri.AbsoluteUri, "sk-f53cf4b315c144528110af15cc315c11"));


            var llmRes = await llm.SummarizeStreamedAsync(
                async t => {
                    System.Diagnostics.Debug.WriteLine(t);
                    await Clients.All.SendAsync("ReceiveMessage", t);
                },

                HlidacStatu.AI.LLM.PrivateLLM.Profiles.AYA_Pravnik, int.Parse(pocetbodu), content, 1024 * 4);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            System.Diagnostics.Debug.WriteLine("A client connected to websocket: " + Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
            System.Diagnostics.Debug.WriteLine("A client disconnected from websocket: " + Context.ConnectionId);
        }
    }
}
