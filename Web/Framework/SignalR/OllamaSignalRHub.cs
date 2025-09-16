using Devmasters;
using HlidacStatu.MLUtil.Splitter;
using Microsoft.AspNetCore.SignalR;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
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
            Serilog.Log.ForContext<OllamaSignalRHub>().Warning($"Calling {nameof(SummaryJson)} method from {nameof(OllamaSignalRHub)}, should this call be repeated frequently, it might have serious impact on Web performance! The reason is loading PlainTextContent.");
            
            Uri ollamaUri = new Uri("http://10.10.100.113:18080/ollama/api");
            var ollamaCl = new HlidacStatu.AI.LLM.Clients.OllamaServerClient<HlidacStatu.AI.LLM.Clients.Options.SummaryOptions>(ollamaUri.AbsoluteUri);
            HlidacStatu.AI.LLM.Summarization llm = new HlidacStatu.AI.LLM.Summarization(ollamaCl);

            //HlidacStatu.AI.LLM.Summarization llm = new(new HlidacStatu.AI.LLM.Summarization.OllamaOpenWebUI(ollamaUri.AbsoluteUri, "sk-f53cf4b315c144528110af15cc315c11"));


            string content = "";
            var s = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(smlouvaId, includePlaintext: true);
            if (s != null)
            {
                List<HlidacStatu.Entities.AI.SumarizaceJSON.Item> summ = new List<HlidacStatu.Entities.AI.SumarizaceJSON.Item>();
                var priloha = s.Prilohy.FirstOrDefault();// (m=>m.UniqueHash() == prilohaId);
                if (priloha != null)
                {
                    var smlSplit = SplitSmlouva.Create(s.Id, priloha.UniqueHash(), priloha.PlainTextContent);


                    Console.Write("AI progress ");
                    decimal count = 0;
                    decimal total = smlSplit.Sections.Count;
                    string message = "Analýza smlouvy";
                    foreach (var sect in smlSplit.Sections)
                    {
                        count++;
                        await Clients.All.SendAsync("ReceiveProgress", new { progress = (count) / total, message = message });

                        string t = sect.ToText();
                        long tokens = HlidacStatu.AI.LLM.Util.SimpleTokenize(t).LongCount();
                        //Console.Write($"({tokens}).");

                        int pocetOdrazek = 2;
                        if (tokens > 4000)
                            pocetOdrazek = (int)((tokens / 4000) + 2);
                        if (tokens < 500)
                            pocetOdrazek = 1;

                        var sectSumm = await llm.SummarizeToJsonAsync(t,pocetOdrazek, 
                            AI.LLM.Models.Model.Default, AI.LLM.LLMQuery.Languages.Czech);
                        if (sectSumm?.sumarizace.Count() > 0)
                        {
                            message = sectSumm.sumarizace.First().titulek;
                            summ.AddRange(sectSumm.sumarizace);
                        }

                    }
                    await Clients.All.SendAsync("ReceiveProgress", new { progress = 1, message = "Zobrazujeme...." });

                }
                await Clients.All.SendAsync("ReceiveJsonSummaryMessage", summ.ToArray());
            }
            else
                await Clients.All.SendAsync("ReceiveJsonSummaryMessage", null);
            
        }



        public async Task Summary(string smlouvaId, string prilohaId, string pocetbodu)
        {
            Serilog.Log.ForContext<OllamaSignalRHub>().Warning($"Calling {nameof(Summary)} method from {nameof(OllamaSignalRHub)}, should this call be repeated frequently, it might have serious impact on Web performance! The reason is loading PlainTextContent.");

            string content = "";
            var s = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(smlouvaId, includePlaintext: true);
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
            var ollamaCl = new HlidacStatu.AI.LLM.Clients.OllamaServerClient<HlidacStatu.AI.LLM.Clients.Options.SummaryOptions>(ollamaUri.AbsoluteUri);
            HlidacStatu.AI.LLM.Summarization llm = new HlidacStatu.AI.LLM.Summarization(ollamaCl);
            //HlidacStatu.AI.LLM.Summarization llm = new(new HlidacStatu.AI.LLM.Summarization.OllamaOpenWebUI(ollamaUri.AbsoluteUri, "sk-f53cf4b315c144528110af15cc315c11"));


            var llmRes = await llm.SummarizeStreamedAsync(content,int.Parse(pocetbodu),
                AI.LLM.Models.Model.Default, AI.LLM.LLMQuery.Languages.Czech,
                async t => {
                    System.Diagnostics.Debug.WriteLine(t);
                    await Clients.All.SendAsync("ReceiveMessage", t);
                });
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
