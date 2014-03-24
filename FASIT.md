NNUG - Global Windows Azure Bootcamp 2014
=========================================

This is a step-by-step workshop assignment that was given during NNUG's Global Windows Azure Bootcamp 2014 @ Bouvet on 29. March 2014. 


Contents
--------

* *Create* and *deploy* a **Azure Cloud Service** containing a **web role**
* *Create* a **Azure Virtual Network** and invite the **web role** into the network
* *Create* and *configure* **Point-to-Site VPN** to access the  **Azure Virtual Network**
* *Create* an **Azure Virtual Machine** with Debian running an instance of **elasticsearch**


### Before you begin

If you are going to follow all of the steps in this assignment, then make sure you have all of the prerequisites:

* Have a PC with Visual Studio 2012/2013
* Have installed the latest version of Azure SDK
* Have installed the [Azure command-line tool](http://www.windowsazure.com/en-us/develop/nodejs/how-to-guides/command-line-tools/)
* Have installed [OpenSSL for Windows](http://code.google.com/p/openssl-for-windows/downloads/detail?name=openssl-0.9.8k_WIN32.zip&can=2&q=) 
* Have downloaded [PuttyGen](http://the.earth.li/~sgtatham/putty/latest/x86/puttygen.exe), [Putty](http://the.earth.li/~sgtatham/putty/latest/x86/putty.exe) og [PSCP](http://the.earth.li/~sgtatham/putty/latest/x86/pscp.exe) 
* Have an account for the [Azure-portal](http://www.windowsazure.com/)

### About this assignment

This assignment is partially based on a guide at [elasticsearch's website](http://www.elasticsearch.org/blog/azure-cloud-plugin-for-elasticsearch/) for their [Azure-plugin for node discovery](https://github.com/elasticsearch/elasticsearch-cloud-azure). We have adapted this for a developer using a PC instead of a Linux-box, and have introduced a few more tasks as well. 


Assignment #1: Create a Windows Azure Cloud Service
---------------------------------------------------

In this assignment you will create a **Azure Cloud Service** and a **web role**. This website will access an elasticsearch cluster already set-up in Azure and will return search results for news articles from vg.no. 

### Create the Cloud Service with a web role

1. Open Visual Studio and choose File > New project. Choose the Visual C# template > Cloud > Windows Azure Cloud Service:
 - Name: GWAB.Azure
 - Solution: GWAB
 - Click Ok.
2. Choose ASP.NET Web Role and click on the right-arrow to add it into our Cloud Service. Click Ok.
3. Choose the template "MVC" and click on "Change Authentication" and select "No Authentication". Click Ok.

You have now created a Cloud Service with a ASP.net MVC web role. You can now deploy your solution and website to Azure:

1. Right-click on GWAB.Azure-project and choose "Publish".
2. Log in with your Azure-credentials and select you subscription in the list and click Next.
3. Create a new Cloud Service by going to "Cloud Service:" and select "Create New" from the dropdow-menu. Specify "GWAB2014" or equivalent as a name and select location "West Europe". 
4. Click Next and then Publish on the next page.

Your Azure Cloud Service is now being deployed to Azure. To see the deployment in action, go to the Azure-portalen and then to Cloud Services > GWAB2014. When the deploy is complete you can try to access the website by navigating with a browser to [gwab2014.cloudapp.net](http://gwab2014.cloudapp.net).


### Connect to elasticsearch

There's an instance of elasticsearch set up for this workshop in Azure. The web role will be configured to post queries to this search cluster and get results back and show them on a search-results page. We will use NEST as a strongly-typed C#-client for elasticsearch.


#### Install NEST with NuGet

Set Webrole1 as "Default project" and install NEST by running the following command in the Package Manager Console in Visual Studio:

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

In the same folder, create a new model class for the rss-news which NEST will map from a mapping in elasticsearch for the indexed rss-news. Notice the usage of `DataMember` and `Name` to map between the fields in the elasticsearch mapping and properties in the `RssItem`-class.

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

Paste in the following code into the constructor of the `HomeController`-class to connect NEST to elasticsearch i Windows Azure:

	const string elasticsearchEndpoint = "http://gwab2014-elasticsearch-cluster.cloudapp.net";

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
Search for news from vg.no by typing text into the search box and click on "Search". Make sure you have a breakpoint in the "Search"-method so that
you can monitor the execution. For complete insight into the queries that NEST performs monitor the Output-window in Visual Studio.

For direct access to the data that's indexed in elasticsearch at *gwab2014-elasticsearch-cluster.cloudapp.net*, go to [http://gwab2014-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://gwab2014-elasticsearch-cluster.cloudapp.net/_plugin/head/).

Deploy the solution to Azure and test again on [gwab2014.cloudapp.net](http://gwab2014.cloudapp.net) when the deploy is complete.


2. Opprett et Windows Azure Virtual Network
-------------------------------------------

You now have a working **Cloud Service** with a **web role** running in Windows Azure that can run queries against elasticsearch. For now these resources are isolated inside of one 
Cloud Service but a more realistic scenario would be that you will need several environments for development, QA and production. And having all of these 
resources together into one network would make troubleshooting, remoting, direct access to single instances and disk access much easier than clicking through pages inside the Azure portal.
Another argument for placing your resources into a virtual network is that you probably don't want to give access to the Azure-portal to all of your developers who will need
access to a deployed resource. Visual Studio would be a better tool for accessing storage resources, and then using Remote Desktop to access instances inside of the network is a 
much better practice.

So we will use an **Azure Virtual Network** to group, protect and ease access to our instances. 
And by placing the instances into different subnets we can easily separate resources based on environment in which they belong to. And we won't have to open up any public 
endpoints to any of the instances either.

## Create certificates for authentication

We will need to create a root certificate and a derived client certificate for authenticating when we will connect to our network via VPN.

### Generate certificates

Use `makecert` by running "VS2012 x64 Cross Tools Command Prompt" (either 64- or 32-bit version). It's highly recommended that you create a folder 
`C:\certs` in which all of the certificate artifacts can be placed together. 

Run the following commands:

	# Generate root certificate
    makecert -sky exchange -r -n "CN=GWAB2014 Root Certificate" -pe -a sha1 -len 2048 -ss My "c:\certs\azure-root-certificate.cer"

	# Generate a client certificate
	makecert -n "CN=GWAB2014 Client Certificate" -pe -sky exchange -m 96 -ss My -in "GWAB2014 Root Certificate" -is my -a sha1

Verify that you see the root- and client certificate in `certmgr.msc` under Certificates > Current User > Personal > Certificates.


### Create a Virtual Network

Now that you have the neccessary certificates you can create the network.

1. Go to the Azure Management Portal > Networks > and click on "Create a virtual network.
2. Provide the following:
	1. Name: gwab2014-we-vnet
	2. Region: West-Europe
	3. Affinity Group: Create new
	4. Affinity Group Name: gwab2014	
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


### Add webrole1 into the network

You have now created the virtual network and it's ready to accept instances. We will now modify the service configuration of the cloud service project to add webrole1 into the network.

Open the file `ServiceConfiguration.Cloud.cscfg` and paste in the code beneath `</Role>`:

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

Go to the Azure Management Portal and delete your cloud service (can add resources to a network when you're performing an update/upgrade of an existing cloud service).
Choose "Publish" on the "GWAB.Azure"-project, createa a new cloud service (use the same name as before) and click on the "Publish"-button. 

Go to the virtual network dashboard on the Azure portal and monitor it as the deployment progresses. When the solution is deployed the webrole1-instance
should appear under "resources" with its assigned IP-address from the DEV-subnet.


### Create a Point-to-Site VPN

Now that we have a virtual network with resources in it, we want to access those resources from outside of Azure but without having to create public endpoints to them.
We can use VPN to achieve this.

#### Configure new subnets for VPN

We will need to create new address spaces for VPN and Gateway:

1. Go to Azure Management Portal > Network > Configure.
2. Under point-to-site connectivity check the "Configure Point-to-site connectivity"-box.
3. Click on the "Add address space"-button under your DEV-subnet.
4. Provide the following for VPN- og Gateway-subnets:
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


#### Create Gateway

Wait for the previous changes to apply (about 1 minute). Then click on the "Create Gateway"-button at the bottom of the page to create a Gateway. Select Yes when prompted.

This will take about 15 minutes to complete. Time for coffee!


#### Upload the root certificate

After the gateway has been created we can upload the root certificate which we created earlier. The VPN-client will use this to create a certificate for authentication and to validate
the user's client-certificate on his machine.

1. Go to Certificates on the Virtual Network page and choose "Upload a root certificate".
2. Select the `C:\certs\azure-root-certificate.cer`-file.
3. Click on the Complete-button.


#### Download the VPN-client

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

You should now be connected to your network and have access to all of the resources in it. Look at the dashboard on your virtual network and verify that the number
of clients is "1". Try to ping the webrole1-instance by using its assigned IP-address.

> If you are not connected then try to re-upload the root-certificate, delete the VPN-client from your PC and download and install it again.


#### (Optional) Distributed VPN-users

Anyone with a certificate and the VPN-client install package can access your network. Find a buddy and see if you can connect to eachothers network.
You will have to install a client-certificate on each users PC so that the VPN-client can authenticate against the root-certificate (under Certificates).

1. Use `certmgr.msc` to find the client-certificate on your PC
2. Right-click and select All Tasks > Export
3. Select "Yes, export the private key" and click Next and Next
4. Oppgi **Nnug2014!** as the password
5. Save the file as `azure-client-certificate.pfx` in `C:\certs`

Send your buddy the pfx-files along with the VPN-client installer-package and have them double-click the pfx-file to install the client-certificate onto their PC.
Then they can install the VPN-client and should be able to connect to your network. Have them ping your webrole1-instance to test connectivity.


4. Create a Windows Azure Virtual Machine with Ubuntu 13 and elasticsearch
-----------------------------------------------------------

Now that the elasticsearch-team has created an Azure-plugin for elasticsearch with support for multicast node discovery in Azure, it's much more easier than before to have a scalable elasticsearch search cluster in Azure.

We will go through the neccessary steps to provision a VM based on a Ubuntu 13 VM-image, place it into our virtual network and use Putty to connect to it over SSH.
On this VM we will install Java (prerequisite), elasticsearch and the Azure-plugin for elasticsearch.

Before we can create the VM we will need to generate SSH-keys.

## Generate SSH-certificates and keys

### Generate Java keystore file

We will create a certificate for SSH and then a private key which Putty will use for authentication. Lastly we will create the Java keystore key which elasticsearch will need
for authenticating.

Open up a command-line tool, find `openssl` and run the following:

	# Create private key and pem-file
	openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout c:\certs\azure-vm-private.key -out c:\certs\azure-vm-certificate.pem

	# Generate certificate for SSH 
	openssl x509 -outform der -in c:\certs\azure-vm-certificate.pem -out c:\certs\azure-vm-certificate.cer
	
	# Generate a keystore (azurekeystore.pkcs12)
	# Transform private key to PEM format
	openssl pkcs8 -topk8 -nocrypt -in c:\certs\azure-vm-private.key -inform PEM -out c:\certs\azure-vm-private.pem -outform PEM
	
	# Transform certificate to PEM format
	openssl x509 -inform der -in c:\certs\azure-vm-certificate.cer -out c:\certs\azure-vm-certificate.pem

	type c:\certs\azure-vm-certificate.pem c:\certs\azure-vm-private.pem > c:\certs\azure-vm-private.pem.txt
	
	# Use passord: Nnug2014!
	openssl pkcs12 -export -in c:\certs\azure-vm-private.pem.txt -out c:\certs\azurekeystore.pkcs12 -name azure -noiter -nomaciter
	
    # If you get the error "Unable to load config info from..." when using openssl.exe, try running:
    # set OPENSSL_CONF=[folder containing OpenSSL]\openssl.cnf 
	set OPENSSL_CONF=C:\tools\openssl-0.9.8k_WIN32\openssl.cnf
    # Validate correct path with:
	echo %OPENSSL_CONF%

### Generate private key for Putty

Putty uses ppk-files for private keys so we have to convert from `azure-vm-private.key` to `azure-vm-private.ppk` with "puttygen".

1. Run "Puttygen.exe" and go to  File->Load private key. 
2. Select the `azure-vm-private.key`-file from `C:\certs`. Click on the "Save private key"-button to save the new file. Select "Yes" for not storing a passphrase. 
3. Name the file `azure-vm-private.ppk` and place it in `C:\certs`. 


## Create the virtual machine with Ubunu 13

Now that we have the certificate and private keys generated we can then create the VM. We will use the VM-image `b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB`.
Run the following in the Azure command-line tool:

azure vm create azure-elasticsearch-cluster b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB --vm-name myesnode1 --location "West Europe" --vm-size extrasmall --ssh 22 --ssh-cert "C:\certs\azure-vm-certificate.pem" --virtual-network-name gwab2014-we-vnet3 --subnet-names DEV elasticsearch Password123#!!
		
	# Deploy an Ubuntu image on an extra small instance in West Europe:
	azure vm create azure-elasticsearch-cluster \
	
	b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB \
	
	--vm-name myesnode1 \
	
	--vm-size extrasmall \

	--ssh 22 \
	
	--ssh-cert "C:\certs\azure-vm-certificate.pem" \

	--virtual-network-name gwab2014-we-vnet \ (skip if you haven't created a virtual network in Azure)

	--subnet-names DEV \ (skip if you haven't created a virtual network in Azure)
	
	--affinity-group gwab2014 \ (use same group as the one for your virtual network i Azure)

	elasticsearch Password1234#!!
	
	# elasticsearch / Password1234#!! is SSH login/password

	# Example:
	azure vm create azure-elasticsearch-cluster b39f27a8b8c64d52b05eac6a62ebad85__Ubuntu-13_10-amd64-server-20130808-alpha3-en-us-30GB --vm-name myesnode1 --vm-size extrasmall --ssh 22 --ssh-cert "C:\certs\azure-vm-certificate.pem" --virtual-network-name gwab2014-we-vnet --subnet-names DEV --affinity-group gwab2014 elasticsearch Password123#!!

The virtual machine is now being provisioned in Azure. We have placed the VM in the same Affinity Group as our virtual network, and placed it in the DEV-subnet.

Let the provisioning complete before you proceed to the next step. You can follow the progress in the Azure-portal under "Virtual machines".


### Install and configure elasticsearch

We will connect to our VM using Putty and then install elasticsearch.

#### Connect with Putty

Provide these settings in Putty:

* Session->Host name: `azure-elasticsearch-cluster.cloudapp.net`
* Connection->SSH->Auth->"Private key file for authentication": Select the `azure-private.ppk`-file from `C:\certs`. Click "Open" and select "Yes".

**Important**: If you get the error message "Host does not exist" in Putty the connect using the VM's Cloud Service IP-address instead, or wait a bit longer as the DNS-address is being created (can take up to 5 minutes)

Putty will start the SSH-session in a command window. Provide the user name "elasticsearch". You will now be authenticated with your private key and be connected to the VM.


#### Copy over the Java keystore file with Putty

We need to copy over the Java keystore-file `azurekeystore.pkcs12` from `C:\certs` to `/home/elasticsearch` on our Ubuntu VM. 

Open up a new command line prompt on your PC and find `pscp.exe`. Run the following command:

	# Copies Java keystore-file fra C:\certs\ to /home/elasticsearch
	pscp -i "C:\certs\azure-vm-private.ppk" "C:\certs\azurekeystore.pkcs12" elasticsearch@azure-elasticsearch-cluster.cloudapp.net:/home/elasticsearch

*Choose 'Y' if your're prompted about storing host key in cache*

Verify the file copy by running: 

	# Get files in /home/elasticsearch
	ls /home/elasticsearch

### Install Java JRE

We need to install Java on the VM as it's a prerequisite for elasticsearch. We will install Java JRE 7 with the following commands:

	# Update all packages
	sudo apt-get update

	# Install Java JRE 7
	sudo apt-get install openjdk-7-jre-headless

*Choose 'Y' if prompted about disk space*

Verify that Java is installed by running the command:

	java -version

> If Java is not installed, try running "sudo apt-get update" again and then try installing Java JRE 7

### Install elasticsearch

Now we are ready to install elasticsearch on our Ubuntu VM. Run the following commands in Putty:

	# Download elasticsearch for Debian
	curl -s https://download.elasticsearch.org/elasticsearch/elasticsearch/elasticsearch-1.0.0.deb -o elasticsearch-1.0.0.deb
	
	# Unpack and install
	sudo dpkg -i elasticsearch-1.0.0.deb

#### Install and configure the Azure-plugin for elasticsearch

Use Putty and run the following commands:

	# Stopp elasticsearch in case it's running
	sudo service elasticsearch stop

	# Install Azure-plugin for elasticsearch 
	sudo /usr/share/elasticsearch/bin/plugin -install elasticsearch/elasticsearch-cloud-azure/2.1.0

	# Add config values with vi/vim ("i" starts editing, ESC + ":x" stores and ends editing)
	sudo vi /etc/elasticsearch/elasticsearch.yml

Add the following at the bottom of the .yml-file:

	cloud.azure.keystore: <path to keystore-file>
	cloud.azure.password: <password for keystore-file>
	cloud.azure.subscription_id: <your Azure subscription-id>
	cloud.azure.service_name: <elasticsearch cluster name>
	cloud.discovery.type: azure

Example:

	cloud.azure.keystore: /home/elasticsearch/azurekeystore.pkcs12
	cloud.azure.password: Nnug2014!
	cloud.azure.subscription_id: 78846242-9c0e-47b5-b157-e9727c7599c7
	cloud.azure.service_name: azure-elasticsearch-cluster
	cloud.discovery.type: azure

#### Automatic start of elasticsearch as service

	sudo update-rc.d elasticsearch defaults 95 10
	sudo /etc/init.d/elasticsearch start


#### Troubleshooting

Should an error occurr during setup or configuration, or elasticsearch doesn't start, then you can look inside the logfile for more details at `/var/log/elasticsearch/elasticsearch.log`.

Verify that elasticsearch is running by running the following command:

	curl http://10.0.1.5:9200/ # 10.0.1.5 is the IP-address our VM has been assigned in the DEV-subnet. Or use localhost instead.


#### Open port 80 for queries

To be able to query elasticsearch we have to create public endpoint on our VM. The endpoint will map to **80** to elasticsearch's default http-listening port of **9200**.

Go to the Azure portal > Virtual machines > myesnode1 > endpoints and click "Add". Choose "Add a stand-alone endpoint" and proceed. Provide the following:
		
* Name: HTTP
* Protocol: TCP
* Public port: 80
* Private port: 9200

and save. Go to [http://azure-elasticsearch-cluster.cloudapp.net/](http://azure-elasticsearch-cluster.cloudapp.net/) and verify that you get elasticsearch's cluster details.


#### (Valgfritt) Install plugins for elasticsearch

There are particularily two plugins for elasticsearch that everyone should install. 
The first one is called **Head** and gives administrators an overview of documents, indexes, aliases, mappings, system information etc.
The other one is called **BigDesk** and is a monitoring tool for administrators for monitoring system events and state.

Use Putty and run the following commands to install these plugins:

	sudo service elasticsearch stop

	# Install plugins Head and BigDesk
	sudo /usr/share/elasticsearch/bin/plugin -install mobz/elasticsearch-head
	
	sudo /usr/share/elasticsearch/bin/plugin -install lukas-vlcek/bigdesk

	sudo service elasticsearch start

elasticsearch-head now ready at [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) 
and bigdesk at [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/bigdesk/).


#### (Valgfritt) Skale your elasticsearch-VM

Now that we have en elasticsearch-cluster running in Azure it might be useful to scale out to handle large load of queries.
This can be scripted with Azure command-line tool. We will scale out from a 1 node cluster to a 3 node cluster by capturing an image of the VM we have running now, and then provision 3 new VMs based on this vm-image.

In the Azure command-line tool, run the following command:

	# Shutdown the VM
	azure vm shutdown myesnode1
	
	# Capture the VM-image
	azure vm capture myesnode1 esnode-image --delete
	
	# Provision 3 VMs
	FOR %? IN (1 2 3) DO azure vm create azure-elasticsearch-cluster esnode-image --vm-name myesnode-%? --vm-size extrasmall --ssh 2%? --ssh-cert "C:\certs\azure-vm-certificate.pem" --virtual-network-name gwab2014-we-vnet --subnet-names DEV --affinity-group gwab2014 --connect elasticsearch Password1234#!!

Wait 2-4 minutes and then go to http://azure-elasticsearch-cluster.cloudapp.net/. Verify that you can see cluster details about the elasticsearch cluster.
Then go to [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) (if you have installed the Head-plugin for elasticsearch) and verify that you see 3 nodes.


#### Index data to your elasticsearch with RSS-rivers

For indexing data into elasticsearch you use "data rivers". These are Java-programs that feeds elasticsearch with data by sending REST-calls at given intervals and creating new documents.
There are alot of different types of rivers one can use (xml, json, rss, web services, files etc.)

For this assignment we will follow [this guide](http://www.pilato.fr/rssriver/) to setup a rss-river to consume an [rss-feed](http://www.vg.no/rss/create.php?categories=12,21,20,34,10,164,22,25&keywords=&limit=20)
provided by vg.no.

Run the following commands in Putty:

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

You have now created a new index **newspapers** and a new mapping **page** for all of the *documents* that will be created in the index.
The riveren will put each rss-object it finds in the rss-feed as a new document in this index. The last command configures the river to setup a new rss-feed to consume.

If you have installed the Head-plugin then you can go to [http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/](http://azure-elasticsearch-cluster.cloudapp.net/_plugin/head/) 
and see that documents are being created inside the **newspapers**-index. Hit the blue Refresh-button to refresh the index as new documents are created.