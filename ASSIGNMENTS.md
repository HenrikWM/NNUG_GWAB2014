NNUG - Global Windows Azure Bootcamp 2014
=========================================

This is a collection of workshop assignments that was given during NNUG's Global Windows Azure Bootcamp 2014 @ Bouvet on 29. March 2014. 


Overview of assignments
-----------------------

* *Create* and *deploy* a **Azure Cloud Service** containing a **web role**
* *Create* a **Azure Virtual Network** and move the **web role** into the network
* *Create* and *configure* **Point-to-Site VPN** to access the  **Azure Virtual Network**
* *Create* an **Azure Virtual Machine** with Debian running an instance of **elasticsearch**
* *Index* RSS-data from vg.no into your elasticsearch cluster
* *Create* a cluster of elasticsearch nodes on multiple **Azure Virtual Machines**


### Before you begin

If you are going to follow all of the steps in this assignment, then make sure you have all of the prerequisites:

* Have a PC with Visual Studio 2012/2013
* Have installed the latest version of Azure SDK
* Have installed and [configured](http://www.windowsazure.com/en-us/documentation/articles/xplat-cli/) the [Azure command-line tool](http://www.windowsazure.com/en-us/develop/nodejs/how-to-guides/command-line-tools/)
* Have installed [OpenSSL for Windows](http://code.google.com/p/openssl-for-windows/downloads/detail?name=openssl-0.9.8k_WIN32.zip&can=2&q=) 
* Have downloaded [PuTTYgen and PuTTY](http://the.earth.li/~sgtatham/putty/latest/x86/putty-0.63-installer.exe)
* Have an account for the [Azure-portal](http://www.windowsazure.com/)

### Questions?

Contact me at **henrik.moe@bouvet.no** or on [GitHub](https://github.com/HenrikWM)

Assignment #1: Create a Windows Azure Cloud Service
---------------------------------------------------

In this assignment you will create a **Azure Cloud Service** and a **web role**. This website will access an elasticsearch cluster already set-up in Azure and will return search results for news articles from vg.no. 

### Create the Cloud Service with a web role

1. Open Visual Studio in elevated mode and choose File > New project. Choose the Visual C# template > Cloud > Windows Azure Cloud Service:
 - Name: **GWAB.Azure**
 - Solution: **GWAB**
 - Click **Ok**.
2. Choose **ASP.NET Web Role** and click on the right-arrow to add it into our Cloud Service. Click Ok.
3. Choose the template **MVC** and click on **Change Authentication** and select **No Authentication**. Click **Ok**.

You have now created a Cloud Service with a ASP.net MVC web role. You can now deploy your solution and website to Azure:

1. Right-click on the **GWAB.Azure**-project and choose **Publish**.
2. Log in with your Azure-credentials and select you subscription in the list and click Next.
3. Create a new Cloud Service by going to "Cloud Service:" and select "Create New" from the dropdow-menu. Specify *GWAB2014-[your initials here]* and select location **West Europe**. 
4. Click **Next** and then **Publish** on the next page.

Your Azure Cloud Service is now being deployed to Azure. To see the deployment in action, go to the Azure-portal and then to Cloud Services > GWAB2014-[your initials here]. When the deploy is complete you can try to access the website by navigating with a browser to *gwab2014-[your initials here].cloudapp.net*.


### Connect to elasticsearch

There's an instance of elasticsearch set up for this workshop in Azure. The web role will be configured to post queries to this search cluster and get results back and show them on a search-results page. We will use NEST as a strongly-typed C#-client for elasticsearch.


#### Install NEST with NuGet

Set `Webrole1` as **Default project** and install `NEST` by running the following command in the Package Manager Console in Visual Studio:

    PM> Install-Package NEST


#### Add code for search

##### index.cshtml

Paste the following code into `\webrole1.web\views\home\index.cshtml` after line 12:

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

and paste in at the very top of the file:

	@model GWAB.Web.Models.HomeModel

##### HomeModel

In the folder `\webrole1.web\Models` create a new model class for the home page:

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

In the same folder, create a new model class for search results:

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

In the same folder, create a new model class for the RSS-news which NEST will map from a mapping in elasticsearch for the indexed RSS-news. Notice the usage of `DataMember` and `Name` to map between the fields in the elasticsearch mapping and properties in the `RssItem`-class.

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

Create a view `SearchResults.cshtml` in the folder `\webrole1.web\views\home` and paste in the following code:

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

Paste in the following code into the constructor of the `HomeController`-class to connect `NEST` to elasticsearch i Windows Azure:

	const string elasticsearchEndpoint = "http://gwab2014-elasticsearch-cluster.cloudapp.net:9200";

    var uri = new Uri(elasticsearchEndpoint);

    var settings = new ConnectionSettings(uri)
        .SetDefaultIndex("newspapers") // index: newspapers
        .MapDefaultTypeNames(i => i.Add(typeof(RssItem), "page")) // mapping: page
        .EnableTrace(); // gir oss json-output fra NEST til Output-vinduet i Visual Studio

    _searchClient = new ElasticClient(settings);

Create a new private member variable `_searchClient` at the top of the `HomeController`-class:

	private readonly ElasticClient _searchClient;

The next code snippet will take in a query, send it to elasticsearch and if there are hits NEST will map the elasticsearch documents to our RssItem-class. The method will then return the list of results in a `SearchResultsModel`-class to the view `SearchResults`.
Paste in the following code into `HomeController`-class:

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

Test the changes locally by running the solution in the Azure-emulator. Set the **GWAB.Azure**-project as "Startup Project" and hit F5. 

Search for news from vg.no by typing text into the search box and click on "Search". Make sure you have a breakpoint in the "Search"-method so that you can monitor the execution. For complete insight into the queries that NEST performs monitor the Output-window in Visual Studio.

For direct access to the data that's indexed in elasticsearch at *gwab2014-elasticsearch-cluster.cloudapp.net:9200*, go to [http://gwab2014-elasticsearch-cluster.cloudapp.net:9200/_plugin/head/](http://gwab2014-elasticsearch-cluster.cloudapp.net:9200/_plugin/head/).

Deploy the solution to Azure and test your solution again on *gwab2014-[your initials here].cloudapp.net* when the deploy is complete.


Assignment #2: Create a Windows Azure Virtual Network
-----------------------------------------------------

You now have a working **Cloud Service** with a **web role** running in Windows Azure that can run queries against elasticsearch. For now these resources are isolated inside of one 
Cloud Service but a more realistic scenario would be that you will need several environments for development, QA and production. And having all of these 
resources together into one network would make troubleshooting, remoting, direct access to single instances and disk access much easier than clicking through pages inside the Azure portal.
Another argument for placing your resources into a virtual network is that you probably don't want to give access to the Azure-portal to all of your developers who will need
access to a deployed resource. Visual Studio would be a better tool for accessing storage resources, and then using Remote Desktop to access instances inside of the network is a 
much better practice.

So we will use an **Azure Virtual Network** to group, protect and ease access to our instances. 
And by placing the instances into different subnets we can easily separate resources based on environment in which they belong to. And we won't have to open up any public 
endpoints to any of the instances either.

### Create certificates for authentication

We will need to create a root certificate and a derived client certificate for authenticating when we will connect to our network via VPN.

#### Generate certificates

Use `makecert` by running "VS2012 x64 Cross Tools Command Prompt" (either 64- or 32-bit version). It's highly recommended that you create a folder 
`C:\certs` in which all of the certificate artifacts can be placed together. 

Run the following commands:

	# Generate root certificate
    makecert -sky exchange -r -n "CN=GWAB2014 Root Certificate" -pe -a sha1 -len 2048 -ss My "c:\certs\azure-root-certificate.cer"

	# Generate a client certificate
	makecert -n "CN=GWAB2014 Client Certificate" -pe -sky exchange -m 96 -ss My -in "GWAB2014 Root Certificate" -is my -a sha1

Verify that you see the root- and client certificate in `certmgr.msc` under Certificates - Current User > Personal > Certificates.


#### Create a Virtual Network

Now that you have the necessary certificates you can create the network.

1. Go to the Azure Management Portal > Networks > and click on "Create a virtual network. Or click on "+ New"-button > Network Services > Virtual Network > Custom create.
2. Provide the following:
	1. Name: gwab2014-[your initials here]-we-vnet
	2. Region: West-Europe
	3. Affinity Group: Create new
	4. Affinity Group Name: gwab2014-[your initials here]	
	5. Go to the next page
3. Skip the page with DNS-Servers and Site-to-Site/Point-to-Site Connectivity and proceed to the next page
4. We wish to place our instances in a 10.0.1.0/24 address space for our development environment i Azure. Provide the following:
	1. Starting IP: 10.0.1.0
	2. CIDIR: /24 (256)
	3. Address Space should now be 10.0.1.0/24.
	4. Subnets:
		1. Name: DEV
		2. Starting IP: 10.0.1.0 
		3. CIDIR: /24 (256)
	5. Verify that Usable Address Space is 10.0.1.0 - 10.0.1.255. This gives us 255 available addresses for our DEV-environment.
5. Click on the Complete-button.


#### Add webrole1 into the network

You have now created the virtual network and it's ready to accept instances. We will now modify the service configuration of the cloud service project to add webrole1 (GWAB.Web_IN_0) into the network.

Open the file `ServiceConfiguration.Cloud.cscfg` and paste in the code beneath `</Role>`:

	<NetworkConfiguration>
	    <VirtualNetworkSite name="gwab2014-[your initials here]-we-vnet" />
	    <AddressAssignments>
	      <InstanceAddress roleName="webrole1">
	        <Subnets>
	          <Subnet name="DEV" />
	        </Subnets>
	      </InstanceAddress>	      
	    </AddressAssignments>
	</NetworkConfiguration>

Go to the Azure Management Portal and delete your cloud service (can add resources to a network when you're performing an update/upgrade of an existing cloud service).

1. Choose "Publish" on the "GWAB.Azure"-project
2. Create a a new cloud service (use the same name as before and use the gwab2014-[your initials here] affinity group)
3. Click on the "Publish"-button. 

Go to the virtual network dashboard on the Azure portal and monitor it as the deployment progresses. When the solution is deployed the `GWAB.Web_IN_0-instance` should appear under "resources" with its assigned IP-address from the DEV-subnet.


#### Create a Point-to-Site VPN

Now that we have a virtual network with resources in it, we want to access those resources from outside of Azure but without having to create public endpoints to them.
We can use VPN to achieve this.

##### Configure new subnets for VPN

We will need to create new address spaces for VPN and Gateway:

1. Go to Azure Management Portal > Network > Configure.
2. Under point-to-site connectivity check the "Configure Point-to-site connectivity"-box.
3. Click on the "Add address space"-button under your DEV-subnet.
4. Provide the following details for VPN- and Gateway-subnets:
	1. Starting IP: 10.0.9.0
	2. CIDIR: /24 (251)
	3. Verify that Address Space is 10.0.9.0/24 and Usable Address Range is 10.0.9.4 - 10.0.9.254. This gives us 250 addresses for VPN-clients and Gateway. 
	4. Create subnet for VPN:
		1. Name: VPN
		2. Starting IP: 10.0.9.0
		3. CIDIR: /25 (123)
		4. Usable Address Range should be 10.0.9.4 - 10.0.9.126
	5. Click on the "Add Gateway Subnet"-button and provide the following:
		1. Starting IP: 10.0.9.128
		2. CIDIR: /29 (3)
		3. Usable Address Range should be 10.0.9.132 - 10.0.9.134 
	6. Click on Save and the bottom of the page and click Yes when prompted.


##### Create Gateway

Wait for the previous changes to apply (about 1 minute). Then click on the "Create Gateway"-button at the bottom of the page to create a Gateway. Select Yes when prompted.

This will take about 15 minutes to complete. Time for coffee!


##### Upload the root certificate

After the gateway has been created we can upload the root certificate which we created earlier. The VPN-client will use this certificate to create a certificate for authentication and to validate the VPN-user's client-certificate on his machine.

1. Go to Certificates on the Virtual Network page and choose "Upload a root certificate".
2. Select the `C:\certs\azure-root-certificate.cer`-file.
3. Click on the Complete-button.


##### Download the VPN-client

1. On the virtual network dashboard you should see two links on the right: "Download the 64-bit Client VPN Package" and "Download the 32-bit Client VPN Package". 
Download the one appropriate for your PC.
2. Double-click on the file and install the client.

Use `certmgr.msc` to verify that you see a certificate which the VPN-client have installed on your PC:
 
1. Go to Trusted Root Certificate Authorities > Certificates
2. In the list you should see a certificate with a name similar to "azuregateway-[GUID]"

Connect with the VPN-client:

1. On your PC, go to `Control Panel\Network and Internet\Network Connections` and right-click on the VPN-client for your virtual network and select "Connect".
2. Click "Connect" when the client opens.
3. Click "Continue".

You should now be connected to your network and have access to all of the resources in it. Look at the dashboard on your virtual network and verify that the number of clients is "1" (might take a minute). Try to ping the `GWAB.Web_IN_0-instance` by using its assigned IP-address.

> If you are not connected then try to re-upload the root-certificate, delete the VPN-client from your PC and download and install it again.


##### (Optional) Distributed VPN-users

Anyone with a certificate and the VPN-client install package can access your network. Find a buddy and see if you can connect to eachothers network.
You will have to install a client-certificate on each users PC so that the VPN-client can authenticate against the root-certificate (under Certificates).

1. Use `certmgr.msc` to find the client-certificate on your PC
2. Right-click and select All Tasks > Export
3. Select "Yes, export the private key" and click Next and Next
4. Provide **Nnug2014!** as the password
5. Save the file as `azure-client-certificate.pfx` in `C:\certs`

Send your buddy the `azure-client-certificate.pfx`-file along with the VPN-client installer-package and have them double-click the pfx-file to install the client-certificate onto their PC. Then they can install the VPN-client and should be able to connect to your network. Have them ping your `GWAB.Web_IN_0-instance` to test connectivity.


Assignment #3: Create a Windows Azure Virtual Machine with Ubuntu 13 and elasticsearch
--------------------------------------------------------------------------------------

We will go through the necessary steps to provision a VM based on a Ubuntu 13 VM-image, place it into our virtual network and use PuTTY to connect to it over SSH.

On this VM we will install Java and elasticsearch, creating our own search cluster.

But before we can create the VM we will need to generate SSH-keys.

### Generate SSH-certificates and keys

#### Generate .pem and .key-files with openssl

We will create a certificate for SSH and then a private key which PuTTY will use for authentication. 

Open up a command-line tool, find `openssl` and run the following:

	# Create private key and pem-file
	openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout c:\certs\azure-vm-private.key -out c:\certs\azure-vm-certificate.pem

    # If you get the error "Unable to load config info from..." when using openssl.exe, try running:
    # set OPENSSL_CONF=[folder containing OpenSSL]\openssl.cnf 
	set OPENSSL_CONF=C:\tools\openssl-0.9.8k_WIN32\openssl.cnf
    # Validate correct path with:
	echo %OPENSSL_CONF%

#### Generate private key for PuTTY

PuTTY uses ppk-files for private keys so we have to convert from `azure-vm-private.key` to `azure-vm-private.ppk` with "PuTTYgen".

1. Run `puttygen.exe` and go to  File->Load private key. 
2. Select the `azure-vm-private.key`-file from `C:\certs`. Click on the "Save private key"-button to save the new file. Select "Yes" for not storing a passphrase. 
3. Name the file `azure-vm-private.ppk` and place it in `C:\certs`. 


### Create the virtual machine

Now that we have the certificate and private keys generated we can then create the VM. We will use the VM-image `b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB`.

Run the following in the Azure command-line tool:
		
	# Deploy an Ubuntu image on an extra small instance in West Europe:
	azure vm create gwab2014-[your initials here]-azure-elasticsearch-cluster \
	
	b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB \
	
	--vm-name [your initials here]-elasticsearch-node0 \
	
	--vm-size extrasmall \

	--ssh 22 \
	
	--ssh-cert "C:\certs\azure-vm-certificate.pem" \

	--virtual-network-name gwab2014-[your initials here]-we-vnet \ (skip if you haven't created a virtual network in Azure)

	--subnet-names DEV \ (skip if you haven't created a virtual network in Azure)
	
	--affinity-group gwab2014-[your initials here] \ (use same group as the one for your virtual network i Azure)

	elasticsearch Password1234#!!
	
	# elasticsearch / Password1234#!! is SSH login/password

	# Example:
	azure vm create gwab2014-hwm-azure-elasticsearch-cluster b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB --vm-name hwm-elasticsearch-node0 --vm-size extrasmall --ssh 22 --ssh-cert "C:\certs\azure-vm-certificate.pem" --virtual-network-name gwab2014-hwm-we-vnet --subnet-names DEV --affinity-group gwab2014-hwm elasticsearch Password1234#!!

The virtual machine is now being provisioned in Azure. We have placed the VM in the same affinity group as our virtual network, and placed it in the DEV-subnet.

Let the provisioning complete before you proceed to the next step. You can follow the progress in the Azure-portal under "Virtual machines". Under "resources" on the virtual network dashboard you should see a new instance appear with assigned IP-address.


### Install and configure elasticsearch

We will connect to our VM using PuTTY and then install elasticsearch.

#### Connect with PuTTY

Provide these settings in PuTTY:

* Session->Host name: `gwab2014-[your initials here]-azure-elasticsearch-cluster`
* Connection->SSH->Auth->"Private key file for authentication": Select the `azure-private.ppk`-file from `C:\certs`. Click "Open" and select "Yes".

**Important**: If you get the error message "Host does not exist" in PuTTY the connect using the VM's Cloud Service IP-address instead, or wait a bit longer as the DNS-address is being created (can take up to 5 minutes)

PuTTY will start the SSH-session in a command window. Provide the user name "elasticsearch". You will now be authenticated with your private key and be connected to the VM.


### Install Java JRE

We need to install Java on the VM as it's a prerequisite for elasticsearch. We will install Java JRE 7 with the following commands:

	# Update all packages
	sudo apt-get update

	# Install Java JRE 7
	# Choose 'Y' if prompted about disk space
	sudo apt-get install openjdk-7-jre-headless

Verify that Java is installed by running the command:

	java -version

> If Java is not installed, try running "sudo apt-get update" again and then try installing Java JRE 7

### Install elasticsearch

Now we are ready to install elasticsearch on our Ubuntu VM. Run the following commands in PuTTY:

	# Download elasticsearch for Debian
	curl -s https://download.elasticsearch.org/elasticsearch/elasticsearch/elasticsearch-1.0.0.deb -o elasticsearch-1.0.0.deb
	
	# Unpack and install
	sudo dpkg -i elasticsearch-1.0.0.deb

	# Add config values with vi/vim ("i" starts editing, past in with a right-click, then ESC + ":x" stores and ends editing)
	sudo vi /etc/elasticsearch/elasticsearch.yml

	# Add to the bottom of the file:
	# Disable replicas for now
	index.number_of_replicas: 0


#### Start elasticsearch automatically as a service

	sudo update-rc.d elasticsearch defaults 95 10
	sudo /etc/init.d/elasticsearch start


#### Troubleshooting

Should an error occur during setup or configuration, or elasticsearch doesn't start, then you can look inside the log-file for more details at `/var/log/elasticsearch/elasticsearch.log`.

Verify that elasticsearch is running by running the following command:

	# 10.0.1.5 is the IP-address our VM has been assigned in the DEV-subnet. 
	# Or use 'localhost' instead.
	curl http://10.0.1.5:9200/ 


#### Open port 80 for queries to elasticsearch

To be able to query elasticsearch we have to create public endpoint on our VM. The endpoint will map to **80** to elasticsearch's default HTTP-listening port of **9200**.

Go to the Azure portal > Virtual machines > [your initials here]-elasticsearch-node1 > endpoints and click "Add". Choose "Add a stand-alone endpoint" and proceed. Provide the following:
		
* Name: HTTP
* Protocol: TCP
* Public port: 9200
* Private port: 9200

and save. Go to *http://gwab2014-[your initials here]-azure-elasticsearch-cluster.cloudapp.net:9200* and verify that you get elasticsearch's cluster details.


#### Install plugins for elasticsearch

There are particularly two plugins for elasticsearch that everyone should use. The first one is called **Head** and gives administrators an overview of documents, indexes, aliases, mappings, system information etc. The other one is called **BigDesk** and is a monitoring tool for administrators for monitoring system events and state. These tools provide you with a great start to begin learning how to use and monitor elasticsearch. Both of them are browser-based so use a browser to navigate to each after the installation.

Use PuTTY and run the following commands to install these plugins:

	# Install plugins Head and BigDesk
	sudo /usr/share/elasticsearch/bin/plugin -install mobz/elasticsearch-head
	
	sudo /usr/share/elasticsearch/bin/plugin -install lukas-vlcek/bigdesk

* You can find *elasticsearch-head* at */_plugin/head* 
* You can find *bigdesk* at */_plugin/bigdesk*


### Index data to your elasticsearch with RSS-rivers

For indexing data into elasticsearch you use "data rivers". These are Java-programs that feeds elasticsearch with data by sending REST-calls at given intervals to elasticsearch's REST-API and creating new documents.

There are several different types of rivers one can use to consume data sources, such as XML, json, RSS, web services, files and more.

For this assignment we will follow [this guide](http://www.pilato.fr/rssriver/) to setup a RSS-river to consume an [RSS-feed](http://www.vg.no/rss/create.php?categories=12,21,20,34,10,164,22,25&keywords=&limit=20)
provided by [vg.no](http://www.vg.no).

Run the following commands in PuTTY:
	
	sudo /usr/share/elasticsearch/bin/plugin -install fr.pilato.elasticsearch.river/rssriver/0.2.0
		
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

	sudo service elasticsearch restart

You have now created a new index called **newspapers** and a new mapping called **page** for all of the **documents** that will be created in the index. The river will take each item it finds in the RSS-feed and create a new document in this index based on the **page**-mapping. The last command configures the river to setup a new RSS-feed to consume.

If you have installed the Head-plugin then you can go to */_plugin/head* and see that documents are being created inside the **newspapers**-index. Hit the blue Refresh-button to the far right to refresh the index as new documents are created.

Assignment #4: Create a cluster of elasticsearch VMs
----------------------------------------------------

### Scale out with more VMs

[See screenshots and the guide this assignment is based on](http://thomasardal.com/running-elasticsearch-in-a-cluster-on-azure/).

We have one VM setup with elasticsearch and serving as a search engine on a single public endpoint on port 9200, but it can't handle too much load. To improve upon our setup we can add more VMs, and then place them under a load-balanced endpoint. This way, all requests coming to *http://gwab2014-[your initials here]-azure-elasticsearch-cluster.cloudapp.net:9200* will be spread evenly to each VM and their internal 9200-endpoints.

#### Create VMs

Now that we have a single VM as an elasticsearch cluster running in Azure it might be useful to scale out to handle large load of queries. This can be scripted with Azure command-line tool. We will scale out from a 1 node cluster to a 3 node cluster by capturing an image of the VM we have running now, and then provision 3 new VMs based on this new vm-image.

In the Azure command-line tool, run the following command:

	# Shutdown the VM
	azure vm shutdown [your initials here]-elasticsearch-node0

	# Capture the VM-image
	azure vm capture [your initials here]-elasticsearch-node0 [your initials here]-elasticsearch-node-image --delete

	# Provision 3 VMs
	FOR %? IN (1 2 3) DO azure vm create gwab2014-[your initials here]-azure-elasticsearch-cluster [your initials here]-elasticsearch-node-image --vm-name [your initials here]-elasticsearch-node%? --vm-size extrasmall --ssh 2%? --ssh-cert "C:\certs\azure-vm-certificate.pem" --virtual-network-name gwab2014-[your initials here]-we-vnet --subnet-names DEV --affinity-group gwab2014-[your initials here] --connect elasticsearch Password1234#!!

Wait 2-4 minutes as each VM is provisioned.


#### Configure endpoints

So we should have 3 VMs: `gwab2014-elasticsearch-node1`, `gwab2014-elasticsearch-node2` and `gwab2014-elasticsearch-node3`. In order for elasticsearch's discovery mechanism to work we will need to add them in a unicast host-list in each of the `elasticsearch.yml`-files on each VM.

First, find the IPs of your VMs. If you've put them into your virtual network then look under "resources" (on the virtual network dashboard) for their IPs. In our case they should be 10.0.1.5, 10.0.1.6 and 10.0.1.7.


##### Add IPs to elasticsearch's configuration

**Important**: Repeat this step for each of your VM.

We will disable multicast UDP-discovery and put the addresses in a fixed unicast host-list.
Connect to the VM with PuTTY (SSH-ports will be 21, 22 and 23). Run the following command in PuTTY:

	sudo vi /etc/elasticsearch/elasticsearch.yml

	# Insert these two lines at the bottom
	discovery.zen.ping.multicast.enabled: false
	discovery.zen.ping.unicast.hosts: ["10.0.1.5", "10.0.1.6", "10.0.1.7"]

	sudo service elasticsearch restart


#### Add endpoints

Now we need to set up a public endpoint for each VM. We will create a load-balanced endpoint on port 9200 for our cloud service in which our 3 VMs reside.

1. Go to the Azure Portal > Virtual Machines > `gwab2014-elasticsearch-node1` > Endpoints
2. Click on "Add".
3. Select "Add a stand-alone endpoint" and click Next.
4. Provide the following:
	1. Name: **ElasticSearch**
	2. Protocol: **TCP**
	3. Public port: **9200**
	4. Private port: **9200**
	5. Select **Create a load-balanced set**
	6. Click Next
5. Provide the following:
	1. Load-balanced set name: **elasticsearch**
	2. Click Complete

For the two other VMs, repeat step 1 and 2. 
1. Then select elasticsearch as an existing load-balanced set and click Next.
2. Provide the same details as on step 4 but ignore the "Reconfigure the load-balanced set".
3. Complete the wizard.

After all of the endpoints are created, navigate to *http://gwab2014-[your initials here]-azure-elasticsearch-cluster.cloudapp.net:9200/_plugin/head* and you should see all 3 nodes in the cluster.

For each request to the public endpoint *http://gwab2014-[your initials here]-azure-elasticsearch-cluster.cloudapp.net:9200* Azure will load-balance traffic and forward it to one of the 3 VMs. 

You've now got a load-balanced elasticsearch cluster running in Azure on Ubuntu 13!

:-)

Remarks
-------

### Availability Sets

Before going into production with this setup, one should place the VMs in an "Availability Set" so that you can ensure a stable environment for the service.

### Different methods of scaling

Our solution is a static method of scaling, by manually editing elasticsearch's unicast host-list. There are other ways of scaling which also should be considered. 

One way is to create a worker role that deploys a full installation with Java and elasticsearch, and edits the elasticsearch.yml-file's unicast host-list during deploy and provides it with all of the instance's IP-addresses. 

Or perhaps try a new [Azure-plugin for elasticsearch](https://github.com/elasticsearch/elasticsearch-cloud-azure) which offers multicast node discovery in Azure. At the present time it requires a Java-keystore SSL-key and certificates, which might be complicated to configure properly and to troubleshoot. There are also challenges with OpenSSL on Windows which seems to sometimes not generate valid key-files. It's definatly something to have a look at as it matures and becomes more stable.

### Maximize performance

Our VMs runs on an **ExtraSmall** VM-size meaning there's a limit to the number of [disks, memory and CPU-cores](http://msdn.microsoft.com/en-us/library/windowsazure/dn197896.aspx) available to the VM. Of course, upgrading to a larger VM-size is a logical next step but this only increases memory and CPU-sizes, not disk-efficiency, even though you get another disk. 
A VM-disk is limited to 500 iops and for a high-performance scenario you want to use more disks so that data is spread across multiple disks. This way the iops-limit won't act as a bottleneck. To enable this for elasticsearch you need to [specify the paths for the data directories](http://www.elasticsearch.org/guide/en/elasticsearch/reference/current/setup-dir-layout.html) in elasticsearch's configuration.



	
 

