using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HlidacStatu.Util;
using Polly.Retry;
using Polly;

namespace HlidacStatu.Plugin.TransparetniUcty
{
    public class CSOB : BaseTransparentniUcetParser
    {
        public override string Name => "ČSOB";

        public CSOB(IBankovniUcet ucet) : base(ucet)
        {
        }

        // which status codes should trigger retry
        private static readonly HttpStatusCode[] HttpStatusCodesWorthRetrying =
        {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        // definition of retry policy
        private readonly RetryPolicy<HttpResponseMessage> RetryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => HttpStatusCodesWorthRetrying.Contains(r.StatusCode))
            .WaitAndRetry(new[]
            {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(30)
            });

        protected override IEnumerable<IBankovniPolozka> DoParse(DateTime? fromDate = default(DateTime?),
            DateTime? toDate = default(DateTime?))
        {
            var polozky = new List<IBankovniPolozka>();
            return polozky; //selhal jsem, je tam nějaká obfuskace, kterou nedokážu obejít


            TULogger.Info($"Zpracovavam ucet {Ucet.CisloUctu} s url {Ucet.Url}");
            string cisloUctuBezKoncovky = Ucet.CisloUctu.Split('/',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            toDate ??= DateTime.Now;
            var latestPossibleDate = DateTime.Now.AddYears(-3).AddDays(1);
            if (fromDate is null || fromDate < latestPossibleDate)
                fromDate = latestPossibleDate;



            if (string.IsNullOrWhiteSpace(cisloUctuBezKoncovky))
            {
                TULogger.Info($"Nedokazu oddelit cislo uctu od kodu banky. Cislo uctu [{Ucet.CisloUctu}]");
                return polozky;
            }

            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(3);
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:90.0) Gecko/20100101 Firefox/90.0");
            httpClient.DefaultRequestHeaders.Host = "www.csob.cz";
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "cs,sk;q=0.8,en-US;q=0.5,en;q=0.3");
            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            

            var page = 1;
            var chunk = 1000;
            bool containsNextPage = false;

            // Nejdříve je potřeba zjistit cookies
            string refererUrl =
                $"https://www.csob.cz/portal/firmy/bezne-ucty/transparentni-ucty/ucet/-/ta/{cisloUctuBezKoncovky}";

            var cookieResponse = RetryPolicy.Execute(() =>
            {
                var cookieRequest = new HttpRequestMessage(HttpMethod.Get, refererUrl);
                cookieRequest.Headers.Add("Sec-Fetch-Dest", "document");
                cookieRequest.Headers.Add("Sec-Fetch-Mode", "navigate");
                cookieRequest.Headers.Add("Sec-Fetch-Site", "none");
                cookieRequest.Headers.Add("Sec-Fetch-User", "?1");
                cookieRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                cookieRequest.Headers.Add("Upgrade-Insecure-Requests", "1");
                return httpClient.Send(cookieRequest);
            });

            if (!cookieResponse.IsSuccessStatusCode)
            {
                TULogger.Info($"Nepovedlo se nacist cookie");
                return polozky;
            }

            if (cookieResponse.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", setCookies);
            }
            
            // ale protože ten ... web je nepošle najednou, tak je zapotřebí ještě jednou
            // druhé kolo načítání cookies
            
            var cookieResponse2 = RetryPolicy.Execute(() =>
            {
                var cookieRequest = new HttpRequestMessage(HttpMethod.Get, refererUrl);
                cookieRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                cookieRequest.Headers.Add("Cache-Control", "no-cache");
                cookieRequest.Headers.Add("Pragma", "no-cache");
                cookieRequest.Headers.Add("Sec-Fetch-Dest", "document");
                cookieRequest.Headers.Add("Sec-Fetch-Mode", "navigate");
                cookieRequest.Headers.Add("Sec-Fetch-Site", "none");
                cookieRequest.Headers.Add("Sec-Fetch-User", "?1");
                cookieRequest.Headers.Add("Upgrade-Insecure-Requests", "1");
                cookieRequest.Headers.Add("Referer", refererUrl);
                return httpClient.Send(cookieRequest);
            });

            if (!cookieResponse2.IsSuccessStatusCode)
            {
                TULogger.Info($"Nepovedlo se nacist cookie2");
                return polozky;
            }

            if (cookieResponse2.Headers.TryGetValues("Set-Cookie", out var setCookies2))
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", setCookies2);
            }
            
            
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            string apiUrl =
                $"https://www.csob.cz/portal/firmy/bezne-ucty/transparentni-ucty/ucet?p_p_id=etnpwltadetail_WAR_etnpwlta&p_p_lifecycle=2&p_p_state=normal&p_p_mode=view&p_p_cacheability=cacheLevelPage&p_p_col_id=column-main&p_p_col_count=1&_etnpwltadetail_WAR_etnpwlta_ta={cisloUctuBezKoncovky}&p_p_resource_id=transactionList";
            do
            {
                // var response = RetryPolicy.Execute(() =>
                // {
                //     var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                //     var requestContent = new StringContent(
                //         $"{{\"accountList\":[{{\"accountNumberM24\":{cisloUctuBezKoncovky}}}],\"filterList\":[{{\"name\":\"AccountingDate\",\"operator\":\"ge\",\"valueList\":[\"{fromDate.Value.ToString("yyyy-MM-dd")}\"]}},{{\"name\":\"AccountingDate\",\"operator\":\"le\",\"valueList\":[\"{toDate.Value.ToString("yyyy-MM-dd")}\"]}}],\"sortList\":[{{\"name\":\"AccountingDate\",\"direction\":\"DESC\",\"order\":1}}],\"paging\":{{\"rowsPerPage\":{chunk},\"pageNumber\":{page}}}}}",
                //         Encoding.UTF8,
                //         "application/json"
                //     );
                //     request.Content = requestContent;
                //
                //     return httpClient.Send(request);
                // });

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                var requestContent = new StringContent(
                    $"{{\"accountList\":[{{\"accountNumberM24\":{cisloUctuBezKoncovky}}}],\"filterList\":[{{\"name\":\"AccountingDate\",\"operator\":\"ge\",\"valueList\":[\"{fromDate.Value.ToString("yyyy-MM-dd")}\"]}},{{\"name\":\"AccountingDate\",\"operator\":\"le\",\"valueList\":[\"{toDate.Value.ToString("yyyy-MM-dd")}\"]}}],\"sortList\":[{{\"name\":\"AccountingDate\",\"direction\":\"DESC\",\"order\":1}}],\"paging\":{{\"rowsPerPage\":{chunk},\"pageNumber\":{page}}}}}",
                    Encoding.UTF8,
                    "application/json"
                );
                request.Content = requestContent;
                request.Headers.Add("Sec-Fetch-Dest", "empty");
                request.Headers.Add("Sec-Fetch-Mode", "no-cors");
                request.Headers.Add("Sec-Fetch-Site", "same-origin");
                request.Headers.Add("Referer", refererUrl);
                request.Headers.Add("Accept", "application/json, text/plain, */*");

                var response = httpClient.Send(request);

                if (response.IsSuccessStatusCode)
                {
                    using var reader = new StreamReader(response.Content.ReadAsStream());
                    var content = reader.ReadToEnd();
                    var result = JsonSerializer.Deserialize<Root>(content);

                    if (result?.accountedTransactions?.accountedTransaction is null || result?.accountedTransactions?.accountedTransaction.Count() == 0)
                    {
                        TULogger.Info($"Ucet neobsahuje zadne polozky.");
                        break;
                    }

                    foreach (var item in result.accountedTransactions.accountedTransaction)
                    {
                        DateTime datumPlatby;
                        try
                        {
                            datumPlatby = new DateTime(item.baseInfo.accountingDate.year,
                                item.baseInfo.accountingDate.monthValue, 
                                item.baseInfo.accountingDate.dayOfMonth);
                        }
                        catch (Exception e)
                        {
                            TULogger.Info($"Nedokazu spravne rozparsovat datum u {item.baseInfo.bankReference}. Exception = {e.Message}");
                            continue;
                        }
                        
                        polozky.Add(new SimpleBankovniPolozka
                        {
                            AddId = item?.baseInfo?.bankReference,
                            CisloUctu = Ucet.CisloUctu,
                            Castka = item?.baseInfo?.accountAmountData?.amount ?? 0,
                            Datum = datumPlatby,
                            VS = item?.transactionTypeChoice?.domesticPayment?.symbols?.variableSymbol,
                            KS = item?.transactionTypeChoice?.domesticPayment?.symbols?.constantSymbol,
                            SS = item?.transactionTypeChoice?.domesticPayment?.symbols?.specificSymbol,
                            NazevProtiuctu = item?.transactionTypeChoice?.domesticPayment?.partyName,
                            PopisTransakce = item?.baseInfo?.transactionDescription,
                            ZpravaProPrijemce = item?.transactionTypeChoice?.domesticPayment?.message?.message1
                                + item?.transactionTypeChoice?.domesticPayment?.message?.message2
                                + item?.transactionTypeChoice?.domesticPayment?.message?.message3
                                + item?.transactionTypeChoice?.domesticPayment?.message?.message4,
                            ZdrojUrl = refererUrl,
                            CisloProtiuctu = item?.transactionTypeChoice?.domesticPayment?.partyAccount?.domesticAccount?.accountNumber +
                                             "/" + 
                                             item?.transactionTypeChoice?.domesticPayment?.partyAccount?.domesticAccount?.bankCode
                        });
                    }

                    containsNextPage = result.accountedTransactions.paging.pageNumber <
                                       result.accountedTransactions.paging.pageCount;
                }
                else
                {
                    TULogger.Info($"Chyba {response.StatusCode}");
                    break;
                }

                page++;
            } while (containsNextPage);


            return polozky;
        }


        public class Paging
        {
            public int pageCount { get; set; }
            public int pageNumber { get; set; }
            public int recordCount { get; set; }
        }

        public class AccountAmountData
        {
            public decimal amount { get; set; }
            public string currencyCode { get; set; }
        }

        public class Chronology
        {
            public string calendarType { get; set; }
            public string id { get; set; }
        }

        public class AccountingDate
        {
            public int dayOfYear { get; set; }
            public string dayOfWeek { get; set; }
            public string month { get; set; }
            public int dayOfMonth { get; set; }
            public int year { get; set; }
            public int monthValue { get; set; }
            public int nano { get; set; }
            public int hour { get; set; }
            public int minute { get; set; }
            public int second { get; set; }
            public Chronology chronology { get; set; }
        }

        public class BaseInfo
        {
            public AccountAmountData accountAmountData { get; set; }
            public decimal accountBalance { get; set; }
            public int? paymentTypeId { get; set; }
            public AccountingDate accountingDate { get; set; }
            public object accountingOrder { get; set; }
            public string idTran { get; set; }
            public int? idZp { get; set; }
            public string accountingTypeId { get; set; }
            public string accountingTypeCode { get; set; }
            public string ccTypeCode { get; set; }
            public string bankReference { get; set; }
            public string transactionTypeCode { get; set; }
            public string transactionDescription { get; set; }
            public string transactionNote { get; set; }
            public object typeOfBreakdownCode { get; set; }
            public string transactionGroupCode { get; set; }
            public object generalTextList { get; set; }
        }

        public class DebitingDate
        {
            public int dayOfYear { get; set; }
            public string dayOfWeek { get; set; }
            public string month { get; set; }
            public int dayOfMonth { get; set; }
            public int year { get; set; }
            public int monthValue { get; set; }
            public int nano { get; set; }
            public int hour { get; set; }
            public int minute { get; set; }
            public int second { get; set; }
            public Chronology chronology { get; set; }
        }

        public class DomesticAccount
        {
            public long accountNumber { get; set; }
            public string bankCode { get; set; }
        }

        public class PartyAccount
        {
            public object internalAccount { get; set; }
            public DomesticAccount domesticAccount { get; set; }
            public object foreignAccount { get; set; }
        }

        public class Symbols
        {
            public string variableSymbol { get; set; }
            public string specificSymbol { get; set; }
            public string constantSymbol { get; set; }
        }

        public class Message
        {
            public string message1 { get; set; }
            public string message2 { get; set; }
            public string message3 { get; set; }
            public string message4 { get; set; }
        }

        public class DomesticPayment
        {
            public DebitingDate debitingDate { get; set; }
            public object paymentAmountData { get; set; }
            public object exchangeRate { get; set; }
            public PartyAccount partyAccount { get; set; }
            public string partyName { get; set; }
            public Symbols symbols { get; set; }
            public Message message { get; set; }
            public object batchName { get; set; }
            public object transactionSystemComment { get; set; }
        }

        public class TransactionTypeChoice
        {
            public DomesticPayment domesticPayment { get; set; }
            public object crossborderPayment { get; set; }
            public object sepaCreditTransfer { get; set; }
            public object tbca { get; set; }
            public object fee { get; set; }
            public object externalAccountTransaction { get; set; }
            public object otherTransaction { get; set; }
        }

        public class AccountedTransaction
        {
            public BaseInfo baseInfo { get; set; }
            public TransactionTypeChoice transactionTypeChoice { get; set; }
        }

        public class AccountedTransactions
        {
            public Paging paging { get; set; }
            public List<AccountedTransaction> accountedTransaction { get; set; }
        }

        public class Root
        {
            public AccountedTransactions accountedTransactions { get; set; }
        }


        // private static readonly Regex StatementItemPattern = new Regex("^\\d{2}\\.\\d{2}\\. ", RegexOptions.Compiled);
        // private static readonly Regex AccountNumberPattern = new Regex(@"\d*\-?\d+/\d{4}", RegexOptions.Compiled);
        // private static readonly Regex ItemsPattern = new Regex(@" *(\d+) *", RegexOptions.Compiled);
        // private const string OpeningBalanceText = "Počáteční zůstatek:";
        // private const string FinalBalanceText = "Konečný zůstatek:";
        // private readonly PositionIndex CreditItemsPosition = new PositionIndex(25, 40);
        // private readonly PositionIndex DebitItemsPosition = new PositionIndex(25, 40);
        // private List<IBankovniPolozka> ParseStatement(string textFile, DateTime date, string sourceUrl)
        // {
        // 	var statementItems = new List<IBankovniPolozka>();
        // 	var data = File.ReadAllLines(textFile);
        // 	var item = (StatementItemRecord)null;
        // 	var overview = new StatementOverview();
        // 	var positions = new StatementItemsPositions();
        //
        // 	foreach (var line in data)
        // 	{
        // 		if (string.IsNullOrEmpty(line)) continue;
        // 		if (line.Contains("Změna úrokové sazby")) continue;
        //
        // 		if (line.StartsWith("Počet kreditních položek"))
        // 		{
        // 			overview.CreditItems = int.Parse(ItemsPattern.Match(GetValue(line, CreditItemsPosition)).Groups[1].Value);
        // 			if (!line.Contains(OpeningBalanceText)) continue;
        // 		}
        // 		if (line.StartsWith("Počet debetních položek"))
        // 		{
        // 			overview.DebitItems = int.Parse(ItemsPattern.Match(GetValue(line, DebitItemsPosition)).Groups[1].Value);
        // 			continue;
        // 		}
        // 		if (line.Contains(OpeningBalanceText))
        // 		{
        // 			overview.OpeningBalance = ParseTools.FromTextToDecimal(GetValue(line, new PositionIndex(line.IndexOf(OpeningBalanceText, SC) + OpeningBalanceText.Length + 1, default(int?)))) ?? 0;
        // 			continue;
        // 		}
        // 		if (line.Trim().StartsWith(FinalBalanceText))
        // 		{
        // 			overview.FinalBalance = ParseTools.FromTextToDecimal(GetValue(line, new PositionIndex(line.IndexOf(FinalBalanceText, SC) + FinalBalanceText.Length + 1, default(int?)))) ?? 0;
        // 			continue;
        // 		}
        //
        // 		if (line.StartsWith("Datum"))
        // 		{
        // 			DefineItemsPositionsForFirstLine(positions, line);
        // 			continue;
        // 		}
        // 		if (line.StartsWith("Valuta"))
        // 		{
        // 			DefineItemsPositionsForSecondLine(positions, line);
        // 			continue;
        // 		}
        //
        // 		if (item != null && (line[0] != ' ' || line.Trim().StartsWith("Převádí se") || line.Trim().StartsWith("Konec výpisu")))
        // 		{
        // 			statementItems.Add(item.ToBankPolozka(date, Ucet, sourceUrl));
        // 			item = null;
        // 		}
        //
        // 		if (StatementItemPattern.IsMatch(line))
        // 		{
        // 			item = CreateStatementItem(line, positions);
        // 		}
        // 		else if (item != null && string.IsNullOrEmpty(item.ZpravaProPrijemce) && AccountNumberPattern.IsMatch(line))
        // 		{
        // 			UpdateStatementItem(item, line, positions);
        // 		}
        // 		else if (item != null)
        // 		{
        // 			item.ZpravaProPrijemce += (string.IsNullOrEmpty(item.ZpravaProPrijemce)
        // 										  ? string.Empty
        // 										  : Environment.NewLine) + line.Trim();
        // 		}
        // 	}
        //
        // 	ValidateParsedItems(sourceUrl, overview, statementItems);
        //
        // 	return statementItems;
        // }
        //
        // private static string DownloadStatement(string statementUrl, string root)
        // {
        // 	var pdfTmpFile = Path.Combine(root, "statement.pdf");
        // 	using (var net = new Devmasters.Net.HttpClient.URLContent(statementUrl))
        // 	{
        // 		File.WriteAllBytes(pdfTmpFile, net.GetBinary().Binary);
        // 	}
        // 	return pdfTmpFile;
        // }
        //
        // private string ConvertStatementToText(string pdfTmpFile, string root)
        // {
        // 	var txtTmpFile = Path.Combine(root, "statement.txt");
        // 	var psi = new System.Diagnostics.ProcessStartInfo(pdf2txt[0],
        // 		string.Format(pdf2txt[1], pdfTmpFile, txtTmpFile));
        // 	var pe = new Devmasters.ProcessExecutor(psi, 60);
        // 	pe.Start();
        // 	return txtTmpFile;
        // }
        //
        // private Dictionary<string, DateTime> GetBankStatementLinks()
        // {
        // 	using (var url = new Devmasters.Net.HttpClient.URLContent(Ucet.Url))
        // 	{
        // 		var doc = new Devmasters.XPath(url.GetContent().Text);
        // 		return doc.GetNodes(
        // 					   "//div[@class='npw-transaction-group']/ul[@class='npw-documents']//a[text()[contains(.,'Transakce')]]")
        // 				   ?.Select(n => new
        // 				   {
        // 					   url = "https://www.csob.cz" + n.Attributes["href"].Value,
        // 					   month = "01-" + n.InnerText.Replace("Transakce ", "").Replace("/", "-").Trim()
        // 				   }
        // 				   )
        // 				   ?.ToDictionary(k => k.url,
        // 					   v => DateTime.ParseExact(v.month, "dd-MM-yyyy", Consts.czCulture))
        // 			   ?? new Dictionary<string, DateTime>();
        // 		;
        // 	}
        // }
        //
        // private void DefineItemsPositionsForFirstLine(StatementItemsPositions positions, string line)
        // {
        // 	if (line.IndexOf("Identifikace", SC) > 0) positions.AddId = new PositionIndex(line.IndexOf("Identifikace", SC), 14);
        // 	else if (line.IndexOf("Reference banky", SC) > 0) positions.AddId = new PositionIndex(line.IndexOf("Reference banky", SC), 20);
        // 	else if (line.IndexOf("Sekv.", SC) > 0) positions.AddId = new PositionIndex(line.IndexOf("Sekv.", SC), 5);
        // 	var protiucetPosition = line.IndexOf("Název protiúčtu", SC);
        // 	positions.NazevProtiuctu = new PositionIndex(protiucetPosition, 25);
        // 	var popisPosition = Math.Max(line.IndexOf("Označení platby", SC), line.IndexOf("Označení operace", SC));
        // 	positions.PopisTransakce = new PositionIndex(popisPosition, protiucetPosition - popisPosition - 1);
        // 	positions.Castka = new PositionIndex(line.IndexOf("Částka", SC), 15);
        // 	positions.KS = new PositionIndex(line.IndexOf("KS", SC), 4);
        // }
        //
        // private void DefineItemsPositionsForSecondLine(StatementItemsPositions positions, string line)
        // {
        // 	positions.CisloProtiuctu = new PositionIndex(line.IndexOf("Protiúčet nebo poznámka", SC), 50);
        // 	positions.SS = new PositionIndex(line.IndexOf("SS", SC), default(int?));
        // 	if (line.IndexOf("KS", SC) > 0)
        // 	{
        // 		positions.KS = new PositionIndex(line.IndexOf("KS", SC), 4);
        // 	}
        // 	var vsPosition = line.IndexOf("VS", SC);
        // 	positions.VS = new PositionIndex(vsPosition, positions.KS.Start - vsPosition - 1);
        // }
        // private StatementItemRecord CreateStatementItem(string line, StatementItemsPositions positions)
        // {
        // 	return new StatementItemRecord
        // 	{
        // 		AddId = GetValue(line, positions.AddId).Trim(),
        // 		Datum = GetValue(line, positions.Datum).Trim(),
        // 		PopisTransakce = GetValue(line, positions.PopisTransakce).Trim(),
        // 		NazevProtiuctu = GetValue(line, positions.NazevProtiuctu).Trim(),
        // 		Castka = ParseAmount(line, positions)
        // 	};
        // }
        //
        // private decimal ParseAmount(string line, StatementItemsPositions positions)
        // {
        // 	var amount = GetValue(line, positions.Castka).Trim();
        // 	return ParseTools.FromTextToDecimal(amount.Trim())
        // 		   ?? throw new ApplicationException(
        // 			   $"Amount on line '{line}' on position {positions.Castka} could not be read ({amount})");
        // }
        //
        // private void UpdateStatementItem(StatementItemRecord item, string line, StatementItemsPositions positions)
        // {
        // 	item.CisloProtiuctu = GetValue(line, positions.CisloProtiuctu).Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).First();
        // 	item.VS = GetValue(line, positions.VS).Trim();
        // 	item.KS = GetValue(line, positions.KS).Trim();
        // 	ParseSS(item, line, positions);
        // 	item.ZpravaProPrijemce = string.Empty;
        // }
        //
        // private void ParseSS(StatementItemRecord item, string line, StatementItemsPositions positions)
        // {
        // 	var ss = GetValue(line, positions.SS);
        // 	if (!string.IsNullOrEmpty(ss) && !ss.StartsWith(" "))
        // 	{
        // 		item.SS = ss.IndexOf(" ", SC) > 0
        // 			? ss.Substring(0, ss.IndexOf(" ", SC))
        // 			: ss;
        // 	}
        // }
        //
        // private static void ValidateParsedItems(string sourceUrl, StatementOverview overview, List<IBankovniPolozka> statementItems)
        // {
        // 	var currentCreditItems = statementItems.Count(i => i.Castka > 0);
        // 	if (overview.CreditItems != currentCreditItems)
        // 	{
        // 		throw new ApplicationException(
        // 			$"Invalid count of credit items (expected {overview.CreditItems}, found {currentCreditItems}) - {sourceUrl}");
        // 	}
        // 	var currentDebitItems = statementItems.Count(i => i.Castka < 0);
        // 	if (overview.DebitItems != currentDebitItems)
        // 	{
        // 		throw new ApplicationException(
        // 			$"Invalid count of debit items (expected {overview.DebitItems}, found {currentDebitItems}) - {sourceUrl}");
        // 	}
        // 	var currentFinalBalance = overview.OpeningBalance + statementItems.Sum(i => i.Castka);
        // 	if (overview.FinalBalance != currentFinalBalance)
        // 	{
        // 		throw new ApplicationException(
        // 			$"Invalid final balance (expected {overview.FinalBalance}, found {currentFinalBalance}) - {sourceUrl}");
        // 	}
        // }
        //
        // private const int DefaultLength = 100;
        // private static string GetValue(string line, PositionIndex pos)
        // {
        // 	if (line.Length <= pos.Start || pos.Start < 0 || pos.Length <= 0)
        // 	{
        // 		return string.Empty;
        // 	}
        // 	else if (line.Length <= pos.Start + (pos.Length ?? DefaultLength))
        // 	{
        // 		return line.Substring(pos.Start);
        // 	}
        // 	return line.Substring(pos.Start, pos.Length ?? DefaultLength);
        // }
        //
        // private class StatementItemRecord
        // {
        // 	public string Datum { get; set; }
        // 	public string PopisTransakce { get; set; }
        // 	public string NazevProtiuctu { get; set; }
        // 	public string CisloProtiuctu { get; set; }
        // 	public string ZpravaProPrijemce { get; set; }
        // 	public string VS { get; set; }
        // 	public string KS { get; set; }
        // 	public string SS { get; set; }
        // 	public decimal Castka { get; set; }
        // 	public string AddId { get; set; }
        // 	public IBankovniPolozka ToBankPolozka(DateTime month, IBankovniUcet ucet, string zdroj)
        // 	{
        // 		var datum = Datum.Split('.'); // 25.04.
        // 		return new SimpleBankovniPolozka
        // 		{
        // 			AddId = Devmasters.TextUtil.NormalizeToBlockText(AddId),
        // 			Castka = Castka,
        // 			CisloProtiuctu = Devmasters.TextUtil.NormalizeToBlockText(CisloProtiuctu),
        // 			CisloUctu = Devmasters.TextUtil.NormalizeToBlockText(ucet.CisloUctu),
        // 			KS = Devmasters.TextUtil.NormalizeToBlockText(KS),
        // 			NazevProtiuctu = Devmasters.TextUtil.NormalizeToBlockText(NazevProtiuctu),
        // 			PopisTransakce = Devmasters.TextUtil.NormalizeToBlockText(PopisTransakce),
        // 			SS = Devmasters.TextUtil.NormalizeToBlockText(SS),
        // 			VS = Devmasters.TextUtil.NormalizeToBlockText(VS),
        // 			ZpravaProPrijemce = Devmasters.TextUtil.NormalizeToBlockText(ZpravaProPrijemce),
        // 			ZdrojUrl = zdroj,
        // 			Datum = new DateTime(month.Year, Convert.ToInt32(datum[1]), Convert.ToInt32(datum[0]))
        // 		};
        // 	}
        // }
        //
        // private class PositionIndex
        // {
        // 	public PositionIndex(int start, int? length)
        // 	{
        // 		Start = start;
        // 		Length = length;
        // 	}
        //
        // 	public int Start { get; private set; }
        // 	public int? Length { get; private set; }
        //
        // 	public override string ToString()
        // 	{
        // 		return Length.HasValue
        // 			? $"{Start} ({Length.Value})"
        // 			: Start.ToString();
        // 	}
        // }
        //
        // private class StatementItemsPositions
        // {
        // 	public StatementItemsPositions()
        // 	{
        // 		Datum = new PositionIndex(0, 6);
        // 	}
        //
        // 	public PositionIndex AddId { get; set; }
        // 	public PositionIndex Datum { get; set; }
        // 	public PositionIndex PopisTransakce { get; set; }
        // 	public PositionIndex NazevProtiuctu { get; set; }
        // 	public PositionIndex Castka { get; set; }
        // 	public PositionIndex CisloProtiuctu { get; set; }
        // 	public PositionIndex VS { get; set; }
        // 	public PositionIndex KS { get; set; }
        // 	public PositionIndex SS { get; set; }
        // }
        //
        // private class StatementOverview
        // {
        // 	public int CreditItems { get; set; }
        // 	public int DebitItems { get; set; }
        // 	public decimal OpeningBalance { get; set; }
        // 	public decimal FinalBalance { get; set; }
        //
        // }
    }
}