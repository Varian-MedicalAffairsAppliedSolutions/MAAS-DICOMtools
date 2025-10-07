[README.md](https://github.com/user-attachments/files/22733578/README.md)
# MAAS-DICOMtools: Batch Processing Suite for DICOM Operations

**Version 2.0** | Educational/Research Use Only | **NOT VALIDATED FOR CLINICAL USE**

A comprehensive DICOM batch processing solution featuring integrated patient list management, web-based interface, and powerful command-line tools for healthcare data operations.

## Overview

MAAS-DICOMtools provides a complete workflow for batch DICOM operations:

1. **ESAPIPatientBrowser** - Build patient lists from Eclipse TPS with advanced search capabilities
2. **DICOMTools Web Interface** - Modern browser-based UI for batch operation management
3. **DicomTools CLI** - Powerful .NET 8 command-line tools for DICOM operations

### System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Eclipse TPS (ESAPI)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              ESAPIPatientBrowser (WPF Application)              │
│  • Smart patient search with autocomplete                       │
│  • Advanced search (35+ criteria)                               │
│  • Cumulative list building                                     │
│  • Plan selection & filtering                                   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ JSON Export / Handoff Files
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│            DICOMTools Web Interface (HTML/JS)                   │
│  • Import patient lists                                         │
│  • Multi-patient batch operations                               │
│  • Settings & configuration management                          │
│  • Connectivity testing suite                                   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ Generate Batch Files
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               DicomTools CLI (.NET 8)                           │
│  • retrieve - Query/Retrieve patient data                       │
│  • store - C-STORE to PACS                                      │
│  • search - Tag-based file search                               │
│  • show - Display DICOM tree structures                         │
│  • Multi-patient support (semicolon-separated IDs)             │
└─────────────────────────────────────────────────────────────────┘
```

## Components

### 1. ESAPIPatientBrowser (Patient List Builder)

A modern WPF application for browsing Eclipse patient databases and creating batch patient lists.

**Key Features:**
- **Smart Patient Search** - Real-time autocomplete, search by ID/Name
- **Advanced Deep Search** - Search by PTV, dose, structures, machines, and 35+ criteria
- **Cumulative List Building** - Add patients sequentially to build comprehensive lists
- **Plan Selection** - Select specific plans per patient with bulk selection tools
- **JSON Export/Import** - Save and load patient lists
- **Direct Web UI Integration** - One-click launch of DICOMTools web interface with patient data

**Requirements:**
- Varian Eclipse TPS (v15.6, 16.1, 17.0+) with ESAPI
- .NET Framework 4.5.2+
- MahApps.Metro and Newtonsoft.Json

**Documentation:** See [ESAPIPatientBrowser/README.md](../ESAPIPatientBrowser/README.md)

---

### 2. DICOMTools Web Interface

Modern HTML5/JavaScript interface for managing DICOM batch operations.

**Key Features:**
- **Import Patient Lists** - Load JSON from ESAPIPatientBrowser
- **Multi-Patient Operations** - Process multiple patients in single batch
- **Batch File Generation** - Create Windows batch files for automated execution
- **Settings Management** - Configure ports, server settings, machine mappings
- **Connectivity Testing** - Network, DICOM connection, port listening diagnostics
- **Form Persistence** - Auto-save all settings for seamless workflow
- **Modern UI** - Dark theme with Teal/Orange accent colors

**Access:** Open `DicomTools/UI/public/index.html` in any modern browser

---

### 3. DicomTools CLI

Powerful .NET 8 command-line tools for DICOM operations using the [fo-dicom](https://github.com/fo-dicom/fo-dicom) library.

**Key Features:**
- **Multi-Patient Support** - Process multiple patients with semicolon-separated IDs
- **Query/Retrieve** - DICOM C-MOVE/C-GET operations
- **Store** - C-STORE to PACS with machine mapping
- **Search** - Tag-based recursive file search
- **Show** - Display DICOM hierarchies as trees
- **Anonymization** - ARIA-compatible security profiles
- **Flexible Configuration** - JSON-based settings

**Requirements:**
- .NET 8 Runtime
- Network access to DICOM services (PACS/Eclipse)

---

## Quick Start

### Step 1: Launch ESAPIPatientBrowser

**From Eclipse:**
1. Run `Scripts → ESAPIPatientBrowserLauncher`
2. Application launches with Eclipse integration

**Standalone:**
1. Run `ESAPIPatientBrowser.exe` directly
2. Manual patient search available

### Step 2: Build Patient List

1. **Search for Patients:**
   - Type Patient ID or Name (autocomplete suggestions appear)
   - Use date range filters
   - Use "Advanced Search" for deep filtering by plan criteria

2. **Select Plans:**
   - Click patient row to view plans
   - Click "Select Plans" button to load treatment plans
   - Check individual plans or use bulk selection buttons

3. **Export to Web UI:**
   - Click "Open DICOMTools Web UI" button
   - Patient list automatically loaded in browser

### Step 3: Generate Batch Operations

1. **Review Patient List** in DICOMTools web interface
   - Multi-patient indicator shows count
   - Patient IDs displayed as semicolon-separated list

2. **Configure Operation:**
   - Select operation: Export, Import, Search, or Show
   - Set paths and DICOM server settings
   - Configure machine mappings if needed

3. **Generate Batch File:**
   - Click "Run Export" (or other operation button)
   - Choose save location for batch file
   - Batch file ready to execute

### Step 4: Execute Batch Operation

1. **Run Batch File:**
   - Double-click the generated `.bat` file
   - Progress displayed in console window
   - Multi-patient operations show per-patient results

2. **Review Results:**
   - Check export directory for patient data
   - Each patient has separate subfolder
   - Error messages shown for any failures

---

## DicomTools CLI Usage

### Multi-Patient Retrieve Example

```bash
DicomTools.exe retrieve --patientId "12345;67890;11111" --path "C:\Temp\Export" --hostName "localhost" --hostPort "51402" --callingAet "AKKI" --calledAet "DD_Eclipse"
```

**Result:** Three patients retrieved into separate subdirectories:
- `C:\Temp\Export\12345\`
- `C:\Temp\Export\67890\`
- `C:\Temp\Export\11111\`


### Basic CLI Usage

```
      DicomTools [command] [options]
```

**Available Commands:**
- `retrieve` - Retrieve patient data using DICOM Query/Retrieve
- `store` - Store patient data using DICOM C-STORE
- `search` - Search DICOM tag values from given path
- `show` - Display plans and related data as a tree

**Global Options:**
- `--version` - Show version information
- `-?, -h, --help` - Show help and usage information

**Tip:** Use DICOMTools Web Interface to generate batch files instead of typing commands manually!

---

## Command Reference

### Retrieve Command

Retrieve patient data from PACS/Eclipse using DICOM Query/Retrieve.

**Usage:**
```bash
DicomTools retrieve --patientId <id> --path <path> --hostName <host> --hostPort <port> --callingAet <aet> --calledAet <aet>
```

**Required Options:**
- `--patientId <patientId>` - Patient ID (supports semicolon-separated list for multi-patient: "ID1;ID2;ID3")
- `--path <path>` - Export directory path
- `--hostName <hostName>` - DICOM service host (e.g., "localhost" or IP address)
- `--hostPort <hostPort>` - DICOM service port (e.g., "51402")
- `--callingAet <callingAet>` - Calling Application Entity Title
- `--calledAet <calledAet>` - Called Application Entity Title (PACS/Eclipse AET)

**Optional Options:**
- `--planId <planId>` - Retrieve specific plan only
- `--onlyApprovedPlans` - Retrieve only approved plans
- `--newPatientId <newPatientId>` - New patient ID for anonymization
- `--newPatientName <newPatientName>` - New patient name for anonymization
- `--anonymize` - Anonymize all exported data
- `--showTree` - Display retrieved data as a tree structure

**How It Works:**
1. Queries DICOM SCP (PACS/Eclipse) for patient studies
2. Retrieves all DICOM data to temporary folder
3. Filters and processes data according to options
4. Copies to specified path with optional anonymization
5. Cleans up temporary files

**Multi-Patient Example:**
```bash
DicomTools retrieve --patientId "PAT001;PAT002;PAT003" --path "C:\Export" --hostName "192.168.1.100" --hostPort "51402" --callingAet "MYAPP" --calledAet "PACS"
```

**Note:** If using anonymization, both `--newPatientId` and `--newPatientName` are required.

### Store Command

Store DICOM files to PACS/Eclipse using C-STORE.

**Usage:**
```bash
DicomTools store --path <path> --hostName <host> --hostPort <port> --callingAet <aet> --calledAet <aet>
```

**Required Options:**
- `--path <path>` - Source directory containing DICOM files
- `--hostName <hostName>` - PACS/SCP host address
- `--hostPort <hostPort>` - PACS/SCP port number
- `--callingAet <callingAet>` - Calling Application Entity Title
- `--calledAet <calledAet>` - Called Application Entity Title (PACS AET)

**Optional Options:**
- `--searchPattern <pattern>` - File search pattern (default: `*.*`, recommended: `*.dcm`)
- `--showMessages` - Display status messages (default: `true`)
- `--statusFileName <filename>` - Resume file tracking successfully stored files
- `--machineMapping <mapping>` - Machine name mappings (e.g., "TrueBeam1=TB1 Halcyon=HAL")
- `--defaultMachines <mapping>` - Default machines by model (e.g., "RDS=HALCYON 23EX=TRUEBEAM")

**Machine Mapping:**

If treatment units are named differently in source and target systems:

```bash
DicomTools store --path "C:\Export" --machineMapping "OldName1=NewName1 OldName2=NewName2" --hostName "pacs.hospital.com" --hostPort "4242" --callingAet "IMPORT" --calledAet "PACS"
```

**Resume Functionality:**

Use `--statusFileName` to resume interrupted imports:

```bash
# First attempt (may fail mid-process)
DicomTools store --path "C:\Export" --statusFileName "C:\import_status.txt" --hostName "pacs" --hostPort "4242" --callingAet "IMPORT" --calledAet "PACS"

# Fix any issues (e.g., add machine mapping)
# Second attempt resumes from last successful file
DicomTools store --path "C:\Export" --statusFileName "C:\import_status.txt" --machineMapping "OldTB=NewTB" --hostName "pacs" --hostPort "4242" --callingAet "IMPORT" --calledAet "PACS"
```

**Tip:** Use DICOMTools web interface dynamic machine mapping for easy configuration!

### Search Command

Search for DICOM files by tag values recursively through directory structures.

**Usage:**
```bash
DicomTools search --tag <tag_query> --path <path> [options]
```

**Required Options:**
- `--tag <tag>` - DICOM tag query in format `(gggg,eeee)=value`
  - Use `?` as value to display tag contents
  - Multiple `--tag` options can be specified
- `--path <path>` - Root directory to search

**Optional Options:**
- `--searchPattern <pattern>` - File search pattern (default: `*.*`, recommended: `*.dcm`)
- `--showStatistics` - Display statistics summary
- `--showOnlyDirectories` - List only directories, not individual files

**Examples:**

Find all files for a specific patient and show modality:
```bash
DicomTools search --tag "(0010,0020)=Phantom-1" --tag "(0008,0060)=?" --path "X:\Data" --searchPattern "*.dcm" --showStatistics
```

List all treatment unit names from RT-Plans:
```bash
DicomTools search --tag "(300A,00B0)/(300A,00B2)=?" --path "X:\Data" --searchPattern "RP*.dcm" --showStatistics
```

Find all CT images and show their series descriptions:
```bash
DicomTools search --tag "(0008,0060)=CT" --tag "(0008,103E)=?" --path "C:\DICOM" --searchPattern "*.dcm"
```

**Use Case:** Quickly locate specific DICOM data in large file collections without loading into Eclipse.

### Show Command

Display DICOM plan hierarchies and relationships as a tree structure.

**Usage:**
```bash
DicomTools show --path <path> --format <tree|flat> [options]
```

**Required Options:**
- `--path <path>` - Root directory containing DICOM files
- `--format <tree|flat>` - Output format (default: `tree`)

**Optional Options:**
- `--searchPattern <pattern>` - File search pattern (default: `*.*`, recommended: `*.dcm`)
- `--defaultMachines <mapping>` - Default machine mappings

**Output Formats:**
- `tree` - Hierarchical display showing plan relationships
- `flat` - Flat list of all items

**Example:**

```bash
DicomTools show --path "C:\Export\PAT001" --format tree --searchPattern "*.dcm"
```

**Sample Output:**
```
Plan: VMAT_60Gy
├── Dose: Dose_60Gy
├── Structure Set: CT_20240315
├── CT Image Series
│   ├── CT_1.2.840...
│   └── CT_1.2.840...
└── Beam: Field_1
    └── Beam: Field_2
```

**Use Case:** Preview exported data structure before importing to PACS or verify completeness of retrieval.

---

## Configuration

### appsettings.json

DicomTools uses `appsettings.json` for configuration. **Tip:** Use DICOMTools web interface Settings page to generate this file automatically!

**Example Configuration:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "DicomStorage": {
    "PortNumber": 104
  },
  "DicomAnonymizer": {
    "SecurityProfileFileName": "AriaSecurityProfile.csv",
    "SecurityProfileOptions": [
      "CleanDesc",
      "RetainDeviceIdent"
    ]
  }
}
```

### Listening Port Configuration

The `DicomStorage.PortNumber` setting controls which port DicomTools listens on for incoming C-STORE requests during retrieve operations.

**Common Ports:**
- `104` - Standard DICOM port (requires administrator privileges)
- `11112` - Alternative high port (no admin privileges needed)
- `4242` - Common DICOM test port

**Important:** The listening port in DicomTools must match the port your PACS/Eclipse is configured to send retrieved files to. Port mismatches are the most common cause of "successful" retrieve operations that don't actually transfer files.

### Logging Levels

Adjust log verbosity for troubleshooting:
- `Error` - Only errors
- `Warning` - Errors and warnings
- `Information` - Normal output (recommended)
- `Debug` - Detailed diagnostics

### Firewall Requirements

**Required Network Access:**
1. **Outbound:** DicomTools → PACS (retrieve/store operations)
2. **Inbound:** PACS → DicomTools (C-STORE during retrieve)

Configure Windows Firewall to allow:
- DicomTools.exe through firewall
- Listening port (default: 104) for inbound connections

### Anonymization Profiles

Two security profiles are available:

**1. AriaSecurityProfile.csv** (Default)
- DCIE-compatible anonymization
- Retains device identification
- Cleans descriptions
- Compatible with ARIA import

**2. BasicSecurityProfile.csv**
- DICOM PS3.15 basic security profile
- More aggressive anonymization
- **Note:** May not be importable into ARIA (required tags removed)

**Reference:** [DICOM PS3.15 2024e - Security and System Management Profiles](https://dicom.nema.org/medical/dicom/current/output/chtml/part15/chapter_e.html)

**Profile Options:**
- `CleanDesc` - Clean patient descriptions
- `RetainDeviceIdent` - Keep device identifiers
- `RetainPatientChars` - Preserve patient characteristics
- `RetainLongModifDatesAndTimes` - Keep modification timestamps

---

## Installation & Setup

### Prerequisites

**For DicomTools CLI:**
- .NET 8 Runtime
- Windows 10/11 or Windows Server
- Network access to DICOM services

**For ESAPIPatientBrowser:**
- Varian Eclipse TPS (v15.6, 16.1, 17.0+)
- .NET Framework 4.5.2+
- MahApps.Metro and Newtonsoft.Json (NuGet packages)

**For DICOMTools Web Interface:**
- Chromium-based browser (Chrome, Edge, Opera, or Brave)
- No additional dependencies

### Quick Install

1. **Download/Clone Repository:**
   ```bash
   git clone https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-DICOMtools.git
   ```

2. **Build DicomTools:**
   ```bash
   cd MAAS-DICOMtools/DicomTools
   dotnet build -c Release
   ```

3. **Build ESAPIPatientBrowser** (if using Eclipse):
   ```bash
   cd ESAPIPatientBrowser
   # Open solution in Visual Studio
   # Build for Release x64
   ```

4. **Configure appsettings.json:**
   - Use DICOMTools web interface Settings page, or
   - Manually edit `appsettings.json` in DicomTools directory

5. **Deploy ESAPIPatientBrowser to Eclipse** (optional):
   - Copy `ESAPIPatientBrowserLauncher.cs` to Eclipse Scripts directory
   - Copy `ESAPIPatientBrowser.exe` and dependencies to same location
   - Restart Eclipse

### First Run

1. **Open DICOMTools Web Interface:** Open `DicomTools/UI/public/index.html` in browser
2. **Configure Settings:** Go to Settings page and configure:
   - DICOM server settings (Host, Port, AETs)
   - DicomTools path (for portable batch files)
   - Listening port
3. **Test Connectivity:** Run Network Test and DICOM Connection Test
4. **Generate First Batch:** Try a simple retrieve operation

---

## Troubleshooting

### Common Issues

**Problem:** Retrieve shows success but no files exported

**Solution:** Port mismatch between DicomTools listening port and PACS send port
- Run Settings → Port Listening Test
- Verify listening port matches PACS configuration
- Check Windows Firewall settings

---

**Problem:** "ESAPI Application Failed to Initialize" in ESAPIPatientBrowser

**Solution:** 
- Ensure Eclipse is installed and ESAPI is available
- Check .NET Framework version (4.5.2+ required)
- Verify platform target is x64 (matches Eclipse)
- Run from Eclipse Scripts menu for proper initialization

---

**Problem:** "Atomic Access Violation" when searching patients

**Solution:** ESAPIPatientBrowser uses safe patterns, but if occurs:
- Close and reopen application
- Ensure only one ESAPI application is running
- Use "Add Patient" button for single patients instead of broad searches

---

**Problem:** Store operation fails with "Treatment unit not found"

**Solution:**
- Configure machine mapping in DICOMTools web interface
- Use `--machineMapping` option: `"OldName=NewName"`
- Use `--defaultMachines` for model-based mapping: `"RDS=HALCYON"`
- Use `--statusFileName` to resume after fixing mappings

---

**Problem:** Import JSON button doesn't load patient list

**Solution:**
- Verify JSON file format matches expected structure
- Check browser console for JavaScript errors
- Ensure JSON contains `patients` or `Patients` array
- Verify file is valid JSON (use online validator)

---

**Problem:** Advanced Search takes too long or times out

**Solution:**
- Use date range to limit patient count
- Search is sequential (opens each patient) - can't be parallelized
- Narrow search criteria before running
- Consider using Basic Search for patient discovery first

---

### Diagnostic Tools

DICOMTools web interface Settings page includes professional diagnostic tools:

1. **Network Test** - Ping host, check port accessibility
2. **DICOM Connection Test** - Verify query capability
3. **Port Listening Test** - Confirm DicomTools can bind to port
4. **Complete Diagnostic** - Comprehensive 4-phase troubleshooting

All tests generate downloadable batch files with detailed analysis.

---

## License & Compliance

### Software License

This software is subject to the **Varian Limited Use Software License Agreement (LUSLA)**.

By using this tool, you acknowledge compliance with all terms and conditions.

[View Full License Agreement](http://medicalaffairs.varian.com/download/VarianLUSLA.pdf)

### Important Disclaimer

**THIS SOFTWARE IS FOR EDUCATIONAL AND RESEARCH PURPOSES ONLY**

- NOT validated for clinical use
- NOT approved for patient care environments
- NOT a medical device
- NOT for diagnostic purposes

**Use at your own risk. Always validate results independently.**

### DICOM Compliance

- Implements DICOM PS3.4 Query/Retrieve service class
- Implements DICOM PS3.4 Storage service class
- Supports DICOM PS3.15 anonymization profiles
- Compatible with Varian Eclipse DICOM DB service

### Data Protection

- Patient data processed locally (no cloud/external servers)
- Anonymization available for data export
- Session-only storage in ESAPIPatientBrowser
- All DICOM operations logged through Eclipse audit system

---

## Contributing

This is part of the Varian Medical Affairs Applied Solutions (MAAS) suite of tools.

---

## Additional Documentation

- **ESAPIPatientBrowser:** [ESAPIPatientBrowser/README.md](../ESAPIPatientBrowser/README.md)
- **Pull Request Notes:** [PULL_REQUEST_NOTES.md](../PULL_REQUEST_NOTES.md)
- **In-App Help:** Open DICOMTools web interface → Help page
- **DICOM Standard:** [DICOM.nema.org](https://www.dicomstandard.org/)
- **fo-dicom Library:** [GitHub.com/fo-dicom/fo-dicom](https://github.com/fo-dicom/fo-dicom)

---

## Version History

**Version 2.0** (Current - Batch-and-Patient-List-Creator Branch)
- Added ESAPIPatientBrowser with advanced search
- Added DICOMTools web interface
- Multi-patient batch processing support
- JSON import/export functionality
- Connectivity testing suite
- Dynamic machine mapping
- Settings management interface

**Version 1.2** (Original DicomTools)
- retrieve, store, search, show commands
- Anonymization support

---

**Built with:** .NET 8, fo-dicom, WPF, MahApps.Metro, HTML5/JavaScript

**Maintained by:** Varian Medical Affairs Applied Solutions

**Support:** For issues and questions, please use GitHub Issues
