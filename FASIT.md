NNUG - Global Windows Azure Bootcamp 2014
=========================================

Denne fasiten viser hvordan man setter opp de ulike komponentene som ble gitt som oppgaver i NNUGs Global Windows Azure Bootcamp 2014.


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
* Lastet ned [PuttyGen](http://the.earth.li/~sgtatham/putty/), [Putty](http://the.earth.li/~sgtatham/putty/) og [PSCP](http://the.earth.li/~sgtatham/putty/) 
* Ha konto til [Azure-portalen](http://www.windowsazure.com/)

Denne guiden er tatt fra [elasticsearch sin egne sider](http://www.elasticsearch.org/blog/azure-cloud-plugin-for-elasticsearch/) for deres [Azure-plugin](https://github.com/elasticsearch/elasticsearch-cloud-azure), men er tilpasset for en utvikler på en Windows PC da guiden forutsetter at man sitter med en Linux-maskin og har tilgang til cat, ssh m.fl.


Oppretting av SSH-nøkler
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


Oppretting av Azure Cloud Service
---------------------------------

Begynn med å laste opp `azure-certificate.cer` i Azure-portalen
<TODO>


Oppretting av en Azure VM med elasticsearch
-------------------------------------------

Nå som elasticsearch-teamet har lagd en Azure-plugin er det enkelt å komme igang med en elasticsearch i Azure. Denne beskrivelsen vil vise hvordan vi bruker Azure command-line tool til å opprette en VM basert på et Debian-image, og så bruker vi SSH for å koble til og installere java, elasticsearch og et par nyttige plugins.

### Opprett java keystore nøkkel

Følg beskrivelsen for å opprette en java keystore nøkkel:

```

	openssl x509 -outform der -in azure-certificate.pem -out azure-certificate.cer
	
	openssl pkcs8 -topk8 -nocrypt -in azure-private.key -inform PEM -out azure-pk.pem -outform PEM
	
	openssl x509 -inform der -in azure-certificate.cer -out azure-cert.pem
	
	type azure-cert.pem azure-pk.pem > azure.pem.txt
	
	openssl pkcs12 -export -in azure.pem.txt -out azurekeystore.pkcs12 -name azure -noiter -nomaciter
	
	# Bruk passord: Nnug2014!

```

Kopier ut `azurekeystore.pkcs12` fra mappen til `openssl.exe` til `C:\certs`.


### Opprett VM

Så kan vi opprette vår Azure VM basert på image `b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB`:

```
		
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

```

Nå opprettes vår virtuelle maskin i Azure. La det går 2-4 minutter før du går videre til neste steg. Du kan følge framdriften i Azure-portalen under "Virtual machines".



### Installere elasticsearch

Vi kobler opp til vår VM via SSH og bruker Putty til dette. Putty trenger en ppk-fil som er vår private nøkkel så da bruker vi Puttygen til å generere ppk-filen.


#### Opprett ppk-fil for Putty

Åpne Puttygen og gå på File->Load private key. Velg filen `azure-private.key` fra `C:\certs\`. Klikk på "Save private key"-knappen for å lagre til ny fil med navn `azure-private.ppk` i samme mappe.


#### Koble til med Putty

Oppgi følgende innstillinger i Putty:

* Session->Host name: `azure-elasticsearch-cluster.cloudapp.net`
* Connection->SSH->Auth->"Private key file for authentication": Velg filen `azure-private.ppk` fra mappe `C:\certs`. Klikk på "Open".

Putty starter SSH-sesjonen i kommandovindu. Oppgi så brukernavnet "elasticsearch". Du blir nå autentisert med din private nøkkel og er kommet inn på VM'en.

#### Kopiere over java keystore fil med Putty

Vi må kopiere vår java keystore-fil `azurekeystore.pkcs12` fra `C:\certs` over til `/home/elasticsearch`. I Puttys kommandivindu kjør:

```

	# Kopier privat nøkkel og java keystore-fil til elasticsearch-home
	pscp -i "C:\certs\azure-private.ppk" "C:\certs\azurekeystore.pkcs12" elasticsearch@azure-elasticsearch-cluster.cloudapp.net:/home/elasticsearch
					

```

Nå skal filen `azure-private.ppk` og `azurekeystore.pkcs12` være overført til mappen `/home/elasticsearch`. Verifiser dette ved å kjøre: 

```

	# Hent filer i home-mappen til elasticsearch
	ls /home/elasticsearch

```

### Installer Java

Siden elasticsearch trenger Java for å kjøre må vi installere Java JRE 7 på vår VM med OpenJDK7. Kjør følgende kommandoer:

```

	# Oppdater alle pakker først
	sudo apt-get update

	## Installer Java
	sudo apt-get install openjdk-7-jre-headless
		
		
```

Verifiser at Java er blitt installert ved å kjøre:

	
```
	
	java -version
	
```

### Installer elasticsearch

Da er vi klare for å installere elasticsearch på vår VM. Vi bruker Putty og kjører følgende kommandoer:



```	

	# Last ned elasticsearch for Debian
	curl -s https://download.elasticsearch.org/elasticsearch/elasticsearch/elasticsearch-1.0.0.deb -o elasticsearch-1.0.0.deb
	
	# Pakk ut og installer
	sudo dpkg -i elasticsearch-1.0.0.deb
			

```

#### Installer og konfigurer Azure-plugin for elasticsearch

Bruk Putty og kjør følgende kommandoer:

```

	# Stopp elasticsearch
	sudo service elasticsearch stop

	# Installer elasticsearch Azure-plugin
	sudo /usr/share/elasticsearch/bin/plugin -install elasticsearch/elasticsearch-cloud-azure/2.0.0

	# Legg inn konfigurasjonsverdier med vi/vim
	sudo vi /etc/elasticsearch/elasticsearch.yml

```

Legg inn følgende linjer nederst i .yml-filen:

```

	cloud:
	        azure:
	            keystore: <path til keystore-fil>
	            password: <passord til keystore-filen>
	            subscription_id: <din Azure-subscription id>
	            service_name: <elasticsearch clusternavn>
	    discovery:
	            type: azure

```

Eksempel på ferdig konfigurasjon:

```

	cloud:
	    azure:
	            keystore: /home/elasticsearch/azurekeystore.pkcs12
	            password: Nnug2014!
	            subscription_id: 78846242-9c0e-47b5-b157-e9727c7599c7
	            service_name: azure-elasticsearch-cluster
	    discovery:
	            type: azure

```

Restart elasticsearch:

```

	sudo service elasticsearch start

```

Skulle en feil oppstå underveis, eller senere i forbindelse med utvikling og testing, så kan du finne loggfilen til elasticsearch i mappen `/var/log/elasticsearch`.


#### Åpne port 80 på vår VM

For å åpne for spørringer mot elasticsearch på vår VM må vi sette opp et "public endpoint" mot vår VM i Azure. I endpointet mapper vi port **80** til elasticsearch sin default http-lytteport **9200**.

Gå til Azure portalen->Virtual machines->myesnode1->endpoints og velg "Add". Oppgi følgende:
		
* Name: HTTP
* Protocol: TCP
* Public port: 80
* Private port: 9200

og lagre. Gå til [http://azure-elasticsearch-cluster.cloudapp.net/](http://azure-elasticsearch-cluster.cloudapp.net/). Verifiser at du får opp systeminformasjon om elasticsearch-clusteret.
		

#### (Valgfritt) Installer plugins til elasticsearch

Det er spesielt to plugins til elasticsearch som gir oversikt over noder, indekser, aliaser m.m. Dette er **Head** og **BigDesk**, og kjør følgende kommandoer i Putty-kommendovinduet for å installere disse:

```

	# Installer plugins Head og BigDesk
	sudo /usr/share/elasticsearch/bin/plugin -install mobz/elasticsearch-head
	
	sudo /usr/share/elasticsearch/bin/plugin -install lukas-vlcek/bigdesk
		

```


Restart elasticsearch:

```

	sudo service elasticsearch start

```

elasticsearch-head er nå klar på [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) og bigdesk på [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/).


### Skalering av elasticsearch 

Nå som vi har en elasticsearch på en VM kan det være nyttig å kunne skalere ut for å ta imot større pågang. Dette kan skriptes med Azure command-line tool. Vi skalerer ut vår VM til 10 instanser ved å slå av vår VM, fange et image av VM'en, og så opprette 10 stk VM'er basert på dette imaget. 

Her er en beskrivelse av kommandoene du kan kjøre:


```

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

```

Vent 2-4 minutter og så gå til http://azure-elasticsearch-cluster.cloudapp.net/. Verifiser at du får opp systeminformasjon om elasticsearch-clusteret. Så gå til [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) (hvis du installerte Head-plugin til elasticsearch) og verifiser at du ser 10 noder.