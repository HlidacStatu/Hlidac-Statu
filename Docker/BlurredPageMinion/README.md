# BlurredPageMinion

Klient pro analýzu obsahu jednotlivých stránek, identifikaci začerněných oblastní a jejich podíl vůči nezačerněnému textu na stránce. 

## Jak BlurredPageMinion spustit

1) Mít [nainstalovaný docker](https://docs.docker.com/install/). Pokud používáte Docker ve Windows, je potřeba ho mít přepnutý na [Linuxové kontejnery](https://docs.docker.com/docker-for-windows/#switch-between-windows-and-linux-containers).

2) Ke spuštění budete potřebovat API klíč, který [získáte zdarma po registraci na Hlídači státu](https://www.hlidacstatu.cz/api).

3) BlurredPageMinion vyžaduje min 200MB RAM, obvykle potřebuje kolem 600-800 MB RAM.
Analýza stránek je CPU náročná, doporučujeme instanci povolit alespoň 2 CPU (či 2 vCPU ve Swarm). Je vyžadován Intel/AMD 64bit CPU. ARM balíček v tuto chvíli není k dispozici.

4a) V tuto chvíli (srpen 2022) v rámci testovacího provozu omezujeme množství běžících BlurredPageMinion instancí. 
Pokud již máte API key, napište žádost o povolení přístupu k API BlurredPageMinion na podpora@hlidacstatu.cz (do předmětu emailu uveďte **BlurredPageMinion** a do těla emailu email, pod kterým jste získali API key)

4b) Variantně nám můžete napsat **Direct message** na twitter účet Hlídače státu (https://twitter.com/hlidacstatu)


5) Pak už stačí jen spustit docker instanci. **Prosím, pokud se nedohodneme jinak, pusťte pouze 1 instanci BlurredPageMinion** Díky.

```  sh
docker run --name blurredpageminion -d -e apikey={váš_apikey} hlidacstatu/blurred_page_minion:latestRelease  
```

## Poznámka
Docker instance může běžet i v interní síti, vyžaduje pouze HTTPS přístup na `api.hlidacstatu.cz`
V případě vážných chyb posílá docker informace o chybě na `https://api.hlidacstatu.cz/api/v2/bp/log`
 
Pokud chcete zapnout detailní logování, pak stačí přidat při spuštění parametr `-e debug=true`.
**Pozor:** pokud zapnete detailní logování, pak součástí zasílaných informací budou i informace o prostředí, ve kterém docker instance běží. Ukázka takového reportu je níže
```
OS :Unix
OS Version:Unix 5.10.102.1
OS ServicePack:
OS Architecture:X64
OS Description:Linux 5.10.102.1-microsoft-standard-WSL2 #1 SMP Wed Mar 2 00:30:59 UTC 2022
OS Runtime:ubuntu.20.04-x64
.NET Framework:.NET 6.0.7
HW Processor Count:4
Process MaxWorkingSet:8.00 EiB
Process NonpagedSystemMemorySize64:0 bytes
Process PagedMemorySize64:0 bytes
Process PagedSystemMemorySize64:0 bytes
Process PeakPagedMemorySize64:0 bytes
Process PeakVirtualMemorySize64:3.44 GiB
Process PeakWorkingSet64:52.8 MiB
Process VirtualMemorySize64:3.38 GiB
Process Physical Mem Mapped to the process:52.8 MiB
Process started:2022-07-31T14:46:35
Process started before (sec):165.3
Process UserProcessorTime in sec:0.6
Process TotalProcessorTime in sec:0.8
GC INFO: A gen0 or gen1 collection
----------
GC Ephemeral info:
GC Finalization Pending Count:0
GC Heap Size Bytes:0 bytes
GC High Memory Threshold:6.10 GiB
GC Memory Loads Bytes:0 bytes
GC Pinned Objects Count:0
GC Total Available:6.78 GiB
GC Total Committed Bytes:0 bytes
GC INFO: A blocking gen2 collection.
----------
GC FullBlocking info:
GC Finalization Pending Count:0
GC Heap Size Bytes:0 bytes
GC High Memory Threshold:6.10 GiB
GC Memory Loads Bytes:0 bytes
GC Pinned Objects Count:0
GC Total Available:6.78 GiB
GC Total Committed Bytes:0 bytes
GC INFO: A background collection.
----------
GC Background info:
GC Finalization Pending Count:0
GC Heap Size Bytes:0 bytes
GC High Memory Threshold:6.10 GiB
GC Memory Loads Bytes:0 bytes
GC Pinned Objects Count:0
GC Total Available:6.78 GiB
GC Total Committed Bytes:0 bytes

Drive Name:/
Drive / Type:Fixed
Drive / Free space for process:202 GiB
Drive / Total Free space:215 GiB
Drive / Total size:251 GiB
Drive Name:/proc
Drive /proc Type:Ram
Drive /proc Free space for process:0 bytes
Drive /proc Total Free space:0 bytes
Drive /proc Total size:0 bytes
```
