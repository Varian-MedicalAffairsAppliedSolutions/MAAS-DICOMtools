# DicomTools

DicomTools is built using .NET 8, which may need to be installed.
It uses [https://github.com/fo-dicom/fo-dicom](fo-dicom) library to handle DICOM communication and files.
At the moment there's no dependency on ESAPI. All commands, except search, create a reference tree having RT-Plans as root elements.
Data not connected to any plan is managed as well and called unconnected. All commands handle multiple patient's data, except retrieve.


## Usage
    Description:
      DicomTools contains 4 different Dicom tools.

    Usage:
      DicomTools [command] [options]

    Options:
      --version       Show version information
      -?, -h, --help  Show help and usage information

    Commands:
      retrieve  Retrieve patient data using Q/R.
      store     Store patient data using C-STORE.
      search    Search dicom tag values from given path recursive.
               Examples:
                List all where PatientId is Phantom-1 and show modality:
                  --tag "(0010,0020)=Phantom-1" --tag "(0008,0060)=?" --path X:\Data --searchPattern *.dcm --showStatistics
               List all treatment unit names:
                  --tag "(300A,00B0)/(300A,00B2)=?" --path X:\Data --searchPattern RP*.dcm --showStatistics
      show      Shows plans and related data as a tree from given path recursive.

## Retrieve

    Description:
      Retrieve patient data using Q/R.

    Usage:
      DicomTools retrieve [options]

    Options:
      --patientId <patientId> (REQUIRED)    Id of the patient to retrieve data.
      --planId <planId>                     Id of the plan to retrieve data.
      --onlyApprovedPlans                   Retrieve only approved plans.
      --newPatientId <newPatientId>         New patient id for the saved data.
      --newPatientName <newPatientName>     New patient name for the saved data.
      --anonymize                           Anonymize all the saved data.
      --path <path> (REQUIRED)              Path where to export files.
      --showTree                            Shows the retrieved data as a tree.
      --hostName <hostName> (REQUIRED)      Name of the Dicom Service host.
      --hostPort <hostPort> (REQUIRED)      Port number of the Dicom Services configuration.
      --callingAet <callingAet> (REQUIRED)  AET of the sender.
      --calledAet <calledAet> (REQUIRED)    AET of the Dicom Services.
      -?, -h, --help                        Show help and usage information

Retrieve command calls DICOM SCP (like Varian Dicom DB service) and uses DICOM Query/Retrieve to fetch all DICOM data of the given patient.
It works so that it fetches all studies into a temporary folder and then picks wanted data from there.
Possible anonymization happens when copying files from temporary folder to given **--path**.
Temporary folder is deleted when all data has been processed.
If anonymization option is used, **--newPatientId** and **--newPatientName** needs to be given.

## Store

    Description:
      Store patient data using C-STORE.

    Usage:
      DicomTools store [options]

    Options:
      --path <path> (REQUIRED)              Path where to search for files to send to SCP.
      --searchPattern <searchPattern>       File search pattern, for example *.dcm. [default: *.*]
      --showMessages                        Controls whether status messages are shown or not. [default: True]
      --statusFileName <statusFileName>     Name of the file containing list of files stored. Storing of listed files will
                                            be skipped.
      --machineMapping <machineMapping>     MachineMapping, like A=B C=D
      --defaultMachines <defaultMachines>   DefaultMachines, like RDS=HALCYON 23EX=D
      --hostName <hostName> (REQUIRED)      Name of the SCP host.
      --hostPort <hostPort> (REQUIRED)      Port number of the SCP configuration.
      --callingAet <callingAet> (REQUIRED)  AET of the sender.
      --calledAet <calledAet> (REQUIRED)    AET of the SCP.
      -?, -h, --help                        Show help and usage information

In some cases, like specified treatment unit not found, store command stops.
If you specify **--statusFileName**, all successfully stored files will be listed there. After fixing the issue,
like specifying correct treatment unit mapping using **--machineMapping** option,
you can retry the command and it will continue from last stored file.

Sometimes treatment unit name is taken away from RT-PLAN.
In that case you can use **--defaultMachines** option to specify treatment unit for machine model found from RT-PLAN.

## Search

    Description:
      Search dicom tag values from given path recursive.
      Examples:
      List all where PatientId is Phantom-1 and show modality:
        --tag "(0010,0020)=Phantom-1" --tag "(0008,0060)=?" --path X:\Data --searchPattern *.dcm --showStatistics
      List all treatment unit names:
        --tag "(300A,00B0)/(300A,00B2)=?" --path X:\Data --searchPattern RP*.dcm --showStatistics

    Usage:
      DicomTools search [options]

    Options:
      --tag <tag> (REQUIRED)           Dicom tags to search in a format (gggg,eee)=value.
                                   For example --tag (0010,0020)=PatientId --tag (0008,0060)=CT.
      --path <path> (REQUIRED)         Path where to search for files including given tag.
      --searchPattern <searchPattern>  File search pattern, for example *.dcm. [default: *.*]
      --showStatistics                 Show statistics
      --showOnlyDirectories            List only directories, not files.
      -?, -h, --help                   Show help and usage information

Search command is useful, when you have a lot of DICOM data in some folder and need to find some files based on tag values.

## Show

    Description:
      Shows plans and related data as a tree from given path recursive.

    Usage:
      DicomTools show [options]

    Options:
      --path <path> (REQUIRED)             Path where to search for files.
      --searchPattern <searchPattern>      File search pattern, for example *.dcm. [default: *.*]
      --format <flat|tree> (REQUIRED)      Either flat list or tree. [default: tree]
      --defaultMachines <defaultMachines>  DefaultMachines, like RDS=HALCYON 23EX=D
      -?, -h, --help                       Show help and usage information

Show command goes thru all files in given **--path** recursively and creates a reference tree from all
files found and shows the result as a flat list or as a tree (default).

## Configuration
You can configure various settings in configuration file **appsettings.json**.

#### Logging
Currently all logs are only shown in console. You can change the log level in appsettings.json to get more information if something is not working as expected.

#### SCP
Retrieve command creates a SCP-service in separate thread for handling in-coming C-STORE requests.
TCP/IP port number can be set in appsettings.json. Default port number is 104.

#### Firewall configuration
DicomTools communicate with SCP using TCP/IP port defined in parameters. That port needs to be open from client to the SCP server.
As retrieve command uses query/retrieve and DICOM C-MOVE to tell SCP to send data to DicomTools SCU using DICOM C-STORE.
Configured TCP/IP port needs to be open **from** SCP to SCU.

#### Anonymization
Anonymization options can be changed in appsettings.json. More information about the anynomization in next chapter .

## Anonymization

Anonymization options are set in configuration file. Default is to use similar options as DCIE uses by default.

**AriaSecurityProfile.csv** contains list of anonymization related tags and what to do with the tag values.

**BasicSecurityProfile.csv** is according to DICOM standard basic security profile. Note that if you use it, you can't import data into ARIA as
many needed tags are removed/cleared.

More information here: [DICOM PS3.15 2024e - Security and System Management Profiles](https://dicom.nema.org/medical/dicom/current/output/chtml/part15/chapter_e.html).

```
Example (default settings): 
  "DicomAnonymizer": {
    "SecurityProfileFileName": "AriaSecurityProfile.csv",
    "SecurityProfileOptions": [
      "CleanDesc",
      "RetainDeviceIdent"
    ]
  }
```