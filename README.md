NNUG - Global Windows Azure Bootcamp 2014
=========================================

Dette er workshop-oppgaver som ble gitt i forbindelse med NNUGs Global Windows Azure Bootcamp 2014 hos Bouvet 29. mars 2014.

## Oppgavesett - Søkedrevet nettportal med elasticsearch

I dette oppgavesettet blir du guidet igjennom ulike aspekter av Windows Azure. Caset er at du skal lage en søkedrevet nettportal som er integrert med elasticsearch. For å få til dette trenger du å opprette SSH-nøkler, en cloud service med en web role, et virtuelt nettverk med VPN og til slutt opprette VM'en hvor du skal sette opp elasticsearch på Ubuntu.

Oppgavesettet beskriver hva som skal lages men er ikke en step-by-step guide. Følg gjerne [løsningsforslaget](https://github.com/HenrikWM/NNUG_GWAB2014/blob/master/FASIT.md) istedet hvis du ønsker å følge en mer detaljert guide.


### Før du begynner

Hvis du skal løse alle oppgavene så bør du:

* Ha med egen pc med VS 2012/2013
* Ha installert siste versjon av Azure SDK
* Ha installert [Azure command-line tool](http://www.windowsazure.com/en-us/develop/nodejs/how-to-guides/command-line-tools/)
* Ha installert [OpenSSL for Windows](http://downloads.sourceforge.net/gnuwin32/openssl-0.9.8h-1-bin.zip) 
* Lastet ned [PuttyGen](http://the.earth.li/~sgtatham/putty/latest/x86/puttygen.exe), [Putty](http://the.earth.li/~sgtatham/putty/latest/x86/putty.exe) og [PSCP](http://the.earth.li/~sgtatham/putty/latest/x86/pscp.exe) 
* Ha konto til [Azure-portalen](http://www.windowsazure.com/)

Denne guiden er tatt fra [elasticsearch sin egne sider](http://www.elasticsearch.org/blog/azure-cloud-plugin-for-elasticsearch/) for deres [Azure-plugin](https://github.com/elasticsearch/elasticsearch-cloud-azure), men er tilpasset for en utvikler på en Windows PC da deres eksempler forutsetter at man sitter med en Linux-maskin og bruker Linux-kommandoer som *cat*, *ssh* m.fl.

### 1. Lage SSH-nøkler

For å deploye fra Visual Studio trenger man et sertifikat. Dette kan genereres av Visual Studio men det er også nyttig å ha gjort dette selv. Du vil også trenge sertifikatet og en privat nøkkel hvis du skal fullføre alle oppgavene.

Du kan gjerne følge [guiden på windowsazure.com](http://www.windowsazure.com/en-us/documentation/articles/linux-use-ssh-key/#create-a-private-key-on-windows) for hvordan du oppretter en privat nøkkel men du får hele oppskriften på generering av privat nøkkel og opprettelse av sertifikatet [i løsningsforslaget](https://github.com/HenrikWM/NNUG_GWAB2014/blob/master/FASIT.md). 

Hvis alt går bra skal du ende opp med filene `azure-private.key`, `azure-certificate.pem` og `azure-certificate.cer`. Kopier disse over til `C:\certs` for senere bruk.

### 2. Opprett og deploy en Azure Cloud Service med en web role

Du skal nå opprette en **Azure Cloud Service** og en **web role**.





### 3. Søkedrevet nettside med elasticsearch

Løsningen som skal lages består av en **Azure Cloud Solution** som inneholder en web role for front-end i ASP.net MVC. Denne er koblet til elasticsearch som kjører på Ubuntu VM i Azure. 

