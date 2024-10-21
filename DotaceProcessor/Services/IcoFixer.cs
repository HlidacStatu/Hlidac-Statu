using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace DotaceProcessor.Services;

public class IcoFixer
{
    private static readonly Lazy<IcoFixer> _instance = new(CreateIcoFixer);
    
    private Dictionary<string, string> _companiesByName = new();
    private Dictionary<string, string> _companiesByIco = new();
    private Dictionary<string, string> _companiesByAsciiName = new();

    private HashSet<string> _entrepreneurs = new();

    private IcoFixer()
    {
    }
    
    public static IcoFixer GetInstance()
    {
        return _instance.Value;
    }

    private static IcoFixer CreateIcoFixer()
    {
        var instance = new IcoFixer();
        instance.LoadCompaniesFromHlidacStatu();
        instance.LoadEntrepreneurs();
        return instance;
    }
    
    public bool TryFindIcoForName(string name, out string ico)
    {
        ico = null;
        var nameTrimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(nameTrimmed))
            return false;

        if (!_companiesByName.TryGetValue(nameTrimmed, out string? newIco))
        {
            if (!_companiesByAsciiName.TryGetValue(MakeAsciiName(nameTrimmed), out newIco))
            {
                return false;
            }
        }
        

        // filtrovat podnikatele - tam nechceme přiřazovat ičo ke jménu, protože je to chybové
        if (_entrepreneurs.Contains(newIco))
            return false;

        ico = newIco;

        return true;
    }
        
    public bool TryFindNameForIco(string ico, out string name)
    {
        if (string.IsNullOrWhiteSpace(ico))
        {
            name = null;
            return false;
        }
        return _companiesByIco.TryGetValue(ico, out name);
    }

    private void LoadCompaniesFromHlidacStatu()
    {
        foreach (var (ico, jmeno) in FirmaRepo.GetJmenoAndIcoTuples())
        {
            AddToDictionary(ico, jmeno);
        }
    }
    

    private void AddToDictionary(string ico, string name)
    {
        if (string.IsNullOrWhiteSpace(ico) || string.IsNullOrWhiteSpace(name))
            return;

        string nameTrimmed = name.Trim();
        string asciiName = MakeAsciiName(name);
        
        _companiesByIco.TryAdd(ico, nameTrimmed);
        _companiesByName.TryAdd(nameTrimmed, ico);
        _companiesByAsciiName.TryAdd(asciiName, ico);
    }
        
    private string MakeAsciiName(string name) => Devmasters.TextUtil.RemoveDiacritics(Firma.JmenoBezKoncovky(name))
        .Trim().ToLower();

    private void LoadEntrepreneurs()
    {
        foreach (var ico in FirmaRepo.GetEntrepreneurIcos())
        {
            _entrepreneurs.Add(ico);
        }   
    }
        
}