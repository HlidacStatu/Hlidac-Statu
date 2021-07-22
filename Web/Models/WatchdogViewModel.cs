using System;

namespace HlidacStatu.Web.Models
{
    public class WatchdogViewModel
    {
        public Type? Datatype { get; set; }
        public string Query { get; set; }
        public string? ButtonText  { get; set; } 
        public string? PreButtonText  { get; set; } 
        public string? PostButtonText  { get; set; } 
        public string PrefillWDname  { get; set; } 
        public bool ShowWDList  { get; set; } 
        public string? DatasetId  { get; set; } 
        public bool ShowButtonIcon  { get; set; } 
        public string? ButtonCss  { get; set; }
        
        public WatchdogViewModel(Type? datatype,
            string query,
            string? buttonText = null,
            string? preButtonText = null,
            string? postButtonText = null,
            string prefillWDname = "",
            bool showWdList = false,
            string? datasetId = null,
            bool showButtonIcon = true,
            string? buttonCss = null)
        {
            Datatype = datatype;
            Query = query;
            ButtonText = buttonText;
            PreButtonText = preButtonText;
            PostButtonText = postButtonText;
            PrefillWDname = prefillWDname;
            ShowWDList = showWdList;
            DatasetId = datasetId;
            ShowButtonIcon = showButtonIcon;
            ButtonCss = buttonCss;
        }
    }
}