# Adjudications

## Description: 
Processes adjudication files by directory path.

## Nuget packages and other dll's utilized
* CsvHelper (https://www.nuget.org/packages/CsvHelper/)
* log4net (https://www.nuget.org/packages/log4net/)
* MySQL.Data (https://www.nuget.org/packages/MySql.Data/)
* Suitability.dll (https://github.com/GSA/Adjudications/blob/master/references/Suitability.dll)
* GSA.sftp.Libraries.Utilities (https://github.com/GSA/Adjudications/blob/master/references/Gsa.Sftp.Libraries.Utilities.dll)

## Initial Setup
### Default config setup

The repository may contains an app config that points to external config files. These external config files not controlled by version control will need to be created and configured prior to running the application. The files required and the default configuration can be found below. For those on the development team, additional details can be found in the documentation on the google drive in the GIT team drive.


 * **Things to do before your first commit**
   * Make a new branch for development. All pre-existing branches are protected and cannot be pushed to directly.
   * You can publish a new branch and do pull requests to have your changes incorporated into the project.
   * Once you have created a new branch you will need to create the config files. (see below for more info on this)
   * Default version of these files are provided in the repo with the .example extension
   * Copy these files into the project **bin\Debug folder** and change the extension to .config using the previous filename
   * Or create new files that contain the code as seen below and place them in the **bin\Debug folder**
   * Do not push your config files to the repository. Pull requests that include these files will be rejected.
 
 * **Current config files that will need to be added.**
   * ConnectionStrings.config
   * AppSettings.config
 
* **Default settings for these files will follow this line**
 
   * **ConnectionStrings.config file should contain the following lines.** 
    ~~~ xml
    <connectionStrings>
	    <add name="GCIMS" connectionString="server=server; port=port; user id=user; password=pw;persist security info=True;database=db; pooling=true;" />
    </connectionStrings>
    ~~~

   * **AppSettings.config should contain the following lines.**
  ~~~ xml
  <appSettings>
    <add key="DEBUGMODE" value="false" />
    <add key="COLUMNCOUNT" value="integer_value" />
    <add key="SMTPSERVER" value="smtp_server" />
    <add key="DEFAULTSUBJECT" value="default_subject" />
    <add key="SUMMARYEMAIL" value="email_address" />
    <add key="DEFAULTEMAIL" value="email_address" />
    <add key="ONBOARDINGLOCATION" value="path"/>
    <add key="EMAILTEMPLATESLOCATION" value="path" />
    <add key="SUMMARYTEMPLATE" value="File_Path\Summary.html" />
    <add key="REGIONALXMLLOCATION" value="File_Path\RegionalEMails.xml" />
    <add key="ADJUDICATIONPRODUCTIONFILELOCATION" value="path" /> 
    <add key="ADJUDICATIONDEBUGFILELOCATION" value="path" />
    <add key="ADJUDICATIONSSUMMARY" value="File_Path\AdjudicationSummary.csv" />
    <add key="SUMMARYBACKUPLOCATION" value="File_Path\SummaryBackups\" />
    <add key="FASEMAIL" value="email_address"/>
    <add key="CHILDCAREEMAIL" value="email_address"/>
    <add key="Salt" value="SaltHere" />
    <add key="EPass" value="EPassHere" />
    <add key="ClientSettingsProvider.ServiceUri" value="" />
  </appSettings>
  ~~~
  
  ***
  
## Usage
Batch processing based on directory in app settings. Place files to be processed in the indicated folder and start the application.

## Contributing
Fork this repository, make changes in your fork, and then submit a pull-request, remembering not to upload any system specific configuration files, PII, or sensitive data of any type. 

## Credits
GSA
