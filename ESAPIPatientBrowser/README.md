# ESAPI Patient Browser

## Overview
The ESAPI Patient Browser is a standalone WPF application that integrates with Eclipse™ Treatment Planning System (TPS) to browse patient databases and export patient/plan lists for use with the CombinedApp DICOM Tools suite.

## Features

### 🔍 **Patient Database Browsing**
- **Smart Search**: Search by Patient ID, First Name, Last Name, or combinations
- **Date Filtering**: Filter patients by creation date ranges
- **Batch Results**: Process up to 100 patients per search (configurable)
- **Real-time Loading**: Plans loaded on-demand when patients are selected

### 📋 **Plan Management**
- **Comprehensive Plan Details**: View course, plan ID, approval status, dose, fractions
- **Multi-Selection**: Select individual patients and specific plans
- **Approval Filtering**: Focus on approved plans or include all plan statuses
- **Structure Set Integration**: View associated structure set information

### 📁 **Data Export & Integration**
- **JSON Export**: Export selected patients/plans to structured JSON files
- **CombinedApp Integration**: Direct integration with DICOM Tools interface
- **Multi-Patient Processing**: Support for batch DICOM operations
- **Session Persistence**: Remember selections across application restarts

### 🔧 **Advanced Features**
- **ESAPI Launcher Script**: Deploy directly into Eclipse Scripts menu
- **Standalone Operation**: Run independently or launched from Eclipse
- **Automatic File Detection**: Finds CombinedApp installation automatically
- **Error Handling**: Comprehensive error handling with detailed diagnostics

## Architecture

### ESAPI Integration
```
Eclipse TPS → Script Launcher → ESAPIPatientBrowser.exe → Patient Database
```

### CombinedApp Workflow
```
ESAPIPatientBrowser → JSON Export → CombinedApp Import → DICOM Batch Files → DicomTools.exe
```

## Installation

### Prerequisites
- **Varian Eclipse™ TPS**: Version 15.6 or 16.1+ with ESAPI
- **CombinedApp**: DICOM Tools interface (in parent directory)
- **.NET Framework 4.5.2+**: Required for WPF application

### Deployment Steps

1. **Compile the Application**:
   ```bash
   # Build for Release x64 (matches Eclipse architecture)
   msbuild ESAPIPatientBrowser.sln /p:Configuration=Release /p:Platform=x64
   ```

2. **Install ESAPI Launcher**:
   - Copy `ESAPIPatientBrowserLauncher.cs` to Eclipse Scripts directory
   - Restart Eclipse to register the script

3. **Deploy Application**:
   ```
   [Eclipse Scripts Directory]/
   ├── ESAPIPatientBrowserLauncher.cs
   ├── ESAPIPatientBrowser.exe
   ├── Newtonsoft.Json.dll
   └── [Other dependencies]
   ```

## Usage

### From Eclipse
1. **Launch from Scripts Menu**: `Scripts → ESAPIPatientBrowserLauncher`
2. **Automatic Patient Loading**: If a patient is open, it will be pre-populated in search

### Standalone Operation
1. **Direct Launch**: Run `ESAPIPatientBrowser.exe` directly
2. **Command Line**: Pass patient ID as argument: `ESAPIPatientBrowser.exe "12345"`

### Patient Search & Selection
```
1. Enter search criteria (ID, name, date range)
2. Click "Search" to find patients
3. Select patients from the results grid
4. Click "Load Plans" to view available plans for each patient
5. Select specific plans if needed
```

### Export to CombinedApp
```
Method 1 - Direct Integration:
1. Select patients/plans in browser
2. Click "Send Patient List"
3. CombinedApp launches automatically with imported data

Method 2 - File-Based:
1. Select patients/plans in browser
2. Click "Export to JSON"
3. Save JSON file
4. In CombinedApp, click "Import JSON" button
5. Select saved JSON file
```

## JSON File Format

### Structure
```json
{
  "exportDateTime": "2025-09-25T14:30:00Z",
  "totalPatients": 3,
  "totalPlans": 8,
  "patients": [
    {
      "patientId": "12345",
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1980-01-15T00:00:00Z",
      "isSelected": true,
      "plans": [
        {
          "patientId": "12345",
          "courseId": "C1",
          "planId": "Plan1",
          "approvalStatus": "Approved",
          "totalDose": 60.0,
          "numberOfFractions": 30,
          "structureSetId": "CT_1",
          "isSelected": true
        }
      ]
    }
  ]
}
```

### CombinedApp Processing
- **Patient ID Field**: Populated with semicolon-separated list: `12345;67890;11111`
- **Multi-Patient Indicator**: Visual display shows patient count and summary
- **Batch Generation**: DicomTools receives all patient IDs in single command
- **Progress Tracking**: Enhanced batch files show per-patient results

## Configuration

### App.config Settings
```xml
<appSettings>
  <!-- Relative path to CombinedApp executable -->
  <add key="CombinedAppPath" value="..\CombinedApp\CombinedApp.exe" />
  
  <!-- URL for CombinedApp web interface (if running) -->
  <add key="CombinedAppUrl" value="http://localhost:8080" />
</appSettings>
```

### ESAPI Compatibility
- **Supported Versions**: Eclipse 15.6, 16.1, 17.0+
- **Required References**: `VMS.TPS.Common.Model.API.dll`, `VMS.TPS.Common.Model.Types.dll`
- **Platform Target**: x64 (matches Eclipse architecture)

## Integration Details

### CombinedApp Enhancements
The integration adds the following features to CombinedApp:

1. **Import JSON Button**: Next to Patient ID field in Export panel
2. **Multi-Patient Display**: Visual indicator showing selected patients
3. **Enhanced Batch Files**: Progress tracking for multiple patients
4. **Session Persistence**: Imported data survives page refreshes

### DICOM Tools Processing
- **Multi-Patient Support**: DicomTools natively supports semicolon-separated patient IDs
- **Individual Processing**: Each patient processed separately with individual result reporting
- **Error Isolation**: Failures with one patient don't affect others
- **Progress Tracking**: Batch files show per-patient success/failure status

## Troubleshooting

### Common Issues

**"ESAPI Application Failed to Initialize"**
- Ensure Eclipse is installed and ESAPI is available
- Check .NET Framework version compatibility
- Verify x64 platform target matches Eclipse

**"CombinedApp Not Found"**
- Check `CombinedAppPath` setting in App.config
- Ensure relative path is correct from ESAPIPatientBrowser location
- Verify CombinedApp.exe exists and is accessible

**"No Patients Found"**
- Check Eclipse database connectivity
- Verify search criteria and date ranges
- Ensure user has appropriate Eclipse permissions

**"JSON Import Failed in CombinedApp"**
- Verify JSON file format matches expected structure
- Check browser console for JavaScript errors
- Ensure JSON file is not corrupted

### Performance Optimization
- **Search Limits**: Default 100 patients per search (modify in `PatientSearchService`)
- **Date Filtering**: Use date ranges to limit database queries
- **On-Demand Loading**: Plans loaded only when "Load Plans" is clicked
- **Memory Management**: Patients closed after plan loading to free ESAPI resources

## Development Notes

### Key Components
- **PatientSearchService**: ESAPI database interaction
- **JsonExportService**: File I/O and JSON serialization
- **CombinedAppService**: Integration with DICOM Tools interface
- **MainViewModel**: MVVM pattern implementation for UI binding

### Extension Points
- **Custom Filters**: Add additional patient/plan filtering criteria
- **Export Formats**: Support additional export formats beyond JSON
- **Integration APIs**: REST API for programmatic access
- **Batch Operations**: Custom batch operations beyond DICOM tools

### Testing Strategy
- **Unit Tests**: Test data models and services independently
- **Integration Tests**: Test ESAPI connectivity in Eclipse environment
- **UI Tests**: Automated testing of WPF interface
- **End-to-End Tests**: Full workflow from Eclipse to CombinedApp

## Compliance & Security

### Educational Use Only
⚠️ **Important**: This software is for educational and research purposes only. It has NOT been validated for clinical use and should not be used in patient care environments.

### DICOM Compliance
- **Anonymization Support**: Integrates with CombinedApp anonymization features
- **Security Profiles**: ARIA and Basic Security Profile support
- **Audit Logging**: All patient access logged through Eclipse audit system

### Data Protection
- **Session-Only Storage**: Patient data stored only during application session
- **No Persistent Storage**: No patient data cached permanently
- **ESAPI Security**: Leverages Eclipse's built-in security and permissions

---

*This tool is part of the CombinedApp DICOM Tools suite. For questions about DICOM operations, refer to the CombinedApp documentation.*
