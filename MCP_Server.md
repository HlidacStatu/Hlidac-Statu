# MCP Server

## Řízený přístup k betaverzi
15.7.2025

**UPOZORNĚNÍ:** funkčnost a změny rozhraní mohou v beta provozu nastat kdykoliv. Stejně tak kdykoliv může dojít k nečekanému ukončení provozu rozhraní. Využívejte také MCP server s rozumem, ladíme ho nyní na funkčnost, není optimalizován na rychlost a vysokou zátěž.

### Příprava
Podmínkou je existující a potvrzený (potvrzuje se platnost mailu hned po registraci) účet na www.hlidacstatu.cz. 

Napište na api@hlidacstatu.cz o přístup a v mailu uveďte emailovou adresu, pod kterou jste zaregistrování na Hlídači.
Mailem Vám přístup potvrdíme (nastavíme vám práva)

### Získání Auth tokenu
Ten najdete na https://api.hlidacstatu.cz v sekci *Autorizační token*. Musíte být zalogovaní.

### Přístup k MCP serveru přes lokální script

Níže uvedený postup je pro Claude.AI Desktop. Jiní klienti mají postup podobní, lokace konfiguračního souboru je však jiná

Lokace konfiguračního souboru: (nebo jde otevřít přes "Settings" z aplikace)
- macOS: ~/Library/Application\ Support/Claude/claude_desktop_config.json
- Windows: %APPDATA%\Claude\claude_desktop_config.json

text `api-token-hlidace` nahradťě tokenem získaným postupem výše.


```
{
    "globalShortcut": "",
    "mcpServers":
    {
        "hlidac-statu":
        {
            "command": "npx",
            "args":
            [
                "mcp-remote",
                "https://mcp.api.hlidacstatu.cz",
                "--header",
                "Authorization: Token api-token-hlidace"
            ]
        }
    }
}
```


### Přístup k MCP serveru přes HTTPS (HTTP Streaming nebo SSE)
Pokud to klient umožňuje, můžete použít přímo komunikaci přes HTTP. Podmínkou je, že můžete pro komunikaci nastavit HTTP Headers. Pokud to klient neumožňuje (k datu vzniku dokumentace např. Claude.AI),
pak použijte přístup k MCP serveru přes lokální skript.

URL: `https://mcp.api.hlidacstatu.cz`

HTTP header: `Authorization: Token api-token-hlidace`

text `api-token-hlidace` nahradťě tokenem získaným postupem výše.


### Feedback

Zpětnou vazbu, nápady, vylepšení nám můžete poslat přímo z AI Chatu (viz třeba konec komunikace na https://claude.ai/share/cd7d6d34-776f-4d69-b446-b40f92abcbcf) anebo nám pište na api@hlidacstatu.cz.

Chyby ideálně piště na https://github.com/HlidacStatu/Hlidac-Statu/issues 

Buďte prosím trpělivý, prázdniny, dovolená a omezené kapacity mohou způsobit pár denní zpoždění. Pokud bychom neodpovědeli do 3 dnů, napište klidně znovu.

## Podpora

Pokud nás chcete v naší neziskové činnosti podpořit, zde je to nejlepší místo - https://texty.hlidacstatu.cz/jak-podporit-hlidac-statu-fungovani-ceska/. 

Díky.
