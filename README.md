
# work-in-progress: I will remove this comment when done (2017-05-28). Expect it to happen within a few days.


# Building Microsoft Dynamic 365 WebResources with Visual Studio and angular 4

This project demonstrates an easy track to develop Microsoft Dynamic 365 WebResource components with angular 4. The setup has been created on a on-premesis development solution, and you might meet several obstacles if you try to develop against an online solution. The final solution can
however be deploy online or on-premesis, using the normal Dynamic 365 solution deployment method.

## How to use, and where will it end up leaving you.

This guide is a step by step guide for building a very simple Visual Studio Solution that will allow you to create a full angular 4 application that can run inside a Dynamic 365 solution, online or on-premises.

Even though a dynamic web resource is a web application, this project is using a simple command line tool as template. The reason for this is the basic nature of a dynamic 365 web resource. It only allow simple html, javascript, css and image files. Nothing else. So any Visual Studio Web template will add things that are not supported anyway, ex. ASP.NET controllers and more.
Secondly, the deployment model of web resources is also very strict. Files need to be uploaded to Dynamic 365 as WebResources, and eventually they will be deployed as single files (possible in sub folder structure). The angular cli build process is a perfect resource to optimize and prepare as few files as possible to be uploaded. This project will show you how, and even give you the needed code to automate the process. The automation process is included in the command line tool (Deploy).

## The following topics will be covered

1. Creating a solution
1. Create an angular application with angular cli
1. Add the angular application to your visual studio solution
1. Deploy the application to your Dynamic 365 
1. Make your angular cli application work - first in general, secondly with IE.
1. Add a simple typescript xrm service that will allow data access through dynamic 365 WebApi
1. Setup a development environment that allow development and test, directly in Visual Studio, without prior deployment to Dynamic 365

## Creating a solution

This project is a demo reference only, so the best starting point is proberbly not just to download this solution. But in very few steps, you can have your own solution up running.

1. Open visualstudio
1. Create new project, using the template [Other Project Types > Visual Solutions > Blank Solution ], in this guide I assume you are naming i ''''MyAngularSolution''''
1. Add a Command line project to the solution, name it Deploy, because that is what is eventually gonna do.
1. If you wish to use Git or other source control repository, now it is the time to add your solution to source control. Use whatever process you normally use. If you don't add you solution to source control now, you might come in trouble later, because the angular cli will add git source control to your angular project, if the folder is not already under source control.


## Create an angular application with angular cli

If you didn’t already install the angular cli tool, you need to do so now. Follow the guide from this link.

Before you run too fast, just do the installation of angular cli for now. Get back here when you are ready to create a new angular app.

[Angular CLI](https://cli.angular.io/)

Now you have the angular cli tool as part of your development environment. 

Open a command prompt, and navigate the the root of the Deploy project folder. ( de ..whatever.folder is your root ) 

````C:\Projects\MyAngularSolution\Deploy````

```ng new demo```

This will create a sub folder in you solution named demo (give you project whatever name corresponding to your need. It will eventually be used as the root name in your Dynamic 365 WebResource)

Now go back to Visual Studio and add the needed folders to your Visual Studio project:

(Click the Deploy project, use the [Show all files] button in the Solution Explore, hightlight according to below and select Include in Project])


![Add angular app to solution](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/solution-add-angular-application.png)

Just for the sake of confirmation, you can test run your application. This is done by opending a command prompt, navigate to

````C:\Projects\MyAngularSolution\Deploy\Demo````

and enter the following command

```ng server --open```

This will start the ng development server and open a new browser window. You should see something like "app works!" in your browser window.

## Deploy the application to your Dynamic 365

Next step is to deploy the application so it can run under Dynamic 365 as web resource. This is actually quite simple, because the only thing we need to do is to create a build, and then upload the files in the build 
as WebResources in our Dynamic 365 solution. As our project grows in size this can become a time consuming task due to the amount of files that must be uploaded and managed individually. Luckily the dynamic 365 SDK allow us to automate the process.  Below I will take you through the process – step by step, however the Deploy command line tool actually contains all the code for a fully automated process. So if you are a lucky rider, you might just dive directly into the code.

### But first we need to to establish a build process

An angular project is basically just a bunch of html, css and typescript files. Alle these files need to be compiled into a workable web solution to be deployed under Dynamic 365. Dynamic 365 do understand
html and css, but there is no direct support for typescript. The angular cli build process it the perfect tool to prepare files for upload. Below the manual process for doing this.

Open a command line tool and navigate to your angular page folder

````C:\Projects\MyAngularSolution\Deploy\Demo````

```ng build --prod --output-hashing none```

The --prod flag is telling angular to build an optimized version suiteable for production, and the --output-hasning is to tell the build tool not to hash the file names generated. The later flag is really convinient for the
dynamic 365 scenario, because dynamic 365 is alrady hashing all resources before served to the browser, and new names for all files would be a nightmare to manage when uploading the files to Dynamic 365.

As a result of this process, ng build will generate a "dist" folder under the "...\demo" folder, containing all the files needed to run the application:


![Add angular app to solution](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/angular-first-build.png)

As you can see from the folder structure, ng build has create a nice simple structure with very few files. One could think that we can simply upload these to Dynamic 365 one by one. 7 files should not be a big deal. 

But that is not a workable solution. On each build of the application, all files need to be updated, because new modules and more might have been added and more. Secondly, as we start build the application, adding assets and more, it will generate more files. So we wish to automate that part of the process.

The ([Dynamic 365 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=50032)) does contain a solution with a tool (SDK\Tools\WebResourceUtility) to upload a folder including all sub folders and files as web resource, so you could just go with that solution. [XrmToolBox](http://www.xrmtoolbox.com/) also have a nice Web Resource manager that allow you to do the same. Personally
i prefere a process where I can manage, script and automate the process, and the amount of code needed is fairly small, so that is the approach i will demonstrate here.

### Creating a simple deploy tool

This sample is using the 2011 organization service to create and update web resource, including solution references. The advantage of this is that the approach will also work with older versions of Dynamic CRM. 
I did not investigate in details if the methods used here is actually supported by the WebApi, but if that is the case, you might wanna go for that interface instead of using the 2011 OrganizationService.

First of all, to create a command line tool with access to Dynamic 365, you need to include the SDK DLL


![Dynamic 365 DLL to be included](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/xrm-sdk-assemblies.png)

Beside these libraries, my implementation is also using

```
System.Runtime.Serialization
System.ServiceModel
```

Mainly to parse the configuration for uploading the resources to the Dynamic 365 WebResource.

#### How does it work

Let me first explain how it works. In the angular folder (demo) i have place a simple json file named xrm.deploy.json. This file have all the connection details for uploading files to Dynamic 365

Deploy\Demo\xrm.deploy.json

```javascript
[
  {
    "solution": "Angular4Demo",
    "name": "demo",
    "dist": "dist",
    "url": "http://kipon-dev/kip/XRMServices/2011/Organization.svc",
    "user": "auser",
    "password": "#aVerySecretPassword!"
  }
]
```

As you can see, this file contains information about the target server. What solution to hit, witch name to give the application, where to find the IOrganizationService, and how to connect.

Basically the settings is an array. This mean that you can deploy the same code to several 365 instances in one go.

*Solution* is the unique name of the Dynamic 365 solution to target. The solution will be used to lookup the publisher, and from there get the web resource name prefix.

*name* is the unique name to be used as web resource name. This allow you to deploy several angular application in the same Dynamic 365 solution. Each element deployed will be named [publicher-prefix]\_/[name],
in my case kipon\_/demo/what-ever

*dist* is where to find the distribution files. The tool will simply upload all files and create a similar structure within dynamic 365.

url, user, password should be Self-explaining.

Since I have a running Dynamic 365 on my box on the "kipon-dev" domain, i am ready to go:

After i compiled the solution in Visual Studio, I can navigate to the the Deploy/Demo folder and call the deployment tool

```..\bin\debug\Deploy```

![Output from Deploy.exe](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/xrm-deploy-result.png)


And looking within Dynamic 365 solution explore:

![Output from Deploy.exe](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/xrm-deploy-solution-result.png)


#### Behind the scene

The deploy tool is farily simple. It has a Setting.cs that maps 1-1 to the xrm.deploy.json, and the class has a simple static member that assume the file to be configuration file to be placed in current folder.

Then we have a ResourceTypeEnum.cs that simple defines the 10 different types of ressource dynamic 365 supportes. On top of that a simple Extensions.cs file that can map from a file name extension, ex .html to the
corresponding resource type, ex. ResourceTypeEnum.Html.

The import manager have all the logic. The method is: Initially lookup the solution, and from there the publisher to get the CustomizationPrefix to be used on upload. 

Then for each file in the dist folder, lookup and update, or create if new a WebResource with correct name and type. The name and structure in dynamic 365 will exactly correspond to the structure in the dist folder.

If the dist folder contains any files that cannot be mapped to one of the 10 types, the process will throw an exception. 

##### some details - creating a web resource, publish it, and finally attach it to a solution:
```c#
webResource = new Entity
{
    Id = Guid.NewGuid(),
    LogicalName = "webresource"
};
webResource["name"] = resourceName;
webResource["content"] = Convert.ToBase64String(File.ReadAllBytes(file));
webResource["displayname"] = name + ": " + resourceName;
webResource["description"] = "Imported as part of the " + name + " application";
webResource["webresourcetype"] = new Microsoft.Xrm.Sdk.OptionSetValue((int)filename.ToResourceType());
orgService.Create(webResource);

var publishRequest = new PublishXmlRequest
{
    ParameterXml = string.Format("<importexportxml><webresources><webresource>{0}</webresource></webresources></importexportxml>", webResource.Id)
};

// attach new webResource to solution
var request = new Microsoft.Crm.Sdk.Messages.AddSolutionComponentRequest
{
    ComponentType = 61, // Web Resource,
    ComponentId = webResource.Id,
    SolutionUniqueName = solution
};

orgService.Execute(request);
```

##### and if the webresource already exists, simply update it

```c#
if (new FileInfo(file).LastWriteTimeUtc > ((DateTime)webResource["modifiedon"]).ToUniversalTime())
{
    Console.WriteLine("Updating " + resourceName);

    webResource["content"] = Convert.ToBase64String(File.ReadAllBytes(file));
    orgService.Update(webResource);

    var publishRequest = new PublishXmlRequest
    {
        ParameterXml = string.Format("<importexportxml><webresources><webresource>{0}</webresource></webresources></importexportxml>", webResource.Id)
    };

    orgService.Execute(publishRequest);
}

```

Take a look at the ImportManager.cs file for a full view. It is less than 200 lines of code.


## Make your angular cli application work - first in general, secondly with IE.

Now we have alle the components to run the angular 4 application within Dynamic 365, you should target the [publisherprefix]_/name/index.html file whereever you choose to embed your code, in my case
kipon_/demo/index.html

I have used the XrmToolBox site map editor to publish it on its own page directly from the 365 main menu:

But initally the application does not work. I get 404 on all resources. There is a simple reason for this. Angular cli is building a index.html page with a 

```html
<base href="/">
```

Remove that tag from the index.html file, build your application again with the ng build command and finally redeploy using the Deploy tool. Now your application is working within chrome.

![Output from Deploy.exe](https://raw.githubusercontent.com/kip-dk/angular-xrm-webresource/master/Documentation/angular-running-in-dynamic.png)


