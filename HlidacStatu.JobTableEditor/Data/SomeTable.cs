using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;

namespace HlidacStatu.JobTableEditor.Data
{
    public class SomeTable
    {
        public InDocTables InDocTable { get; set; }
        public CellShell[][] Cells { get; set; }
        public TimeSpan ProcessingTime { get; private set; }
        public string Author { get; set; }

        public List<InDocJobs> FoundJobs { get; private set; }


        private DateTime _tableOpenedAt;

        public Func<Task> OnSave { get; set; }

        public async Task Save()
        {
            if (OnSave != null)
                await OnSave();
        }

        public void StartWork()
        {
            _tableOpenedAt = DateTime.Now;
        }

        public void EndWork()
        {
            ProcessingTime = DateTime.Now - _tableOpenedAt;
        }

        //todo: refactor this and underlaying code
        public void ParseJobs()
        {
            //todo: detekce jednotky
            
            var jobsList = new List<InDocJobs>();
            List<int> priceCols = new List<int>();
            List<int> priceWithVATCols = new List<int>();
            List<int> priceWithoutVATCols = new List<int>();
            
            for (int rowNum = 0; rowNum < Cells.Length; rowNum++)
            {
                // find values of interest
                var foundValues = new List<Identifier.CellIdentifier>();
                for (int colNum = 0; colNum < Cells[rowNum].Length; colNum++)
                {
                    var cell = Cells[rowNum][colNum];
                    if (cell.IsImportant)
                    {
                        //prepare for next processing
                        foundValues.Add(
                            new Identifier.CellIdentifier(cell,
                                priceCols.Contains(colNum),
                                priceWithVATCols.Contains(colNum),
                                priceWithoutVATCols.Contains(colNum)));
                    }
                    else
                    {
                        //check if column contains key words
                        string reducedString = cell.Value.ReduceText();
                        if (Identifier.WithDphRegex.IsMatch(reducedString))
                        {
                            priceWithVATCols.Add(colNum);
                        }
                        else if (Identifier.WithoutDphRegex.IsMatch(reducedString))
                        {
                            priceWithoutVATCols.Add(colNum);
                        }
                        else if (Identifier.CurrencyRegex.IsMatch(reducedString))
                        {
                            priceCols.Add(colNum);
                        }
                        
                    }
                }
                //identify values
                if (!foundValues.Any())
                    continue;
                
                var job = new InDocJobs();

                var textCells = foundValues.Where(v => v.IsTextCell).OrderByDescending(v => v.LetterCount);
                var numberCells = foundValues.Where(v => v.IsNumberCell).OrderByDescending(v => v.DecimalValue).ToList();

                // jako job nastaví textovou buňku s nejdelším textem
                job.JobRaw = textCells.FirstOrDefault()?.Cell.Value;

                // Nastaví ceny
                if (numberCells.Count() > 1)
                {
                    // daň vychází matematicky
                    var highestPrice = numberCells[0].DecimalValue;
                    var secondHighestPrice = numberCells[1].DecimalValue;
                    // check if second highest price is in the VAT range (with some tolerance)
                    if (secondHighestPrice * 1.23m > highestPrice && secondHighestPrice * 1.1m < highestPrice)
                    {
                        job.Price = secondHighestPrice;
                        job.PriceVAT = highestPrice;
                    }
                    
                }
                if (numberCells.Any() && !job.Price.HasValue)
                {
                    var firstCell = numberCells[0];
                    // daň vychází kontextuálně
                    if (firstCell.IsVatPrice) 
                    {
                        job.PriceVAT = firstCell.DecimalValue;
                    }
                    // daň nenalezena
                    else
                    {
                        job.Price = firstCell.DecimalValue;
                    }
                }

                job.UnitCount = 1; // default value for unit count (zatím nevím, jak bych tohle číslo parsoval)
                
                jobsList.Add(job);

            }

            FoundJobs = jobsList;
        }

        
    }

    public static class Identifier
    {
        public static Regex WithDphRegex = new Regex(@"(s|vc|vcetne)[\. ]{0,2}(dph)", RegexOptions.Compiled);
        public static Regex WithoutDphRegex = new Regex(@"(bez)[\. ]{0,2}(dph)", RegexOptions.Compiled);
        public static Regex CurrencyRegex = new Regex(@"(kc|eur|korun|czk)", RegexOptions.Compiled);

        public static string ReduceText(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";
            return input.ToAlphaNumeric().Trim().RemoveAccents().ToLowerInvariant();
        }
        
        private static string RemoveAccents(this string input)
        {
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
            
        private static string ToAlphaNumeric(this string text)
        {
            var allowedChars = " .,".ToCharArray();
            return new string(Array.FindAll(
                text.ToCharArray(),
                c => char.IsLetterOrDigit(c) || allowedChars.Contains(c)));
        }
        
        private static string RemoveWordsNearNumbers(this string text)
        {
            text = WithDphRegex.Replace(text, "");
            text = WithoutDphRegex.Replace(text, "");
            text = CurrencyRegex.Replace(text, "");
            return text;
        }
        
        
        
        public class CellIdentifier
        {
            public CellShell Cell { get; set; }
            public int DigitCount { get; set; }
            public int LetterCount { get; set; }
            public decimal? DecimalValue { get; set; }
            public string TextValue { get; set; }
            public bool IsInPriceColumn { get; set; }
            public bool IsInWithVatColumn { get; set; }
            public bool IsInWithoutVatColumn { get; set; }
            
            private bool ContainsVatSubstring { get; set; }
            private bool ContainsWithoutVatSubstring { get; set; }
            private bool ContainsCurrencySubstring { get; set; }

            public CellIdentifier(CellShell cellShell, bool isInPriceColumn, bool isInWithVatColumn, bool isInWithoutVatColumn)
            {
                Cell = cellShell;
                string cleanText = cellShell.Value.ReduceText();

                IsInPriceColumn = isInPriceColumn;
                IsInWithVatColumn = isInWithVatColumn;
                IsInWithoutVatColumn = isInWithoutVatColumn;

                ContainsVatSubstring = WithDphRegex.IsMatch(cleanText);
                ContainsWithoutVatSubstring = WithoutDphRegex.IsMatch(cleanText);
                ContainsCurrencySubstring = CurrencyRegex.IsMatch(cleanText);

                cleanText = cleanText.RemoveWordsNearNumbers();
                
                foreach (var letter in cleanText)
                {
                    if(char.IsDigit(letter))
                        DigitCount++;
                    else if(char.IsLetter(letter))
                        LetterCount++;
                }

                DecimalValue = Devmasters.ParseText.ToDecimal(cleanText);
                TextValue = cleanText;

            }
            
            public bool IsTextCell => LetterCount > DigitCount;
            public bool IsNumberCell => DigitCount > LetterCount;

            public bool IsVatPrice => IsInWithVatColumn || ContainsVatSubstring;


        }
    }
}