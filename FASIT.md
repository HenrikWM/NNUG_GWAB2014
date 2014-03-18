NNUG - Global Windows Azure Bootcamp 2014
=========================================

Denne fasiten viser hvordan man oppgaver som ble gitt på NNUGs Global Windows Azure Bootcamp 2014 hos Bouvet 29. mars.


Hva fasiten dekker
--------------------------

* Oppretting av nødvendige SSH-nøkler til bruk i **Azure**
* Oppretting og deploying en **Azure Cloud Service** med en **web role**
* Oppretting av et **Azure Virtual Network** og plassere vår **web role** inn i dette nettverket
* Oppretting og konfigurasjon av **Point-to-Site VPN** mot et **Azure Virtual Network**
* Oppretting av en **Azure VM** med **elasticsearch** på Debian


### Før du begynner

Hvis du skal følge fasiten så bør du:

* Ha med egen pc med VS 2012/2013
* Ha installert siste versjon av Azure SDK
* Ha installert [Azure command-line tool](http://www.windowsazure.com/en-us/develop/nodejs/how-to-guides/command-line-tools/)
* Ha installert [OpenSSL for Windows](http://downloads.sourceforge.net/gnuwin32/openssl-0.9.8h-1-bin.zip) 
* Lastet ned [PuttyGen](http://the.earth.li/~sgtatham/putty/latest/x86/puttygen.exe), [Putty](http://the.earth.li/~sgtatham/putty/latest/x86/putty.exe) og [PSCP](http://the.earth.li/~sgtatham/putty/latest/x86/pscp.exe) 
* Ha konto til [Azure-portalen](http://www.windowsazure.com/)

Denne guiden er tatt fra [elasticsearch sin egne sider](http://www.elasticsearch.org/blog/azure-cloud-plugin-for-elasticsearch/) for deres [Azure-plugin](https://github.com/elasticsearch/elasticsearch-cloud-azure), men er tilpasset for en utvikler på en Windows PC da guiden forutsetter at man sitter med en Linux-maskin og bruker Linux-kommandoer som *cat*, *ssh* m.fl.


1. Opprett SSH-nøkler
------------------------

For å komme igang med Azure trenger vi å lage et sertifikat. Dette sertifikatet med tilhørende privat nøkkel får vi bruk for når vi skal deploye til Azure og koble til vår elasticsearch-VM senere. Veiledningen under er basert på dokumentasjon fra windowsazure.com på [SSH-nøkkelgenerering](http://www.windowsazure.com/en-us/documentation/articles/linux-use-ssh-key/#create-a-private-key-on-windows) til bruk i Azure.

Her er en beskrivelse av hvordan du oppretter disse ved å bruke `openssl.exe`:

```

	# Opprett privat nøkkel og pem-fil
    openssl.exe req -x509 -nodes -days 365 -newkey rsa:2048 -keyout azure-private.key -out azure-certificate.pem			

	# Opprett sertifikat
	openssl.exe  x509 -outform der -in azure-certificate.pem -out azure-certificate.cer


```

Verifiser at du finner `azure-private.key`, `azure-certificate.pem`, `azure-certificate.cer` i samme mappe som `openssl.exe`. Kopier disse over til `C:\certs` for senere bruk. 


2. Opprett Windows Azure Cloud Service
------------------------------------

Du skal nå opprette en **Azure Cloud Service** og en **web role**. 

### Installer og last opp sertifikat

Finn `azure-certificate.cer` og dobbelklikk på filen og velg "Install certificate" for å installere denne på maskinen din. Last deretter opp `azure-certificate.cer` i Azure-portalen:

1. Gå til Azure-portalen og velg Settings > Management certificates. 
2. Klikk på Upload-knappen nederst på siden og velg `azure-certificate.cer` i `C:\certs`.

Du har nå lastet opp sertifikatet som gjør at deploy og autentisering kan utføres.

### Opprett Cloud Service med web role

1. Åpne Visual Studio og velg File > New project. Velg malen Visual C# > Cloud > Windows Azure Cloud Service:
 - Name: GWAB.Azure
 - Solution: GWAB
 - Klikk Ok.
2. Velg ASP.NET Web Role og klikk på pil til høyre for å legge den inn i vår Cloud Service. Klikk Ok.
3. Velg malen MVC og klikk på "Change Authentication" og velg "No Authentication". Klikk Ok.

Du har nå opprettet en Cloud Service med en ASP.Net MVC web role. Du kan nå deploye din nettside til Azure:

1. Høyreklikk på GWAB.Azure-prosjektet og velg "Publish".
2. Logg inn med ditt Azure-abonnement og velg ditt abonnement i abonnementlisten og klikk på Next.
3. Opprett en ny Cloud Service ved å gå til "Cloud Service:" og velg "Create New" fra nedtrekksmenyen. Oppgi navnet "GWAB2014" eller tilsvarende og velg lokasjon "West Europe".
4. Du kan aktivere Remote Desktop hvis ønskelig. Husk å velg sertifikatet `azure-certificate.cer` i `C:\certs`. 
5. Velg Next og deretter Publish på neste side.

Din Azure Cloud Service blir nå deployet til Azure. Du kan følge statusen i Azure-portalen på Cloud Services > GWAB2014. Når deploy er utført kan du prøve å aksessere nettsiden ved å gå til [gwab2014.cloudapp.net](http://gwab2014.cloudapp.net).


### Koble til elasticsearch

Det er allerede satt opp en elasticsearch-instans i Azure og du skal nå koble til denne slik at nettportalen kan utføre spørringer og få søketreff tilbake. Vi bruker da NEST som er en C#-klient for elasticsearch.

#### Installer NEST med NuGet

Velg Webrole1 som Default project og installer NEST ved å kjøre følgende kommando i Package Manager Console i Visual Studio:

    PM> Install-Package NEST


#### Legg inn kode for søk

##### index.cshtml

Lim inn følgende kode i `\webrole1.web\views\home\index.cshtml` etter linje 12:


	<div class="row">
	    <div class="col-md-12">
	        @{
	            using (Html.BeginForm("search", "Home"))
	            {
	                <p>@Html.TextBoxFor(m => m.QueryString, new { @class = "form-control", placeholder = "Search for news from vg.no..." })</p>
	                <p><input type="submit" value="Search" class="btn btn-primary btn-large" /></p>
	            }
	        }
	    </div>
	</div>

og lim inn øverst i filen:

```
	@model GWAB.Web.Models.HomeModel
``` 

##### HomeModel

I mappen `\webrole1.web\Models` opprett en ny modellklasse for Home:

	using System.ComponentModel.DataAnnotations;

	namespace GWAB.Web.Models
	{
	    public class HomeModel
	    {
	        [Display(Name = "Search for news:")]
	        public string QueryString { get; set; }
	
	        public HomeModel()
	        {
	            QueryString = string.Empty;
	        }
	    }
	}

I samme mappe opprett en ny modellklasse for søketreff:

	using System.Collections.Generic;

	namespace GWAB.Web.Models
	{
	    public class SearchResultsModel
	    {
	        public ICollection<RssItem> Items { get; set; }
	
	        public SearchResultsModel()
	        {
	            Items = new List<RssItem>();
	        }
	    }
	}

I samme mappe opprett en ny modellklasse for rss-nyhetene som NEST skal mappe fra vår "page"-mapping i elasticsearch til C#. Merk at det brukes `DataMember` og `Name` for å mappe direkte mellom felter i "page"-mappingen og feltene i `RssItem`-klassen.

	using System.Runtime.Serialization;
	
	namespace GWAB.Web.Models
	{
	    [DataContract]
	    public class RssItem
	    {
	        [DataMember(Name = "title")]
	        public string Title { get; set; }
	
	        [DataMember(Name = "description")]
	        public string Description { get; set; }
	
	        [DataMember(Name = "author")]
	        public string Author { get; set; }
	
	        [DataMember(Name = "link")]
	        public string Link { get; set; }
	
	        public RssItem()
	        {
	            Title = string.Empty;
	            Description = string.Empty;
	            Author = string.Empty;
	            Link = string.Empty;
	        }
	    }
	}

##### SearchResult.cshtml

Opprett viewet `SearchResults.cshtml` under `\webrole1.web\views\home` og lim inn følgende kode:

	@model GWAB.Web.Models.SearchResultsModel

	@{
	    ViewBag.Title = "Search results";
	}
	
	<h2>Search results</h2>
	<h3>Your search returned @Model.Items.Count hit(s):</h3>
	
	<hr/>
	
	<div id="searchresults">
	    @foreach (var item in Model.Items)
	    {
	        <h4><a href="@item.Link" title="@item.Title">@item.Title</a></h4>
	        <p>@item.Description</p>
	    }
	</div>

##### HomeController.cs

Lim inn kode i konstruktøren HomeController() for å koble NEST til vår elasticsearch i Windows Azure:

	const string elasticsearchEndpoint = "http://gwab2014-elasticsearch-cluster.cloudapp.net";

    var uri = new Uri(elasticsearchEndpoint);

    var settings = new ConnectionSettings(uri)
        .SetDefaultIndex("newspapers") // index: newspapers
        .MapDefaultTypeNames(i => i.Add(typeof(RssItem), "page")) // mapping: page
        .EnableTrace(); // gir oss json-output fra NEST til Output-vinduet i Visual Studio

    _searchClient = new ElasticClient(settings);

Opprett ny privat klassevariabel `_searchClient` øverst i `HomeController`-klassen:

	private readonly ElasticClient _searchClient;

Lim inn følgende kode for ny action som tar imot søkestrengen, setter søketreffene fra NEST til `Items`-listen i `SearchResultsModel`, og returnerer viewet `SearchResults` med modellen:

	public ActionResult Search(string querystring)
    {
        var model = new SearchResultsModel();

        if (string.IsNullOrEmpty(querystring) == false)
        {
            var results = _searchClient.Search<RssItem>(s => s
                    .From(0)
                    .Size(100)
                    .Query(q => q.QueryString(qs => qs.OnFields(f => f.Description).Query("Cantona")))
            );

            model.Items = results.Documents.ToList();
        }

        return View("SearchResults", model);
    }

Test endringene lokalt i Azure-emulatoren ved å merke prosjektet **GWAB.Azure** som "Startup Project" og trykk F5. Søk etter nyheter fra vg.no i søkeboksen og se om det kommer treff. Ha gjerne på breakpoints i "Search"-action så du kan se på spørringene som blir utført av NEST. For innblikk i hva som er indeksert i elasticsearch på *gwab2014-elasticsearch-cluster.cloudapp.net*, gå til [http://gwab2014-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://gwab2014-elasticsearch-cluster.cloudapp.net/_plugin/head/) for å se på dataene.

Deploy applikasjonen ut til Azure og test at søke virker ute på Azure.


3. Opprett et Windows Azure Virtual Network
-------------------------------------------

Nå har du en cloud service med en web role og et fungerende søk mot elasticsearch. Enn så lenge ligger dette i Azure som en cloud service og isolert. Et mer reelt scenario er at man har flere miljø, typisk utvikling (DEV), staging, produksjon osv. og da bør alle disse instansene samles for å ha kontroll og enklere tilgang.

La oss putte dette inn i et **Virtual Network** slik at du kan plassere instanser og tjenester innenfor et lukket nettverk. Å ha ressurser i et lukket nettverk gir mange fordeler, som f.eks. å teste løsningen uten å åpne et *Endpoint* mot omverdnen, få tilgang til lokale disker på instansene og å bruke Remote Desktop inn på instansene uten å måtte gå via *Azure Management Portal*.


### Opprett Virtual Network

1. Gå til Azure Management Portal > Networks > og klikk på "Create a virtual network.
2. Oppgi følgende:
	1. Name: gwab2014-we-vnet
	2. Region: West-Europe
	3. Affinity Group: Create new
	4. Affinity Group Name: gwab2014	
	5. Gå til neste side
3. Hopp over DNS-Servers og Site-to-Site/Point-to-Site Connectivity og gå til neste side
4. Vi ønsker å plassere våre ressurser i et 10.0.1.0/24 adresseområde for vårt utviklingsmiljø i Azure (DEV), oppgi følgende:
	1. Starting IP: 10.0.1.0
	2. CIDIR: /24 (256)
	3. Address Space skal nå være 10.0.1.0/24.
	4. Subnets:
		1. Name: DEV
		2. Starting IP: 10.0.1.0 
		3. CIDIR: /24 (256)
	5. Bekreft at Usable Address Space er 10.0.1.0 - 10.0.1.255. Dette gir oss 255 ledige adresser for vårt DEV-miljø.
5. Klikk på Complete-knappen og vent i ca. 5 min mens nettverket blir opprettet.


### Legg inn webrole1 i nettverket

Du har nå et virtuelt nettverk i Azure. Neste skritt er å melde inn din web role inn i nettverket.

Åpne filen `ServiceConfiguration.Cloud.cscfg` og lim inn under `</Role>`:

	<NetworkConfiguration>
	    <VirtualNetworkSite name="gwab2014-we-vnet" />
	    <AddressAssignments>
	      <InstanceAddress roleName="webrole1">
	        <Subnets>
	          <Subnet name="DEV" />
	        </Subnets>
	      </InstanceAddress>	      
	    </AddressAssignments>
	</NetworkConfiguration>

Gå til Azure Management Portal og slett cloud servicen (kan ikke legge til ressurser i et nettverk når man foretar update/upgrade på en cloud service). Velg "Publish" på "GWAB.Azure"-prosjektet, opprett ny cloud service (bruk samme navn som du hadde) og klikk på "Publish"-knappen. 

Følg med på dashbordet til det virtuelle nettverket i Azure-portalen. Når løsningen er deployet skal du se din web role instans dukke opp under "resources" med angitt ip-adresse fra subnettet "DEV".


### Opprett Point-to-Site VPN

Siste steg er å opprette en VPN-tunnell inn til nettverket slik at man kan komme inn i nettverket utenfor Azure.

1. Gå til Azure Management Portal > Network > Configure (arkfane).
2. Under point-to-site connectivity huk av for "Configure Point-to-site connectivity".
3. Klikk på "Add address space"-knappen under DEV-subnettet.
4. Oppgi følgende for VPN- og Gateway-subnettene:
	1. Starting IP: 10.0.9.0
	2. CIDIR: /24 (251)
	3. Sjekk at Address Space er 10.0.9.0/27 og Usable Address Range er 10.0.9.4 - 10.0.9.254. Dette gir oss 30 ledige adresser for VPN-klienter og Gateway. 
	4. Lag subnett VPN:
		1. Starting IP: 10.0.9.0
		2. CIDIR: /25 (123)
		3. Usable Address Range skal være 10.0.9.4 - 10.0.9.126
	5. Lag subnett Gateway:
		1. Starting IP: 10.0.9.128
		2. CIDIR: /29 (3)
		3. Usable Address Range skal være 10.0.9.132 - 10.0.9.134 
	6. Klikk på Save nederst på siden og velg Yes.







4. Opprett en Windows Azure Virtual Machine med elasticsearch
-------------------------------------------

Nå som elasticsearch-teamet har lagd en Azure-plugin er det enkelt å komme igang med en elasticsearch i Azure. Denne beskrivelsen vil vise hvordan vi bruker Azure command-line tool til å opprette en VM basert på et Debian-image, og så bruker vi SSH for å koble til og installere java, elasticsearch og et par nyttige plugins.

### Opprett java keystore nøkkel

Følg beskrivelsen for å opprette en java keystore nøkkel:

	openssl x509 -outform der -in azure-certificate.pem -out azure-certificate.cer
	
	openssl pkcs8 -topk8 -nocrypt -in azure-private.key -inform PEM -out azure-pk.pem -outform PEM
	
	openssl x509 -inform der -in azure-certificate.cer -out azure-cert.pem
	
	type azure-cert.pem azure-pk.pem > azure.pem.txt
	
	openssl pkcs12 -export -in azure.pem.txt -out azurekeystore.pkcs12 -name azure -noiter -nomaciter
	
	# Bruk passord: Nnug2014!

Kopier ut `azurekeystore.pkcs12` fra mappen til `openssl.exe` til `C:\certs`.


### Opprett VM

Så kan vi opprette vår Azure VM basert på image `b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB`:
		
	# Deploy an Ubuntu image on an extra small instance in West Europe:
	azure vm create azure-elasticsearch-cluster \
	
	b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB \
	
	--vm-name myesnode1 \
	
	--location "West Europe" \
	
	--vm-size extrasmall \
	
	--ssh 22 \
	
	--ssh-cert "C:\certs\azure-certificate.pem" \
	
	elasticsearch Password1234#!!
	
	# elasticsearch / Password1234#!! are the SSH login/password for this instance.


Nå opprettes vår virtuelle maskin i Azure. La det går 2-4 minutter før du går videre til neste steg. Du kan følge framdriften i Azure-portalen under "Virtual machines".


### Installer og konfigurer elasticsearch

Vi kobler opp til vår VM via SSH og bruker Putty til dette. Putty trenger en ppk-fil som er vår private nøkkel så da bruker vi Puttygen til å generere ppk-filen.


#### Opprett ppk-fil for Putty

Åpne Puttygen og gå på File->Load private key. Velg filen `azure-private.key` fra `C:\certs\`. Klikk på "Save private key"-knappen for å lagre til ny fil med navn `azure-private.ppk` i samme mappe.


#### Koble til med Putty

Oppgi følgende innstillinger i Putty:

* Session->Host name: `azure-elasticsearch-cluster.cloudapp.net`
* Connection->SSH->Auth->"Private key file for authentication": Velg filen `azure-private.ppk` fra mappe `C:\certs`. Klikk på "Open".

**Viktig**: Får du feilmeldingen "Host does not exist" i Putty så bruk IP-adressen til VM'ens Cloud Service

Putty starter SSH-sesjonen i kommandovindu. Oppgi så brukernavnet "elasticsearch". Du blir nå autentisert med din private nøkkel og er kommet inn på VM'en.


#### Kopiere over java keystore fil med Putty

Vi må kopiere vår java keystore-fil `azurekeystore.pkcs12` fra `C:\certs` over til `/home/elasticsearch` på vår Ubuntu VM. 

Åpne nytt kommandovindu på din maskin og finn `pscp.exe`, og kjør følgende kommando:

	# Kopier java keystore-fil fra C:\certs\ til /home/elasticsearch
	pscp -i "C:\certs\azurekeystore.pkcs12" elasticsearch@azure-elasticsearch-cluster.cloudapp.net:/home/elasticsearch

*Velg 'Y' hvis du får spørsmål om å lagre host key i cache.*


Nå skal filen `azurekeystore.pkcs12` være overført til mappen `/home/elasticsearch`. Verifiser dette ved å kjøre: 

	# Hent filer i home-mappen til elasticsearch
	ls /home/elasticsearch

### Installer elasticsearch

Siden elasticsearch trenger Java for å kjøre må vi installere Java JRE 7 på vår VM med OpenJDK7. Kjør følgende kommandoer:

	# Oppdater alle pakker først
	sudo apt-get update

	# Installer Java
	sudo apt-get install openjdk-7-jre-headless

*Velg 'Y' hvis du blir spurt om diskplassbruk under installasjonen.*

Verifiser at Java er blitt installert ved å kjøre:

	java -version

Da har vi installert Java og vi klare for å installere elasticsearch på vår Ubuntu VM. Vi bruker Putty og kjører følgende kommandoer:

	# Last ned elasticsearch for Debian
	curl -s https://download.elasticsearch.org/elasticsearch/elasticsearch/elasticsearch-1.0.0.deb -o elasticsearch-1.0.0.deb
	
	# Pakk ut og installer
	sudo dpkg -i elasticsearch-1.0.0.deb

#### Installer og konfigurer Azure-plugin for elasticsearch

Bruk Putty og kjør følgende kommandoer:

	# Stopp elasticsearch
	sudo service elasticsearch stop

	# Installer elasticsearch Azure-plugin
	sudo /usr/share/elasticsearch/bin/plugin -install elasticsearch/elasticsearch-cloud-azure/2.0.0

	# Legg inn konfigurasjonsverdier med vi/vim
	sudo vi /etc/elasticsearch/elasticsearch.yml

Legg inn følgende linjer nederst i .yml-filen:

	cloud:
	        azure:
	            keystore: <path til keystore-fil>
	            password: <passord til keystore-filen>
	            subscription_id: <din Azure-subscription id>
	            service_name: <elasticsearch clusternavn>
	    discovery:
	            type: azure

	# Slå av replica shards for nå
	index.number_of_replicas: 0

Eksempel på ferdig konfigurasjon:

	cloud:
	    azure:
	            keystore: /home/elasticsearch/azurekeystore.pkcs12
	            password: Nnug2014!
	            subscription_id: 78846242-9c0e-47b5-b157-e9727c7599c7
	            service_name: azure-elasticsearch-cluster
	    discovery:
	            type: azure

	# Slå av replica shards for nå
	index.number_of_replicas: 0

Start elasticsearch:

	sudo service elasticsearch start

Skulle en feil oppstå underveis, eller senere i forbindelse med utvikling og testing, så kan du finne loggfilen til elasticsearch i mappen `/var/log/elasticsearch`.


#### Åpne port 80 på vår VM

For å åpne for spørringer mot elasticsearch må vi sette opp et "public endpoint" mot vår VM i Azure. I endpointet mapper vi port **80** til elasticsearch sin default http-lytteport **9200**.

Gå til Azure portalen->Virtual machines->myesnode1->endpoints og velg "Add". Velg Add a stand-alone endpoint og gå videre. Oppgi følgende:
		
* Name: HTTP
* Protocol: TCP
* Public port: 80
* Private port: 9200

og lagre. Gå til [http://azure-elasticsearch-cluster.cloudapp.net/](http://azure-elasticsearch-cluster.cloudapp.net/). Verifiser at du får opp systeminformasjon om elasticsearch-clusteret.
		
### Valgfrie oppgaver

#### Installer plugins til elasticsearch på din VM

Det er spesielt to plugins til elasticsearch som gir oversikt over noder, indekser, aliaser m.m. Dette er **Head** og **BigDesk**, og kjør følgende kommandoer i Putty-kommendovinduet for å installere disse:

	# Installer plugins Head og BigDesk
	sudo /usr/share/elasticsearch/bin/plugin -install mobz/elasticsearch-head
	
	sudo /usr/share/elasticsearch/bin/plugin -install lukas-vlcek/bigdesk

Restart elasticsearch:

	sudo service elasticsearch restart

elasticsearch-head er nå klar på [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) og bigdesk på [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/).


#### Skalering av din elasticsearch-VM

Nå som vi har en elasticsearch på en VM kan det være nyttig å kunne skalere ut for å ta imot større pågang. Dette kan skriptes med Azure command-line tool. Vi skalerer ut vår VM til 10 instanser ved å slå av vår VM, fange et image av VM'en, og så opprette 10 stk VM'er basert på dette imaget. 

Her er en beskrivelse av kommandoene du kan kjøre:

	# Slå av VM
	azure vm shutdown myesnode1
	
	# Fang et image av VM'en
	azure vm capture myesnode1 esnode-image --delete
	
	# Start 10 instanser av vår VM:
	for x in $(seq 1 10)
	    do
	        echo "Launching azure instance #$x..."
	        azure vm create azure-elasticsearch-cluster \
	                        esnode-image \
	                        --vm-name myesnode$x \
	                        --vm-size extrasmall \
	                        --ssh $((21 + $x)) \
	                        --ssh-cert /tmp/azure-certificate.pem \
	                        --connect \
	                        elasticsearch Password1234#!!
	    done

Vent 2-4 minutter og så gå til http://azure-elasticsearch-cluster.cloudapp.net/. Verifiser at du får opp systeminformasjon om elasticsearch-clusteret. Så gå til [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) (hvis du installerte Head-plugin til elasticsearch) og verifiser at du ser 10 noder.


#### Indekser data til din elasticsearch med RSS-rivers

For å indeksere data inn i elasticsearch brukes "data rivers". Dette er java-programmer som mater data inn i elasticsearch via REST-kall til gitte tider. Det finnes mange ulike varianter (xml, rss, json m.m.) å velge blant og med litt Java-kunnskap kan man lage egne.

I denne oppgaven følger vi [denne guiden](http://www.pilato.fr/rssriver/) for å sette opp en enkel rss-river til å konsumere en [rss-feed](http://www.vg.no/rss/create.php?categories=12,21,20,34,10,164,22,25&keywords=&limit=20) levert av vg.no.

I Putty-kommandolinje kjør følgende:

	cd /usr/share/elasticsearch

	bin/plugin -install fr.pilato.elasticsearch.river/rssriver/0.2.0
		
	curl -XPUT 'http://localhost:9200/newspapers/' -d '{}'
	
	curl -XPUT 'http://localhost:9200/newspapers/page/_mapping' -d '{
	  "page" : {
	    "properties" : {
	      "title" : {"type" : "string", "analyzer" : "french"},
	      "description" : {"type" : "string", "analyzer" : "french"},
	      "author" : {"type" : "string"},
	      "link" : {"type" : "string"}
	    }
	  }
	}' 
	
	curl -XPUT 'localhost:9200/_river/newspapers/_meta' -d '{
	  "type": "rss",
	  "rss": {
	    "feeds" : [ {
	        "name": "vg",
	        "url": "http://www.vg.no/rss/create.php?categories=12,21,20,34,10,164,22,25&keywords=&limit=20"
	        }
	    ]
	  }
	}'

Nå har du opprettet en ny index **newspapers** og en ny mapping **page** for alle *documents* som skal indekseres hit. Riveren vil putte hvert rss-objekt inn som et nytt *document* i denne indexen. 

Hvis du har installert Head-plugin kan du gå til [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) og se at data er nå indeksert. 