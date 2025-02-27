﻿using Devmasters.Collections;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Enhancers;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HlidacStatu.Plugin.Enhancers
{
    public partial class TextMiner : IEnhancer
    {
        public int Priority => 1;

        public static int MaxIrisQueueLenght = 50;
        bool asyncOCR = false;
        bool skipOCR = false;
        bool forceAlreadyMined = false;
        int priority = 5;
        private bool forceOCR;
        int? lengthLess = null;

        public bool missingOnly = true;

        public enum OCREngines
        {
            IrisOnly,
            GhostscriptOnly,
            All,
        }

        string pathToOcr = Devmasters.Config.GetWebConfigValue("ReadIrisMonitorPath");
        public TextMiner()
        {
            if (!pathToOcr.EndsWith(Path.DirectorySeparatorChar))
                pathToOcr += Path.DirectorySeparatorChar;
        }

        public TextMiner(bool skipOCR, bool forceAlreadyMined, bool asyncOCR = false, int priority = 5, 
            bool forceOCR = false, int? lengthLess = null, bool missingOnly = true)
            : this()
        {
            this.skipOCR = skipOCR;
            this.forceAlreadyMined = forceAlreadyMined;
            this.asyncOCR = asyncOCR;
            this.priority = priority;
            this.forceOCR = forceOCR;
            this.lengthLess = lengthLess;
            this.missingOnly = missingOnly;
        }
        public void SetInstanceData(object data)
        {
        }

        public string Description
        {
            get
            {
                return "Vytahnout texty z prilozenych smluv";
            }
        }

        public string Name
        {
            get
            {
                return "TextMiner";
            }
        }
        bool changed = false;


        DateTime history = new DateTime(2016, 1, 1);

        public bool Update(ref Smlouva item)
        {

            if (item.Prilohy != null)
            {

                List<Smlouva.Priloha> newPrilohy = new List<Smlouva.Priloha>();
                for (int i = 0; i < item.Prilohy.Count(); i++)
                {
                    bool doOcr = false;
                    var att = item.Prilohy[i];
                    if (!this.forceAlreadyMined && att.LastUpdate > history)
                        doOcr = doOcr || false;
                    else
                        doOcr = true;

                    if (!this.forceAlreadyMined && att.PlainTextContentQuality != DataQualityEnum.Unknown) //already parsed
                    {
                        //att.LastUpdate = DateTime.Now.AddDays(-7);
                        doOcr = doOcr || false;
                    }
                    else
                        doOcr = true;

                    if (this.lengthLess.HasValue && att.Lenght < this.lengthLess)
                        doOcr = true;
                    else
                        doOcr = doOcr || false;

                    if (this.missingOnly && att.Lenght<2)
                        doOcr = true;
                    else
                        doOcr = doOcr || false;

                    if (this.forceAlreadyMined)
                        doOcr = true;

                    if (doOcr == false)
                        continue;

                    Base.Logger.Debug($"Getting priloha {att.nazevSouboru} for smlouva {item.Id}");
                    string downloadedFile = SmlouvaRepo.GetCopyOfDownloadedPrilohaPath(att, item);
                    Base.Logger.Debug($"Getdone priloha {att.nazevSouboru} for smlouva {item.Id} done.");
                    if (downloadedFile != null)
                    {
                        try
                        {
                            HlidacStatu.Lib.OCR.Api.Client.MiningIntensity intensity = HlidacStatu.Lib.OCR.Api.Client.MiningIntensity.Maximum;
                            if (skipOCR)
                                intensity = HlidacStatu.Lib.OCR.Api.Client.MiningIntensity.SkipOCR;
                            if (this.forceOCR)
                                intensity = Lib.OCR.Api.Client.MiningIntensity.ForceOCR;


                            Base.Logger.Debug($"STARTING TextMiner Client.TextFromFile Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  skipOCR:{intensity.ToString()}");

                            HlidacStatu.Lib.OCR.Api.Result res = null;
                            if (asyncOCR)
                            {                                //res = HlidacStatu.Lib.OCR.Api.Client.TextFromFile(
                                //    Devmasters.Config.GetWebConfigValue("OCRServerApiKey"),
                                //    downloadedFile, "TextMiner",
                                //    HlidacStatu.Lib.OCR.Api.Client.TaskPriority.High, intensity
                                //    ); 
                                Base.Logger.Debug($"TextMiner Client.TextFromFile Adding NewTask Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  skipOCR:{intensity.ToString()}");
                                res = ItemToOcrQueue.AddNewTask(OcrWork.DocTypes.Smlouva, item.Id, priority: this.priority);
                                Base.Logger.Debug($"TextMiner Client.TextFromFile Added NewTask Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  skipOCR:{intensity.ToString()}");
                            }
                            else
                            {
                                Base.Logger.Debug($"TextMiner Client.TextFromFile Doing OCR Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  skipOCR:{intensity.ToString()}");
                                res = HlidacStatu.Lib.OCR.Api.Client.TextFromFile(
                                    Devmasters.Config.GetWebConfigValue("OCRServerApiKey"),
                                    downloadedFile, "TextMiner",
                                    HlidacStatu.Lib.OCR.Api.Client.TaskPriority.High, intensity);
                                Base.Logger.Debug($"TextMiner Client.TextFromFile Done OCR Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  skipOCR:{intensity.ToString()}");
                            }

                            if (res.IsValid == HlidacStatu.Lib.OCR.Api.Result.ResultStatus.InQueueWithCallback)
                            {
                                Base.Logger.Debug($"Queued TextMiner Client.TextFromFile Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  taskid:{res.Id}");
                            }
                            else if (res.IsValid == HlidacStatu.Lib.OCR.Api.Result.ResultStatus.Invalid)
                            {
                                Base.Logger.Error($"ERROR TextMiner Client.TextFromFile Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  taskid:{res.Id}  error:{res.Error}");
                            }
                            else if (res.IsValid == HlidacStatu.Lib.OCR.Api.Result.ResultStatus.Unknown)
                            {
                                Base.Logger.Error($"UNKNOWN Status TextMiner Client.TextFromFile Id:{item.Id} att:{att.nazevSouboru}  async:{asyncOCR}  taskid:{res.Id}  error:{res.Error}");
                            }
                            else if (res.IsValid == HlidacStatu.Lib.OCR.Api.Result.ResultStatus.Valid)
                            {
                                if (res.Documents.Count > 1)
                                {
                                    //first
                                    att.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(res.Documents[0].Text);
                                    if (res.Documents[0].UsedOCR)
                                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                                    else
                                        att.PlainTextContentQuality = DataQualityEnum.Parsed;

                                    att.ContentType = res.Documents[0].ContentType;

                                    att.UpdateStatistics(item);

                                    att.LastUpdate = DateTime.Now;

                                    if (att.EnoughExtractedText)
                                    {
                                        if (att.PlainTextContentQuality == DataQualityEnum.Estimated)
                                            item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", this));
                                        else
                                            item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", this));

                                    }
                                    changed = true;

                                    for (int ii = 1; ii < res.Documents.Count; ii++)
                                    {

                                        var att1 = new Smlouva.Priloha();
                                        att1.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(res.Documents[ii].Text);
                                        if (res.Documents[ii].UsedOCR)
                                            att1.PlainTextContentQuality = DataQualityEnum.Estimated;
                                        else
                                            att1.PlainTextContentQuality = DataQualityEnum.Parsed;

                                        att.ContentType = res.Documents[ii].ContentType;

                                        att1.UpdateStatistics(item);

                                        att1.LastUpdate = DateTime.Now;

                                        if (att1.EnoughExtractedText)
                                        {
                                            if (att1.PlainTextContentQuality == DataQualityEnum.Estimated)
                                                item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + (item.Prilohy.Count() + ii).ToString() + "].PlainTextContent", "", "", this));
                                            else
                                                item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + (item.Prilohy.Count() + ii).ToString() + "].PlainTextContent", "", "", this));

                                        }
                                        newPrilohy.Add(att1);
                                    }
                                    //others

                                }
                                else if (res.Documents.Count == 1)
                                {
                                    att.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(res.Documents[0].Text);
                                    if (res.Documents[0].UsedOCR)
                                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                                    else
                                        att.PlainTextContentQuality = DataQualityEnum.Parsed;
                                    att.ContentType = res.Documents[0].ContentType;
                                    att.UpdateStatistics(item);

                                    att.LastUpdate = DateTime.Now;

                                    if (att.EnoughExtractedText)
                                    {
                                        if (att.PlainTextContentQuality == DataQualityEnum.Estimated)
                                            item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", this));
                                        else
                                            item.Enhancements = item.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", this));
                                    }
                                    changed = true;
                                }
                            }
                            if (newPrilohy.Count > 0)
                                item.Prilohy = item.Prilohy.Concat(newPrilohy).ToArray();

                            Base.Logger.Debug("Done TextMiner Client.TextFromFile Id:" + item.Id + " att:" + att.nazevSouboru);

                        }
                        finally
                        {
                            Base.Logger.Debug($"deleting temporary {downloadedFile} file TextMiner Client.TextFromFile Id:" + item.Id + " att:" + att.nazevSouboru);
                            Devmasters.IO.IOTools.DeleteFile(downloadedFile);
                        }

                    }

                }
            }

            return changed;
        }



    }
}

