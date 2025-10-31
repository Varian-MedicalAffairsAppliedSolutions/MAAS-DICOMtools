// DICOM Tools - Batch File Generation Frontend
(function(){
  const $ = (id) => document.getElementById(id);
  
  let currentOperation = 'export';
  const STORAGE_KEY = 'dicom-tools-form-data';
  const FOLDER_CACHE_KEY = 'dicom-tools-last-folder';
  const CONFIG_STORAGE_KEY = 'dicom-tools-config-data';
  
  // Cache for directory handle (modern browsers)
  let lastDirectoryHandle = null;
  
  // Form persistence
  function autoSaveForm() {
    const formData = {};
    document.querySelectorAll('input, select, textarea').forEach(input => {
      if (input.id) {
        formData[input.id] = input.type === 'checkbox' ? input.checked : input.value;
      }
    });
    
    // Save machine mappings separately
    const machineMappings = getMachineMappings();
    if (machineMappings.length > 0) {
      formData.machineMappings = machineMappings;
    }
    
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(formData));
    } catch (e) {
      console.warn('Failed to save form data:', e);
    }
  }
  
  function autoLoadForm() {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (!saved) {
        // Set default values for first-time users
        setDefaultValues();
        return;
      }
      
      const formData = JSON.parse(saved);
      Object.keys(formData).forEach(id => {
        if (id === 'machineMappings') {
          // Handle machine mappings separately
          loadMachineMappings(formData[id]);
        } else {
        const element = document.getElementById(id);
        if (element) {
          if (element.type === 'checkbox') {
            element.checked = formData[id];
          } else {
            element.value = formData[id];
            }
          }
        }
      });

      // If handoff/imported JSON indicates objectives, auto-check the toggle for visibility
      try {
        if (window.HANDOFF_FULL && window.HANDOFF_FULL.hasObjectives) {
          const t = document.getElementById('includeObjectives');
          if (t) t.checked = true;
        }
      } catch (e) {}
    } catch (e) {
      console.warn('Failed to load form data:', e);
    }
  }
  
  function setDefaultValues() {
    // Set default search patterns to *.dcm
    const searchPatternFields = ['storeSearchPattern', 'searchPattern', 'showSearchPattern'];
    searchPatternFields.forEach(fieldId => {
      const element = document.getElementById(fieldId);
      if (element) {
        element.value = '*.dcm';
      }
    });
  }
  
  // Save last used folder path
  function saveLastFolderPath(folderPath) {
    try {
      localStorage.setItem(FOLDER_CACHE_KEY, folderPath);
      console.log('Saved last folder path:', folderPath);
    } catch (e) {
      console.warn('Failed to save folder path:', e);
    }
  }
  
  // Get last used folder path
  function getLastFolderPath() {
    try {
      return localStorage.getItem(FOLDER_CACHE_KEY);
    } catch (e) {
      console.warn('Failed to load folder path:', e);
      return null;
    }
  }
  
  // Try to get directory handle from last saved folder (modern browsers)
  async function getLastDirectoryHandle() {
    if (!lastDirectoryHandle) {
      const lastPath = getLastFolderPath();
      if (lastPath) {
        console.log('Last saved folder path:', lastPath);
        // For now, we can't directly recreate a directory handle from a path
        // But we'll show the path to the user in the save dialog
      }
    }
    return lastDirectoryHandle;
  }
  
  // Save directory handle for reuse (modern browsers)
  function saveDirectoryHandle(dirHandle) {
    lastDirectoryHandle = dirHandle;
    // Note: Directory handles can't be stored in localStorage due to security
    // They're only cached in memory for the current session
  }
  
  // Generate unique timestamp
  function generateTimestamp() {
    const now = new Date();
    return now.getFullYear().toString() +
           (now.getMonth() + 1).toString().padStart(2, '0') +
           now.getDate().toString().padStart(2, '0') + '_' +
           now.getHours().toString().padStart(2, '0') +
           now.getMinutes().toString().padStart(2, '0') +
           now.getSeconds().toString().padStart(2, '0');
  }

  // Build command line arguments
  function buildCommandLine(operation, parameters) {
    let args = [operation];
    
    switch (operation) {
      case 'retrieve':
        // Patient/Plan parameters first (as defined in RetrieveCommand.cs)
        if (parameters.patientId) args.push('--patientId', `"${parameters.patientId}"`);
        if (parameters.planId) args.push('--planId', `"${parameters.planId}"`);
        if (parameters.onlyApprovedPlans) args.push('--onlyApprovedPlans');
        // Always add --force for batch operations to skip prompts and enable data reuse
        args.push('--force');
        if (parameters.newPatientId) args.push('--newPatientId', `"${parameters.newPatientId}"`);
        if (parameters.newPatientName) args.push('--newPatientName', `"${parameters.newPatientName}"`);
        if (parameters.anonymize) args.push('--anonymize');
        // Path parameter
        if (parameters.path) args.push('--path', `"${parameters.path}"`);
        // Display options
        if (parameters.showTree) args.push('--showTree');
        // Connection parameters last (as defined in RetrieveCommand.cs)
        if (parameters.hostName) args.push('--hostName', `"${parameters.hostName}"`);
        if (parameters.hostPort) args.push('--hostPort', `"${parameters.hostPort}"`);
        if (parameters.callingAet) args.push('--callingAet', `"${parameters.callingAet}"`);
        if (parameters.calledAet) args.push('--calledAet', `"${parameters.calledAet}"`);
        // Additional options removed - useGet and useTls are not supported in current DicomTools version
        break;
        
      case 'store':
        // Follow exact order from StoreCommand.cs
        if (parameters.path) args.push('--path', `"${parameters.path}"`);
        if (parameters.searchPattern) args.push('--searchPattern', `"${parameters.searchPattern}"`);
        if (parameters.showMessages !== undefined) args.push('--showMessages', parameters.showMessages.toString());
        if (parameters.statusFileName) args.push('--statusFileName', `"${parameters.statusFileName}"`);
        // Handle multiple machine mappings as individual parameters
        if (parameters.machineMappings && parameters.machineMappings.length > 0) {
          parameters.machineMappings.forEach(mapping => {
            args.push('--machineMapping', `"${mapping}"`);
          });
        }
        if (parameters.defaultMachines) args.push('--defaultMachines', `"${parameters.defaultMachines}"`);
        if (parameters.hostName) args.push('--hostName', `"${parameters.hostName}"`);
        if (parameters.hostPort) args.push('--hostPort', `"${parameters.hostPort}"`);
        if (parameters.callingAet) args.push('--callingAet', `"${parameters.callingAet}"`);
        if (parameters.calledAet) args.push('--calledAet', `"${parameters.calledAet}"`);
        break;
        
      case 'search':
        // Tag parameters first (as defined in SearchTagCommand.cs)
        if (parameters.tagArray && parameters.tagArray.length > 0) {
          parameters.tagArray.forEach(tag => {
            if (tag.trim()) args.push('--tag', `"${tag.trim()}"`);
          });
        }
        // Path parameters second
        if (parameters.path) args.push('--path', `"${parameters.path}"`);
        if (parameters.searchPattern) args.push('--searchPattern', `"${parameters.searchPattern}"`);
        // Display options last
        if (parameters.showStatistics) args.push('--showStatistics');
        if (parameters.showOnlyDirectories) args.push('--showOnlyDirectories');
        break;
        
      case 'show':
        // Path parameters first (as defined in ShowCommand.cs)
        if (parameters.path) args.push('--path', `"${parameters.path}"`);
        if (parameters.searchPattern) args.push('--searchPattern', `"${parameters.searchPattern}"`);
        // Format parameter
        if (parameters.format) args.push('--format', `"${parameters.format}"`);
        // Optional parameters last
        if (parameters.defaultMachines) args.push('--defaultMachines', `"${parameters.defaultMachines}"`);
        break;
    }
    
    return args.join(' ');
  }

  // Generate optimization objectives copy commands for batch file
  // Handles edge cases: anonymization, multi-patient, missing files
  function generateObjectivesExportBlock(parameters, isMultiPatient) {
    // UI toggle (visual control). If present and not checked, skip objectives copy.
    try {
      const toggle = document.getElementById('includeObjectives');
      if (toggle && toggle.checked === false) {
        return '';
      }
    } catch (e) {}
    // Check if objectives data exists in imported JSON
    // Also honor parameter-level toggle if provided by import pipeline
    if (parameters.includeObjectives === false) {
      return '';
    }

    if (!parameters.hasObjectives || !parameters.objectiveFiles || parameters.objectiveFiles.length === 0) {
      return '';
    }
    
    // Validate staging path
    if (!parameters.objectivesStagingPath) {
      return `\r\nREM Objectives export skipped - no staging path in JSON\r\n`;
    }
    
    let stagingPath = parameters.objectivesStagingPath;
    
    let block = `\r\nREM ============================================\r\n`;
    block += `REM Copy Optimization Objectives\r\n`;
    block += `REM ============================================\r\n`;
    block += `echo.\r\n`;
    block += `echo [INFO] Copying optimization objectives from staging folder...\r\n\r\n`;
    
    // Try multiple locations for objectives folder (for robustness)
    block += `REM Try to locate objectives staging folder\r\n`;
    block += `set "_OBJ_STAGING="\r\n`;
    
    // Try 1: Absolute path (if provided)
    if (stagingPath.match(/^[a-zA-Z]:/) || stagingPath.startsWith('\\\\')) {
      block += `if exist "${stagingPath}" set "_OBJ_STAGING=${stagingPath}"\r\n`;
    }
    
    const isAbsolute = /^[a-zA-Z]:/.test(stagingPath) || stagingPath.startsWith('\\\\');
    if (!isAbsolute) {
      // Try 2: Relative to batch file location
      block += `if not defined _OBJ_STAGING if exist "%~dp0${stagingPath}" set "_OBJ_STAGING=%~dp0${stagingPath}"\r\n`;
      
      // Try 3: Relative to current directory
      block += `if not defined _OBJ_STAGING if exist "%CD%\\${stagingPath}" set "_OBJ_STAGING=%CD%\\${stagingPath}"\r\n`;
      
      // Try 4: Relative to export path parent
      block += `if not defined _OBJ_STAGING if exist "${parameters.path}\\..\\${stagingPath}" set "_OBJ_STAGING=${parameters.path}\\..\\${stagingPath}"\r\n`;
      
      // Extra fallback: Common Downloads location (typical for exported JSON)
      block += `if not defined _OBJ_STAGING if exist "%USERPROFILE%\\Downloads\\${stagingPath}" set "_OBJ_STAGING=%USERPROFILE%\\Downloads\\${stagingPath}"\r\n`;
    }
    
    block += `\r\n`;
    block += `if not defined _OBJ_STAGING (\r\n`;
    block += `    echo [WARNING] Objectives staging folder not found >> "%_LOG%"\r\n`;
    block += `    echo [WARNING] Searched for: ${stagingPath} >> "%_LOG%"\r\n`;
    block += `    echo [WARNING] Ensure the Objectives folder is:\r\n`;
    block += `    echo [WARNING]   - In the same directory as this batch file, OR\r\n`;
    block += `    echo [WARNING]   - In the same directory as the JSON file exported from Patient List Builder\r\n`;
    block += `    echo [WARNING] Skipping objectives copy >> "%_LOG%"\r\n`;
    block += `    goto :skip_objectives\r\n`;
    block += `)\r\n`;
    block += `echo [INFO] Staging folder found: %_OBJ_STAGING%\r\n\r\n`;
    
    if (isMultiPatient) {
      const patientIdsArray = parameters.patientId.split(';').map(p => p.trim()).filter(p => p);
      
      patientIdsArray.forEach((originalPid, index) => {
        // Determine target folder name (respecting anonymization)
        let targetFolderName;
        if (parameters.anonymize && parameters.newPatientId) {
          const paddedIndex = String(index + 1).padStart(2, '0');
          const anonId = `${parameters.newPatientId}-${paddedIndex}`;
          targetFolderName = anonId.replace(/[<>:\"\\/\\|?*]/g, '_');
        } else {
          targetFolderName = originalPid.replace(/[<>:\"\\/\\|?*]/g, '_');
        }
        
        const targetPath = `${parameters.path}\\${targetFolderName}`;
        
        // Find objective files for THIS patient using ORIGINAL patient ID
        const patientObjectives = parameters.objectiveFiles.filter(
          obj => (obj.patientId === originalPid || obj.PatientId === originalPid)
        );
        
        if (patientObjectives.length > 0) {
          block += `REM Patient: ${originalPid}${parameters.anonymize ? ` -> ${targetFolderName}` : ''}\r\n`;
          block += `if not exist "${targetPath}" mkdir "${targetPath}"\r\n`;
          
        patientObjectives.forEach(objFile => {
          const fileName = objFile.fileName || objFile.FileName;
          const planId = objFile.planId || objFile.PlanId;
          
          block += `if exist "%_OBJ_STAGING%\\${fileName}" (\r\n`;
          block += `    echo [INFO] Copying objectives for ${originalPid} - ${planId}\r\n`;
          block += `    copy "%_OBJ_STAGING%\\${fileName}" "${targetPath}\\${fileName}" >nul 2>>"%_LOG%"\r\n`;
          block += `    if errorlevel 1 (\r\n`;
          block += `        echo [WARNING] Failed to copy ${fileName} >> "%_LOG%"\r\n`;
          block += `    ) else (\r\n`;
          block += `        echo [INFO] Successfully copied ${fileName}\r\n`;
          block += `    )\r\n`;
          block += `) else (\r\n`;
          block += `    echo [WARNING] Objectives file not found: ${fileName} >> "%_LOG%"\r\n`;
          block += `)\r\n`;
        });
          block += `\r\n`;
        }
      });
    } else {
      // Single patient mode
      const originalPid = (parameters.patientId || '').trim();
      
      // Determine target folder (matching the logic used for DicomTools --path parameter)
      let targetFolderName;
      if (parameters.anonymize && parameters.newPatientId) {
        targetFolderName = parameters.newPatientId.replace(/[<>:\"\\/\\|?*]/g, '_').trim();
      } else {
        targetFolderName = originalPid.replace(/[<>:\"\\\/\|?*]/g, '_').trim();
      }
      
      const targetPath = `${parameters.path}\\${targetFolderName}`;
      
      // Find objectives for this patient using ORIGINAL patient ID
      const patientObjectives = parameters.objectiveFiles.filter(
        obj => (obj.patientId === originalPid || obj.PatientId === originalPid)
      );
      
      if (patientObjectives.length > 0) {
        block += `REM Patient: ${originalPid}${parameters.anonymize ? ` -> ${targetFolderName}` : ''}\r\n`;
        block += `if not exist "${targetPath}" mkdir "${targetPath}"\r\n`;
        
        patientObjectives.forEach(objFile => {
          const fileName = objFile.fileName || objFile.FileName;
          const planId = objFile.planId || objFile.PlanId;
          
          block += `if exist "%_OBJ_STAGING%\\${fileName}" (\r\n`;
          block += `    echo [INFO] Copying objectives for ${planId}\r\n`;
          block += `    copy "%_OBJ_STAGING%\\${fileName}" "${targetPath}\\${fileName}" >nul 2>>"%_LOG%"\r\n`;
          block += `    if errorlevel 1 (\r\n`;
          block += `        echo [WARNING] Failed to copy ${fileName} >> "%_LOG%"\r\n`;
          block += `    ) else (\r\n`;
          block += `        echo [INFO] Successfully copied ${fileName}\r\n`;
          block += `    )\r\n`;
          block += `) else (\r\n`;
          block += `    echo [WARNING] Objectives file not found: ${fileName} >> "%_LOG%"\r\n`;
          block += `)\r\n`;
        });
      } else {
        block += `echo [INFO] No objectives found for patient ${originalPid}\r\n`;
      }
    }
    
    block += `\r\n:skip_objectives\r\n`;
    block += `echo [INFO] Objectives processing completed\r\n`;
    block += `echo.\r\n\r\n`;
    
    return block;
  }

  // Create and save batch file
  async function createBatchFile(operation, parameters) {
    const timestamp = generateTimestamp();
    const commandLine = buildCommandLine(operation, parameters);
    // Determine configured DicomTools.exe path (optional) and listening port
    let configuredExePath = '';
    let configuredListeningPort = '';
    try {
      const raw = localStorage.getItem(CONFIG_STORAGE_KEY);
      if (raw) {
        const cfg = JSON.parse(raw);
        if (cfg && typeof cfg.dicomToolsPath === 'string') {
          const p = cfg.dicomToolsPath.trim();
          if (p) {
            configuredExePath = /\\DicomTools\.exe$/i.test(p) ? p : (p.replace(/[\\\/]+$/, '') + '\\DicomTools.exe');
          }
        }
        if (cfg && (cfg.listeningPort || cfg.listeningPort === 0)) {
          configuredListeningPort = String(cfg.listeningPort).trim();
        }
      }
    } catch {}
    
    // Check if this is a multi-patient operation
    const isMultiPatient = parameters.patientId && parameters.patientId.includes(';');
    const patientCount = isMultiPatient ? parameters.patientId.split(';').length : 1;
    
    // Prepare multi-patient retrieve block if needed
    let retrieveExecutionBlock = '';
    // Prepare single-patient retrieve block to always export into patient subfolder
    let retrieveSingleExecutionBlock = '';
    if (operation === 'retrieve' && isMultiPatient) {
      const patientIdsArray = parameters.patientId.split(';').map(p => p.trim()).filter(p => p);
      // Build base host connection args (no plan filters - those are added per-patient if needed)
      const hostArgs = [];
      if (parameters.hostName) hostArgs.push(`--hostName "${parameters.hostName}"`);
      if (parameters.hostPort) hostArgs.push(`--hostPort "${parameters.hostPort}"`);
      if (parameters.callingAet) hostArgs.push(`--callingAet "${parameters.callingAet}"`);
      if (parameters.calledAet) hostArgs.push(`--calledAet "${parameters.calledAet}"`);

      retrieveExecutionBlock += `REM Multi-patient retrieve - base folder ensured; per-patient folders not pre-created to avoid prompts\r\n`;
      if (parameters.planId) {
        retrieveExecutionBlock += `REM Filtering to specific plan: ${parameters.planId}\r\n`;
        retrieveExecutionBlock += `REM ⚡ SELECTIVE RETRIEVAL: DicomTools will filter plans BEFORE downloading data (performance optimized)\r\n`;
      }
      if (parameters.onlyApprovedPlans) {
        retrieveExecutionBlock += `REM Only approved plans will be exported\r\n`;
        retrieveExecutionBlock += `REM ⚡ SELECTIVE RETRIEVAL: DicomTools will filter plans BEFORE downloading data (performance optimized)\r\n`;
      }
      if (parameters.anonymize) {
        retrieveExecutionBlock += `REM Anonymization enabled - patients will be sequentially numbered\r\n`;
      }
      retrieveExecutionBlock += `if exist "${parameters.path}" (\r\n`;
      retrieveExecutionBlock += `    echo [INFO] Clearing base export directory: ${parameters.path}\r\n`;
      retrieveExecutionBlock += `    rmdir /s /q "${parameters.path}" 2>nul\r\n`;
      retrieveExecutionBlock += `    timeout /t 1 /nobreak >nul\r\n`;
      retrieveExecutionBlock += `)\r\n`;
      retrieveExecutionBlock += `if not exist "${parameters.path}" mkdir "${parameters.path}" 2>nul\r\n\r\n`;

      patientIdsArray.forEach((pid, index) => {
        // Determine folder name and anonymized IDs
        let folderName, anonId, anonName;
        
        if (parameters.anonymize) {
          // For multiple patients, add sequential suffix (01, 02, 03, etc.)
          const paddedIndex = String(index + 1).padStart(2, '0');
          anonId = `${parameters.newPatientId}-${paddedIndex}`;
          anonName = `${parameters.newPatientName}^${paddedIndex}`;
          folderName = anonId.replace(/[<>:\"\\/\\|?*]/g, '_').trim();
        } else {
          // No anonymization - use original patient ID
          folderName = pid.replace(/[<>:\"\\/\\|?*]/g, '_').trim();
        }
        
        const subPath = `${parameters.path}\\${folderName}`;
        
        // Check if this patient has specific plans from Patient List Builder
        const patientPlans = parameters.multiplePlans 
          ? parameters.multiplePlans.filter(p => p.patientId === pid)
          : [];
        
        if (patientPlans.length > 0) {
          // Patient has specific plans - generate one command per plan
          retrieveExecutionBlock += `echo [INFO] Retrieving ${patientPlans.length} plan(s) for patient ${pid}\r\n`;
          retrieveExecutionBlock += `if exist "${subPath}" (\r\n`;
          retrieveExecutionBlock += `    echo [INFO] Clearing existing patient directory: ${subPath}\r\n`;
          retrieveExecutionBlock += `    rmdir /s /q "${subPath}" 2>nul\r\n`;
          retrieveExecutionBlock += `    timeout /t 1 /nobreak >nul\r\n`;
          retrieveExecutionBlock += `)\r\n\r\n`;
          
          patientPlans.forEach((planInfo, planIndex) => {
            const patientArgs = [...hostArgs];
            
            // Add plan-specific filter
            if (planInfo.planId) patientArgs.push(`--planId "${planInfo.planId}"`);
            if (parameters.onlyApprovedPlans) patientArgs.push(`--onlyApprovedPlans`);
            // Add --force flag
            patientArgs.push(`--force`);
            
            // Add anonymization parameters if present
            if (parameters.anonymize) {
              patientArgs.push(`--anonymize`);
              patientArgs.push(`--newPatientId "${anonId}"`);
              patientArgs.push(`--newPatientName "${anonName}"`);
            }
            
            retrieveExecutionBlock += `echo [INFO] Plan ${planIndex + 1}/${patientPlans.length}: ${planInfo.planId}\r\n`;
            retrieveExecutionBlock += `"%_DT_EXE%" retrieve --patientId "${pid}" --path "${subPath}" --showTree ${patientArgs.join(' ')} 2>>"%_LOG%"\r\n\r\n`;
          });
        } else {
          // No specific plans - use global plan filter (original behavior)
          const patientArgs = [...hostArgs];
          // Add plan filters for this patient
          if (parameters.planId) patientArgs.push(`--planId "${parameters.planId}"`);
          if (parameters.onlyApprovedPlans) patientArgs.push(`--onlyApprovedPlans`);
          // Add --force flag
          patientArgs.push(`--force`);
          
          if (parameters.anonymize) {
            patientArgs.push(`--anonymize`);
            patientArgs.push(`--newPatientId "${anonId}"`);
            patientArgs.push(`--newPatientName "${anonName}"`);
          }
          
          let infoMessage = `Retrieving patient ${pid}`;
          if (parameters.planId) infoMessage += ` (plan: ${parameters.planId})`;
          if (parameters.anonymize) infoMessage += ` (anonymizing to ${anonId})`;
          
          retrieveExecutionBlock += `echo [INFO] ${infoMessage}\r\n`;
          retrieveExecutionBlock += `if exist "${subPath}" (\r\n`;
          retrieveExecutionBlock += `    echo [INFO] Clearing existing patient directory: ${subPath}\r\n`;
          retrieveExecutionBlock += `    rmdir /s /q "${subPath}" 2>nul\r\n`;
          retrieveExecutionBlock += `    timeout /t 1 /nobreak >nul\r\n`;
          retrieveExecutionBlock += `)\r\n`;
          retrieveExecutionBlock += `"%_DT_EXE%" retrieve --patientId "${pid}" --path "${subPath}" --showTree ${patientArgs.join(' ')} 2>>"%_LOG%"\r\n\r\n`;
        }
      });
    }
    if (operation === 'retrieve' && !isMultiPatient) {
      const pid = (parameters.patientId || '').trim();
      
      // Determine folder name based on anonymization settings
      let folderName;
      if (parameters.anonymize && parameters.newPatientId) {
        // Use anonymized ID for folder name
        folderName = parameters.newPatientId.replace(/[<>:\"\\/\\|?*]/g, '_').trim();
      } else {
        // Use original patient ID
        folderName = pid.replace(/[<>:\"\\\/\|?*]/g, '_').trim();
      }
      
      const subPath = `${parameters.path}\\${folderName}`;
      
      // Check if multiple plans are selected
      const hasMultiplePlans = parameters.multiplePlans && parameters.multiplePlans.length > 1;
      
      if (hasMultiplePlans) {
        // Multiple plans - group by patient and create one retrieve command per plan
        const plansByPatient = {};
        parameters.multiplePlans.forEach(planInfo => {
          const patientId = planInfo.patientId || pid;
          if (!plansByPatient[patientId]) {
            plansByPatient[patientId] = [];
          }
          plansByPatient[patientId].push(planInfo);
        });
        
        retrieveSingleExecutionBlock += `REM Multiple plans selected - exporting ${parameters.multiplePlans.length} plan(s) across ${Object.keys(plansByPatient).length} patient(s)\r\n`;
        retrieveSingleExecutionBlock += `REM ⚡ SELECTIVE RETRIEVAL: Each plan will be retrieved individually with optimized filtering\r\n\r\n`;
        
        // Generate commands for each patient
        Object.keys(plansByPatient).forEach(patientId => {
          const patientPlans = plansByPatient[patientId];
          
          // Determine folder for this patient
          let patientFolderName;
          if (parameters.anonymize && parameters.newPatientId) {
            patientFolderName = parameters.newPatientId.replace(/[<>:\"\\/\\|?*]/g, '_').trim();
          } else {
            patientFolderName = patientId.replace(/[<>:\"\\\/\|?*]/g, '_').trim();
          }
          const patientSubPath = `${parameters.path}\\${patientFolderName}`;
          
          retrieveSingleExecutionBlock += `echo [INFO] Retrieving ${patientPlans.length} plan(s) for patient ${patientId}\r\n`;
          
          // Clear directory once before first plan for this patient
          retrieveSingleExecutionBlock += `if exist "${patientSubPath}" (\r\n`;
          retrieveSingleExecutionBlock += `    echo [INFO] Clearing existing patient directory: ${patientSubPath}\r\n`;
          retrieveSingleExecutionBlock += `    rmdir /s /q "${patientSubPath}" 2>nul\r\n`;
          retrieveSingleExecutionBlock += `    timeout /t 1 /nobreak >nul\r\n`;
          retrieveSingleExecutionBlock += `)\r\n\r\n`;
          
          // Generate one command per plan for this patient
          patientPlans.forEach((planInfo, index) => {
            const hostArgs = [];
            if (parameters.hostName) hostArgs.push(`--hostName "${parameters.hostName}"`);
            if (parameters.hostPort) hostArgs.push(`--hostPort "${parameters.hostPort}"`);
            if (parameters.callingAet) hostArgs.push(`--callingAet "${parameters.callingAet}"`);
            if (parameters.calledAet) hostArgs.push(`--calledAet "${parameters.calledAet}"`);
            
            // Add plan filter for this specific plan
            if (planInfo.planId) hostArgs.push(`--planId "${planInfo.planId}"`);
            if (parameters.onlyApprovedPlans) hostArgs.push(`--onlyApprovedPlans`);
            // Add --force to skip confirmation prompts and enable data reuse
            hostArgs.push(`--force`);
            
            // Add anonymization parameters if present
            if (parameters.anonymize) {
              hostArgs.push(`--anonymize`);
              if (parameters.newPatientId) hostArgs.push(`--newPatientId "${parameters.newPatientId}"`);
              if (parameters.newPatientName) hostArgs.push(`--newPatientName "${parameters.newPatientName}"`);
            }
            
            retrieveSingleExecutionBlock += `echo [INFO] Plan ${index + 1}/${patientPlans.length}: ${planInfo.planId}\r\n`;
        retrieveSingleExecutionBlock += `"%_DT_EXE%" retrieve --patientId "${patientId}" --path "${patientSubPath}" --showTree ${hostArgs.join(' ')} 2>>"%_LOG%"\r\n\r\n`;
          });
        });
      } else {
        // Single plan or no plan filter - original behavior
        const hostArgs = [];
        if (parameters.hostName) hostArgs.push(`--hostName "${parameters.hostName}"`);
        if (parameters.hostPort) hostArgs.push(`--hostPort "${parameters.hostPort}"`);
        if (parameters.callingAet) hostArgs.push(`--callingAet "${parameters.callingAet}"`);
        if (parameters.calledAet) hostArgs.push(`--calledAet "${parameters.calledAet}"`);
        
        // Add plan filtering parameters
        if (parameters.planId) hostArgs.push(`--planId "${parameters.planId}"`);
        if (parameters.onlyApprovedPlans) hostArgs.push(`--onlyApprovedPlans`);
        // Add --force to skip confirmation prompts and enable data reuse
        hostArgs.push(`--force`);
        
        // Add anonymization parameters if present
        if (parameters.anonymize) {
          hostArgs.push(`--anonymize`);
          if (parameters.newPatientId) hostArgs.push(`--newPatientId "${parameters.newPatientId}"`);
          if (parameters.newPatientName) hostArgs.push(`--newPatientName "${parameters.newPatientName}"`);
        }

        let infoMessage = `Retrieving patient ${pid}`;
        if (parameters.planId) infoMessage += ` (plan: ${parameters.planId})`;
        if (parameters.anonymize) infoMessage += ` (anonymizing to ${parameters.newPatientId})`;
        
        if (parameters.planId || parameters.onlyApprovedPlans) {
          retrieveSingleExecutionBlock += `REM ⚡ SELECTIVE RETRIEVAL ENABLED: Filtering plans before downloading (performance optimized)\r\n`;
        }
        retrieveSingleExecutionBlock += `echo [INFO] ${infoMessage}\r\n`;
        retrieveSingleExecutionBlock += `if exist "${subPath}" (\r\n`;
        retrieveSingleExecutionBlock += `    echo [INFO] Clearing existing patient directory: ${subPath}\r\n`;
        retrieveSingleExecutionBlock += `    rmdir /s /q "${subPath}" 2>nul\r\n`;
        retrieveSingleExecutionBlock += `    timeout /t 1 /nobreak >nul\r\n`;
        retrieveSingleExecutionBlock += `)\r\n`;
        retrieveSingleExecutionBlock += `"%_DT_EXE%" retrieve --patientId "${pid}" --path "${subPath}" --showTree ${hostArgs.join(' ')} 2>>"%_LOG%"\r\n`;
      }
    }

  // Helper: generate anonymization CSV consolidation block
  function generateAnonymizationMergeBlock(parameters) {
    if (!parameters.anonymize) return '';
    let block = `\r\nREM ============================================\r\n`;
    block += `REM Consolidate Anonymization Mapping CSVs\r\n`;
    block += `REM ============================================\r\n`;
    block += `set "_ANON_OUT=${parameters.path}\\anonymization_map.csv"\r\n`;
    block += `if not exist "%_ANON_OUT%" echo OriginalPatientId,OriginalPatientName,AnonymizedPatientId,AnonymizedPatientName,TimestampUtc>"%_ANON_OUT%"\r\n`;
    block += `for /r "${parameters.path}" %%F in (anonymization_map.csv) do (\r\n`;
    block += `    if /I not "%%~fF"=="%_ANON_OUT%" (\r\n`;
    block += `        for /f "skip=1 usebackq delims=" %%L in ("%%F") do (\r\n`;
    block += `            >> "%_ANON_OUT%" echo %%L\r\n`;
    block += `        )\r\n`;
    block += `    )\r\n`;
    block += `)\r\n`;
    block += `if exist "%_ANON_OUT%" (\r\n`;
    block += `    echo [INFO] Consolidated anonymization map: %_ANON_OUT%\r\n`;
    block += `) else (\r\n`;
    block += `    echo [INFO] No anonymization maps found to consolidate\r\n`;
    block += `)\r\n\r\n`;
    return block;
  }

  // Create batch file content with enhanced progress tracking
    const batchContent = `@echo off
title DICOM Tools - ${operation.toUpperCase()} Operation ${isMultiPatient ? `(${patientCount} Patients)` : ''}
color 0A

echo ===============================================
echo   DICOM Tools - ${operation.toUpperCase()} Operation
echo   Timestamp: ${timestamp}
${isMultiPatient ? `echo   Multi-Patient Mode: ${patientCount} patients
echo   Patients: ${parameters.patientId}` : ''}
echo ===============================================
echo.

REM Resolve DicomTools.exe path
set "_DT_EXE=${configuredExePath ? configuredExePath.replace(/"/g, '""') : '%~dp0DicomTools.exe'}"
if not exist "%_DT_EXE%" (
    echo [ERROR] DicomTools.exe not found at "%_DT_EXE%"
    echo Configure path in Settings -> DICOM Server Settings -> DicomTools Path
    pause
    exit /b 1
)
for %%I in ("%_DT_EXE%") do set "_DT_DIR=%%~dpI"
pushd "%_DT_DIR%"

REM Apply configured listening port (overrides appsettings.json at runtime)
${configuredListeningPort ? `set "DicomStorage:PortNumber=${configuredListeningPort}"
echo [INFO] Using listening port from Settings: ${configuredListeningPort}
` : ''}

REM Show current directory for debugging
echo [INFO] Current directory: %CD%
REM Ensure export root exists before initializing the batch log
if not exist "${parameters.path}" mkdir "${parameters.path}" 2>nul
REM Initialize batch error log at export root (or source path for import)
set "_EXPORT_ROOT=${parameters.path}"
set "_LOG=%_EXPORT_ROOT%\\batch_errors.txt"
del /f /q "%_LOG%" >nul 2>&1
echo [INFO] Batch started %date% %time% > "%_LOG%"
echo [INFO] Executing DICOM Tools...
echo [CMD]  "%_DT_EXE%" ${commandLine}
echo.

${operation === 'retrieve' ? (isMultiPatient ? `${retrieveExecutionBlock}
${generateObjectivesExportBlock(parameters, true)}
${generateAnonymizationMergeBlock(parameters)}` : `${retrieveSingleExecutionBlock}
${generateObjectivesExportBlock(parameters, false)}
${generateAnonymizationMergeBlock(parameters)}`) : `REM Execute DicomTools.exe with resolved path
"%_DT_EXE%" ${commandLine} 2>>"%_LOG%"
`}

REM Capture exit code
set EXIT_CODE=%ERRORLEVEL%
popd

echo.
echo ===============================================
if %EXIT_CODE% EQU 0 (
    echo [SUCCESS] Operation completed successfully!
    echo Exit Code: %EXIT_CODE%
    >> "%_LOG%" echo [INFO] Operation completed successfully! Exit Code: %EXIT_CODE%
    ${isMultiPatient && operation === 'retrieve' ? `
    echo.
    echo [MULTI-PATIENT] Checking export results for ${patientCount} patients...
    echo Export Path: ${parameters.path}
    if exist "${parameters.path}" (
        echo [INFO] Export directory created successfully
        for /d %%D in ("${parameters.path}\\*") do (
            echo [PATIENT] Found data for: %%~nxD
        )
    ) else (
        echo [WARNING] Export directory not found - check for errors above
        >> "%_LOG%" echo [WARNING] Export directory not found - check for errors above
    )` : ''}
) else (
    echo [ERROR] Operation failed!
    echo Exit Code: %EXIT_CODE%
    >> "%_LOG%" echo [ERROR] Operation failed! Exit Code: %EXIT_CODE%
    ${isMultiPatient ? `echo [MULTI-PATIENT] When processing multiple patients, some may succeed while others fail` : ''}
)
echo ===============================================
echo.

echo Press any key to close...
pause >nul`;

    const filename = `DICOMTools_${operation}_${timestamp}.bat`;

    // Try to use File System Access API to let user choose save location
    try {
      if ('showSaveFilePicker' in window) {
        // Get last used directory handle if available
        const lastDirHandle = await getLastDirectoryHandle();
        
        const options = {
          suggestedName: filename,
          types: [{
            description: 'Batch files',
            accept: { 'text/plain': ['.bat'] }
          }]
        };
        
        // If we have a cached directory handle, start there
        if (lastDirectoryHandle) {
          options.startIn = lastDirectoryHandle;
        }
        
        const fileHandle = await window.showSaveFilePicker(options);
        
        const writable = await fileHandle.createWritable();
        await writable.write(batchContent);
        await writable.close();
        
        // Try to cache the directory for next time
        try {
          // Attempt to get the parent directory handle (Chrome 108+)
          if ('getDirectoryHandle' in fileHandle || fileHandle.kind === 'directory') {
            // If we can get parent directory info, save it
            saveLastFolderPath('User selected folder - cached for session');
          }
          
          // For current session, try to extract directory info from file handle
          const file = await fileHandle.getFile();
          if (file && file.name) {
            // Save the fact that user has used the save dialog before
            saveLastFolderPath(`Last used: ${new Date().toLocaleDateString()}`);
          }
        } catch (e) {
          console.log('Could not cache directory info:', e);
          // Still save that a folder was selected
          saveLastFolderPath(`Folder selected: ${new Date().toLocaleDateString()}`);
        }
        
        showMessage(`Batch file "${filename}" saved successfully!\n\nTo run: Double-click the batch file\n\nTip: For faster saves, this location is now your default save folder for this session.`);
        return;
      }
    } catch (error) {
      // User cancelled or API not supported, fall back to download
      console.log('File save cancelled or not supported, falling back to download');
    }

    // Fallback: Create and download the batch file
    const blob = new Blob([batchContent], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    if (configuredExePath) {
      showMessage(`Batch file "${filename}" downloaded.\n\nYou can run it from any location. It will call:\n${configuredExePath}`);
    } else {
      showMessage(`Batch file "${filename}" downloaded to Downloads folder.\n\nIMPORTANT: Move this file to the same folder as DicomTools.exe or set the DicomTools Path in Settings before running it.\n\nTo run: Double-click the batch file`);
    }
    
    return timestamp;
  }

  // Ping functionality removed

  // Get current output element
  function getCurrentOutputElement() {
    const activePanel = document.querySelector('.panel:not([style*="display: none"])');
    if (activePanel) {
      return activePanel.querySelector('pre[id*="output"]') || activePanel.querySelector('pre');
    }
    return $('output');
  }

  // Show message in output (without toast)
  function showMessage(message) {
    const outputEl = getCurrentOutputElement();
    if (outputEl) {
      outputEl.textContent += `${message}\n\n`;
      outputEl.scrollTop = outputEl.scrollHeight;
    }
  }

  function showToast(message) {
    const toast = document.createElement('div');
    toast.textContent = message;
    toast.style.cssText = `
      position: fixed; top: 20px; right: 20px; z-index: 10000;
      background: var(--siemens-petrol); color: white;
      padding: 12px 20px; border-radius: 6px; font-size: 13px;
      max-width: 400px; opacity: 0; transition: opacity 0.3s ease;
      white-space: pre-line;
    `;
    
    document.body.appendChild(toast);
    setTimeout(() => toast.style.opacity = '1', 10);
    
    setTimeout(() => {
      toast.style.opacity = '0';
      setTimeout(() => toast.remove(), 300);
    }, 6000);
  }

  function showConfigMessage(message) {
    const configOutput = $('configOutput');
    if (configOutput) {
      configOutput.style.display = 'block';
      configOutput.textContent = message;
      configOutput.scrollTop = configOutput.scrollHeight;
    } else {
      // Fallback to regular message if configOutput not found
      showMessage(message);
    }
  }

  // createHelpBatch function removed

  
  
  

  // Command execution functions
  function executeRetrieve() {
    const basePath = $('exportPath').value;
    const timestamp = generateTimestamp();
    const uniquePath = basePath ? `${basePath}\\${timestamp}` : `C:\\Temp\\DICOMExport\\${timestamp}`;
    
    const args = {
      patientId: $('patientId').value,
      path: uniquePath,
      hostName: $('hostName').value,
      hostPort: $('hostPort').value,
      callingAet: $('callingAet').value,
      calledAet: $('calledAet').value
    };
    
    // Add plan filtering options
    const planId = $('planId') ? $('planId').value.trim() : '';
    if (planId) {
      args.planId = planId;
    }
    
    // Check if multiple plans were selected from Patient List Builder
    let selectedPlans = [];
    try {
      const storedPlans = sessionStorage.getItem('selectedPlans');
      if (storedPlans) {
        selectedPlans = JSON.parse(storedPlans);
      }
    } catch (e) {
      // Ignore parse errors
    }
    
    // Store selected plans array for batch file generation
    if (selectedPlans.length > 0) {
      args.multiplePlans = selectedPlans;
    }
    
    const onlyApprovedPlans = $('onlyApprovedPlans');
    if (onlyApprovedPlans && onlyApprovedPlans.checked) {
      args.onlyApprovedPlans = true;
    }
    
    // Add anonymization parameters if enabled
    const anonymizeCheckbox = $('anonymize');
    if (anonymizeCheckbox && anonymizeCheckbox.checked) {
      const newPatientId = $('newPatientId').value.trim();
      const newPatientName = $('newPatientName').value.trim();
      
      // Validate that both fields are filled
      if (!newPatientId || !newPatientName) {
        showMessage('Error: Both New Patient ID and New Patient Name are required when anonymization is enabled.');
        return;
      }
      
      args.anonymize = true;
      args.newPatientId = newPatientId;
      args.newPatientName = newPatientName;
    }
    
    // Load optimization objectives data from imported JSON (if available)
    try {
      const importedData = sessionStorage.getItem('importedPatientData');
      if (importedData) {
        const jsonData = JSON.parse(importedData);
        
        // Check if objectives data exists in imported JSON
        if (jsonData.hasObjectives || jsonData.HasObjectives) {
          args.hasObjectives = true;
          args.objectivesStagingPath = jsonData.objectivesStagingPath || jsonData.ObjectivesStagingPath || 'Objectives';
          args.objectiveFiles = jsonData.objectiveFiles || jsonData.ObjectiveFiles || [];
          
          console.log(`Objectives data loaded: ${args.objectiveFiles.length} files, staging path: ${args.objectivesStagingPath}`);
        }
      }
    } catch (e) {
      console.warn('Could not load objectives data from imported JSON:', e);
      // Continue without objectives - not a critical error
    }
    
    createBatchFile('retrieve', args);
  }

  function executeStore() {
    const args = {
      path: $('storePath').value,
      searchPattern: $('storeSearchPattern').value,
      statusFileName: $('statusFileName').value,
      machineMappings: getMachineMappings(), // Now returns array
      defaultMachines: $('defaultMachines').value,
      hostName: $('hostName').value,
      hostPort: $('hostPort').value,
      callingAet: $('callingAet').value,
      calledAet: $('calledAet').value
    };
    createBatchFile('store', args);
  }

  function executeSearch() {
    const tags = $('searchTags').value;
    const args = {
      path: $('searchPath').value,
      searchPattern: $('searchPattern').value,
        tagArray: tags ? tags.split(/\s+/) : [],
        showStatistics: $('showStatistics').checked,
        showOnlyDirectories: $('showOnlyDirectories').checked
    };
    createBatchFile('search', args);
  }

  function executeShow() {
    const args = {
      path: $('showPath').value,
      searchPattern: $('showSearchPattern').value,
      format: $('showFormat').value,
      defaultMachines: $('showDefaultMachines').value
    };
    createBatchFile('show', args);
  }

  // executePing function removed

  // Configuration Management Functions
  function loadCurrentConfig() {
    try {
      const saved = localStorage.getItem(CONFIG_STORAGE_KEY);
      if (saved) {
        const config = JSON.parse(saved);
        
        // Update UI with saved config
        if (config.listeningPort) {
          $('listeningPort').value = config.listeningPort;
        }
        if (config.logLevel) {
          $('logLevel').value = config.logLevel;
        }
        // Load consolidated DICOM server settings if present
        if (config.hostName) {
          const el = $('hostName');
          if (el) el.value = config.hostName;
        }
        if (config.hostPort) {
          const el = $('hostPort');
          if (el) el.value = config.hostPort;
        }
        if (config.callingAet) {
          const el = $('callingAet');
          if (el) el.value = config.callingAet;
        }
        if (config.calledAet) {
          const el = $('calledAet');
          if (el) el.value = config.calledAet;
        }
        if (config.dicomToolsPath) {
          const el = $('dicomToolsPath');
          if (el) el.value = config.dicomToolsPath;
        }
        
        updatePortStatus('Configuration loaded from browser storage');
      } else {
        // Set defaults
        $('listeningPort').value = '104';
        $('logLevel').value = 'Information';
        updatePortStatus('Using default configuration');
      }
    } catch (e) {
      console.warn('Failed to load config:', e);
      updatePortStatus('Error loading configuration');
    }
  }

  function saveCurrentConfig() {
    try {
      const config = {
        listeningPort: $('listeningPort').value,
        logLevel: $('logLevel').value,
        // Consolidated DICOM server settings
        hostName: $('hostName')?.value || '',
        hostPort: $('hostPort')?.value || '',
        callingAet: $('callingAet')?.value || '',
        calledAet: $('calledAet')?.value || '',
        dicomToolsPath: $('dicomToolsPath')?.value || '',
        lastUpdated: new Date().toISOString()
      };
      
      localStorage.setItem(CONFIG_STORAGE_KEY, JSON.stringify(config));
      return config;
    } catch (e) {
      console.warn('Failed to save config:', e);
      return null;
    }
  }

  function updatePortStatus(message) {
    const statusEl = $('portStatus');
    if (statusEl) {
      statusEl.textContent = message;
    }
  }

  // JSON Import Functionality
  function importPatientJson() {
    try {
      // Create a file input element
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.json';
      input.style.display = 'none';
      
      input.onchange = function(event) {
        const file = event.target.files[0];
        if (!file) return;
        
        const reader = new FileReader();
        reader.onload = function(e) {
          try {
            const jsonData = JSON.parse(e.target.result);
            
            // Validate JSON structure (accept both "patients" and "Patients")
            const patientsArray = Array.isArray(jsonData.patients) ? jsonData.patients : (Array.isArray(jsonData.Patients) ? jsonData.Patients : null);
            if (!patientsArray) {
              throw new Error('Invalid JSON format: missing or invalid patients/Patients array');
            }
            
            // Extract patient IDs from the imported data
            const patientIds = extractPatientIds(jsonData);
            
            if (patientIds.length === 0) {
              showMessage('No patients found in the imported JSON file.');
              return;
            }
            
            // Update the patient ID field with the imported data
            updatePatientIdField(patientIds, jsonData);
            
            // Show success message
            showMessage(`Successfully imported ${patientIds.length} patient(s) from JSON file:\n${patientIds.join(', ')}`);
            
            // Auto-save the form with the new data
            autoSaveForm();
            
          } catch (parseError) {
            showMessage(`Error parsing JSON file: ${parseError.message}`);
          }
        };
        
        reader.onerror = function() {
          showMessage('Error reading file. Please try again.');
        };
        
        reader.readAsText(file);
      };
      
      // Trigger file dialog
      document.body.appendChild(input);
      input.click();
      document.body.removeChild(input);
      
    } catch (error) {
      showMessage(`Error importing JSON: ${error.message}`);
    }
  }
  
  function extractPatientIds(jsonData) {
    const patientIds = [];
    const patientsArray = Array.isArray(jsonData)
      ? jsonData
      : (Array.isArray(jsonData.patients) ? jsonData.patients : (Array.isArray(jsonData.Patients) ? jsonData.Patients : []));
    
    patientsArray.forEach(p => {
      const id = (p.patientId || p.PatientId || p.id || p.ID || '').toString().trim();
      const isSelectedProp = (p.hasOwnProperty('isSelected') ? p.isSelected : (p.hasOwnProperty('IsSelected') ? p.IsSelected : undefined));
      const included = (isSelectedProp === undefined || isSelectedProp !== false);
      if (id && included) {
        patientIds.push(id);
      }
    });
    
    return patientIds;
  }
  
  function extractSelectedPlans(jsonData) {
    const selectedPlans = [];
    const patientsArray = Array.isArray(jsonData)
      ? jsonData
      : (Array.isArray(jsonData.patients) ? jsonData.patients : (Array.isArray(jsonData.Patients) ? jsonData.Patients : []));
    
    console.log('extractSelectedPlans - patientsArray length:', patientsArray.length);
    
    patientsArray.forEach(patient => {
      const patientId = patient.PatientId || patient.patientId || 'Unknown';
      console.log('Processing patient:', patientId);
      
      // Get plans array - process ALL patients to find selected plans
      const plansArray = patient.Plans || patient.plans || [];
      console.log('  Plans count:', plansArray.length);
      
      // Extract selected plan IDs
      plansArray.forEach(plan => {
        const isPlanSelected = plan.hasOwnProperty('IsSelected') ? plan.IsSelected :
                              (plan.hasOwnProperty('isSelected') ? plan.isSelected : false);
        
        const planId = (plan.PlanId || plan.planId || '').toString().trim();
        console.log('  Plan:', planId, 'isSelected:', isPlanSelected);
        
        if (isPlanSelected && planId) {
          // Store plan with patient ID
          const planInfo = { planId, patientId };
          // Only add if not already in list (check by plan and patient)
          const exists = selectedPlans.some(p => 
            p.planId === planId && 
            p.patientId === patientId
          );
          if (!exists) {
            selectedPlans.push(planInfo);
          }
        }
      });
    });
    
    console.log('extractSelectedPlans - result:', selectedPlans);
    return selectedPlans;
  }
  
  function updatePatientIdField(patientIds, jsonData) {
    const patientIdField = $('patientId');
    if (!patientIdField) return;
    
    // Join multiple patient IDs with semicolon (DicomTools supports this)
    const patientIdString = patientIds.join(';');
    patientIdField.value = patientIdString;
    
    // Extract and populate selected plans
    const selectedPlans = extractSelectedPlans(jsonData);
    const planIdField = $('planId');
    
    if (planIdField && selectedPlans.length > 0) {
      // Store ALL selected plans for batch file generation (will create one command per plan)
      sessionStorage.setItem('selectedPlans', JSON.stringify(selectedPlans));
      
      const firstPlan = selectedPlans[0];
      planIdField.value = firstPlan.planId;
      
      // Show info message about plan selection
      if (selectedPlans.length === 1) {
        const patientInfo = firstPlan.patientId ? ` (Patient: ${firstPlan.patientId})` : '';
        showMessage(`Auto-populated Plan: ${firstPlan.planId}${patientInfo} (from Patient List Builder selection)`);
      } else {
        // Multiple plans selected - will export all
        const allPlans = selectedPlans.map(p => {
          return p.patientId ? `${p.patientId} - ${p.planId}` : p.planId;
        });
        showMessage(`✅ ${selectedPlans.length} plans selected for export:\n${allPlans.join('\n')}\n\nAll plans will be exported to their respective patient folders.\nShowing first plan in UI fields: ${allPlans[0]}`);
      }
    }
    
    // Store a normalized JSON data for potential future use
    try {
      const normalized = Array.isArray(jsonData)
        ? { patients: jsonData }
        : (jsonData.patients ? jsonData : (jsonData.Patients ? { patients: jsonData.Patients } : jsonData));
      sessionStorage.setItem('importedPatientData', JSON.stringify(normalized));
    } catch {}
    
    // Update the UI to show multi-patient mode
    updatePatientDisplayUI(patientIds, jsonData);
  }
  
  function updatePatientDisplayUI(patientIds, jsonData) {
    // Create a visual indicator that shows we're in multi-patient mode
    let existingIndicator = document.querySelector('#multi-patient-indicator');
    if (existingIndicator) {
      existingIndicator.remove();
    }
    
    if (patientIds.length > 1) {
      const indicator = document.createElement('div');
      indicator.id = 'multi-patient-indicator';
      indicator.style.cssText = `
        margin-top: 4px;
        padding: 8px;
        background: #e3f2fd;
        border: 1px solid #2196f3;
        border-radius: 4px;
        font-size: 12px;
        color: #1976d2;
      `;
      
      const summary = (jsonData && jsonData.totalPatients !== undefined) ? jsonData.totalPatients : patientIds.length;
      
      indicator.innerHTML = `
        <strong>Multi-Patient Mode:</strong> ${summary} patients selected
        <br><strong>Patients:</strong> ${patientIds.slice(0, 3).join(', ')}${patientIds.length > 3 ? ` (+${patientIds.length - 3} more)` : ''}
        <button style="float: right; margin-top: -2px; padding: 2px 8px; font-size: 11px;" onclick="clearImportedPatients()">Clear</button>
      `;
      
      const patientIdField = $('patientId');
      if (patientIdField && patientIdField.parentNode) {
        patientIdField.parentNode.insertBefore(indicator, patientIdField.nextSibling);
      }
    }
  }
  
  // Global function to clear imported patients (called by the clear button)
  window.clearImportedPatients = function() {
    const patientIdField = $('patientId');
    if (patientIdField) {
      patientIdField.value = '';
    }
    
    const indicator = document.querySelector('#multi-patient-indicator');
    if (indicator) {
      indicator.remove();
    }
    
    sessionStorage.removeItem('importedPatientData');
    autoSaveForm();
    showMessage('Imported patient list cleared.');
  };

  function generateAppSettingsJson(config) {
    const appSettings = {
      "Logging": {
        "LogLevel": {
          "Default": config.logLevel || "Information",
          "Microsoft": "Information",
          "Microsoft.Hosting.Lifetime": "Information"
        },
        "Console": {
          "LogLevel": {
            "Default": config.logLevel || "Information"
          }
        }
      },
      "DicomStorage": {
        "PortNumber": parseInt(config.listeningPort) || 104
      },
      "DicomAnonymizer": {
        "SecurityProfileFileName": "AriaSecurityProfile.csv",
        "SecurityProfileOptions": [
          "CleanDesc",
          "RetainDeviceIdent"
        ]
      }
    };
    
    return JSON.stringify(appSettings, null, 2);
  }

  function updatePortConfiguration() {
    const port = $('listeningPort').value;
    const logLevel = $('logLevel').value;
    
    // Validate port
    const portNum = parseInt(port);
    if (!portNum || portNum < 1 || portNum > 65535) {
      showMessage('Error: Invalid port number. Must be between 1 and 65535.');
      return;
    }
    
    // Save to local storage
    const config = saveCurrentConfig();
    if (!config) {
      showMessage('Error: Failed to save configuration.');
      return;
    }
    
    // Generate new appsettings.json content
    const appSettingsContent = generateAppSettingsJson(config);
    
    // Create downloadable file
    const timestamp = generateTimestamp();
    const filename = `appsettings_${timestamp}.json`;
    
    try {
      const blob = new Blob([appSettingsContent], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.display = 'none';
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      updatePortStatus(`Port set to ${port}, log level set to ${logLevel}`);
      showMessage(`Configuration updated successfully!

New appsettings.json file downloaded: ${filename}

IMPORTANT STEPS:
1. Replace the existing appsettings.json with the downloaded file
2. Restart DicomTools for changes to take effect
3. Ensure your PACS/Eclipse is configured to send to port ${port}

The new configuration:
- Listening Port: ${port}
- Log Level: ${logLevel}`);
      
      // Show current config in output
      const configOutput = $('configOutput');
      if (configOutput) {
        configOutput.style.display = 'block';
        configOutput.textContent = appSettingsContent;
      }
      
    } catch (error) {
      showMessage('Error: Failed to create configuration file. ' + error.message);
    }
  }

  function createConfigBackup() {
    const timestamp = generateTimestamp();
    const filename = `appsettings_backup_${timestamp}.json`;
    
    // Generate current config (this would ideally read the current appsettings.json)
    const currentConfig = {
      listeningPort: $('listeningPort').value || '104',
      logLevel: $('logLevel').value || 'Information'
    };
    
    const backupContent = generateAppSettingsJson(currentConfig);
    
    try {
      const blob = new Blob([backupContent], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.display = 'none';
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      showMessage(`Configuration backup created: ${filename}`);
      
    } catch (error) {
      showMessage('Error: Failed to create backup. ' + error.message);
    }
  }

  function resetConfigToDefaults() {
    if (confirm('Reset configuration to defaults?\n\nThis will:\n- Set listening port to 104\n- Set log level to Information\n- Clear any saved configuration')) {
      $('listeningPort').value = '104';
      $('logLevel').value = 'Information';
      
      // Clear saved config
      localStorage.removeItem(CONFIG_STORAGE_KEY);
      
      updatePortStatus('Configuration reset to defaults');
      showMessage('Configuration reset to defaults. Click "Update Port Configuration" to download the new appsettings.json file.');
    }
  }

  // Connectivity Testing Functions
  function loadTestParameters() {
    // With merged settings, tests read values directly from server settings fields
    const hostName = $('hostName')?.value || '';
    const hostPort = $('hostPort')?.value || '';
    const callingAet = $('callingAet')?.value || '';
    const calledAet = $('calledAet')?.value || '';
    const patientId = $('patientId')?.value || '';
    if (!$('testPatientId')?.value && patientId) {
      const el = $('testPatientId');
      if (el) el.value = patientId;
    }
  }

  function generateNetworkTestBatch() {
    const hostName = $('hostName').value || '172.16.0.4';
    const hostPort = $('hostPort').value || '51402';
    const listeningPort = $('listeningPort').value || '104';
    const timestamp = generateTimestamp();
    
    const batchContent = `@echo off
title DICOM Tools - Network Connectivity Test
color 0E

echo ===============================================
echo   DICOM Tools - Network Connectivity Test
echo   Host: ${hostName}:${hostPort}
echo   DicomTools Listening Port: ${listeningPort}
echo   Timestamp: ${timestamp}
echo ===============================================
echo.

echo [TEST 1] Basic Network Connectivity
echo [INFO] Testing ping to ${hostName}...
ping -n 3 ${hostName}
if %ERRORLEVEL% EQU 0 (
    echo [PASS] Host ${hostName} is reachable
) else (
    echo [FAIL] Host ${hostName} is not responding to ping
    echo [INFO] This could indicate network issues or firewall blocking
)
echo.

echo [TEST 2] Port Accessibility Test
echo [INFO] Testing if port ${hostPort} is accessible on ${hostName}...
powershell -Command "try { $tcp = New-Object System.Net.Sockets.TcpClient; $tcp.ReceiveTimeout = 5000; $tcp.SendTimeout = 5000; $tcp.Connect('${hostName}', ${hostPort}); $tcp.Close(); Write-Host '[PASS] Port ${hostPort} is accessible on ${hostName}' -ForegroundColor Green } catch { Write-Host '[FAIL] Cannot connect to ${hostName}:${hostPort}' -ForegroundColor Red; Write-Host '[INFO] Error: ' $_.Exception.Message -ForegroundColor Yellow }"
echo.

echo [TEST 3] DicomTools Listening Port Check
echo [INFO] Checking if port ${listeningPort} is available for DicomTools...
netstat -an | findstr ":${listeningPort} " >nul
if %ERRORLEVEL% EQU 0 (
    echo [WARN] Port ${listeningPort} is already in use
    echo [INFO] Check what's using it: netstat -aon | findstr ":${listeningPort}"
) else (
    echo [PASS] Port ${listeningPort} appears to be available
)
echo.

echo [TEST 4] Network Interface Information
echo [INFO] Your network configuration:
ipconfig | findstr /i "IPv4"
echo.

echo ===============================================
echo [SUMMARY]
echo ===============================================
echo If all tests pass:
echo - Network connectivity to Eclipse is working
echo - DICOM port ${hostPort} is accessible
echo - DicomTools can bind to port ${listeningPort}
echo.
echo If tests fail:
echo - Check network connectivity and firewall settings
echo - Verify Eclipse DICOM service is running
echo - Ensure ports are not blocked
echo ===============================================

echo.
echo Press any key to close...
pause >nul`;

    const filename = `DICOMTools_network_test_${timestamp}.bat`;
    downloadBatchFile(filename, batchContent);
    showConfigMessage(`Network connectivity test created: ${filename}

IMPORTANT: Save this batch file in the same directory as DicomTools.exe before running it.

This test will:
1. Ping the Eclipse host (${hostName})
2. Test port accessibility (${hostPort})
3. Check DicomTools listening port (${listeningPort})
4. Show network configuration

To run:
1. Move ${filename} to your DicomTools directory
2. Double-click the batch file to execute
3. Review the test results for connectivity issues`);
  }

  function generateDicomConnectionTestBatch() {
    const hostName = $('hostName').value || '172.16.0.4';
    const hostPort = $('hostPort').value || '51402';
    const callingAet = $('callingAet').value || 'AKKI';
    const calledAet = $('calledAet').value || 'DD_Eclipse';
    const patientId = $('testPatientId').value || 'test1';
    const timestamp = generateTimestamp();
    // Resolve configured DicomTools.exe path if available
    let configuredExePath = '';
    try {
      const raw = localStorage.getItem(CONFIG_STORAGE_KEY);
      if (raw) {
        const cfg = JSON.parse(raw);
        if (cfg && typeof cfg.dicomToolsPath === 'string') {
          const p = cfg.dicomToolsPath.trim();
          if (p) configuredExePath = /\\DicomTools\.exe$/i.test(p) ? p : (p.replace(/[\\\/]+$/, '') + '\\DicomTools.exe');
        }
      }
    } catch {}
    
    const batchContent = `@echo off
title DICOM Tools - DICOM Connection Test
color 0A

echo ===============================================
echo   DICOM Tools - DICOM Connection Test
echo   Testing DICOM Query capability
echo   Timestamp: ${timestamp}
echo ===============================================
echo.

REM Resolve DicomTools.exe path
set "_DT_EXE=${configuredExePath ? configuredExePath.replace(/"/g, '""') : '%~dp0DicomTools.exe'}"
if not exist "%_DT_EXE%" (
    echo [ERROR] DicomTools.exe not found at "%_DT_EXE%"
    echo Configure path in Settings -> DICOM Server Settings -> DicomTools Path
    pause
    exit /b 1
)

echo [INFO] Test Parameters:
echo [INFO] Host: ${hostName}:${hostPort}
echo [INFO] Calling AET: ${callingAet}
echo [INFO] Called AET: ${calledAet}
echo [INFO] Test Patient ID: ${patientId}
echo.

echo [TEST] DICOM Query Test (Find Studies)
echo [INFO] Testing if DicomTools can query Eclipse for patient studies...
echo [CMD] "%_DT_EXE%" retrieve --patientId "${patientId}" --path "C:\\temp\\dicom_test_${timestamp}" --hostName "${hostName}" --hostPort "${hostPort}" --callingAet "${callingAet}" --calledAet "${calledAet}"
echo.

REM Create temp directory
if not exist "C:\\temp\\dicom_test_${timestamp}" mkdir "C:\\temp\\dicom_test_${timestamp}"

REM Execute the test
"%_DT_EXE%" retrieve --patientId "${patientId}" --path "C:\\temp\\dicom_test_${timestamp}" --hostName "${hostName}" --hostPort "${hostPort}" --callingAet "${callingAet}" --calledAet "${calledAet}"

set RESULT_CODE=%ERRORLEVEL%

echo.
echo ===============================================
echo [ANALYSIS]
echo ===============================================
echo Exit Code: %RESULT_CODE%
echo.

if %RESULT_CODE% EQU 0 (
    echo [SUCCESS] DICOM connection successful!
    echo [INFO] DicomTools can communicate with Eclipse
    echo.
    if exist "C:\\temp\\dicom_test_${timestamp}" (
        for /f %%i in ('dir /b "C:\\temp\\dicom_test_${timestamp}" 2^>nul ^| find /c /v ""') do set FILE_COUNT=%%i
        if defined FILE_COUNT (
            if !FILE_COUNT! GTR 0 (
                echo [SUCCESS] Retrieved !FILE_COUNT! files - Full retrieve working!
            ) else (
                echo [PARTIAL] Query worked but no files retrieved
                echo [INFO] This suggests Eclipse is not sending files back
                echo [INFO] Check Eclipse configuration for AET: ${callingAet}
            )
        )
    )
) else (
    echo [FAIL] DICOM connection failed
    echo [INFO] Possible causes:
    echo - Eclipse DICOM service not running
    echo - Wrong host/port configuration
    echo - AET names not recognized by Eclipse
    echo - Network/firewall blocking DICOM traffic
)

echo.
echo [CLEANUP] Removing temp directory...
if exist "C:\\temp\\dicom_test_${timestamp}" rmdir /s /q "C:\\temp\\dicom_test_${timestamp}" 2>nul

echo ===============================================
echo Press any key to close...
pause >nul`;

    const filename = `DICOMTools_dicom_connection_test_${timestamp}.bat`;
    downloadBatchFile(filename, batchContent);
    showConfigMessage(`DICOM connection test created: ${filename}

${configuredExePath ? 'This test uses your configured DicomTools.exe path and can be run from any location.' : 'IMPORTANT: Either save this batch file in the same directory as DicomTools.exe, or set the DicomTools Path in Settings before running it.'}

This test will:
1. Verify DicomTools.exe is present
2. Test DICOM query capability with Eclipse
3. Check if files are successfully retrieved
4. Provide detailed analysis of results

Parameters:
- Host: ${hostName}:${hostPort}
- AETs: ${callingAet} → ${calledAet}
- Test Patient: ${patientId}

To run:
1. Double-click the batch file to execute
2. Check the results to verify DICOM communication`);
  }

  function generatePortListeningTestBatch() {
    const listeningPort = $('listeningPort').value || '104';
    const timestamp = generateTimestamp();
    // Resolve configured DicomTools.exe path if available
    let configuredExePath = '';
    try {
      const raw = localStorage.getItem(CONFIG_STORAGE_KEY);
      if (raw) {
        const cfg = JSON.parse(raw);
        if (cfg && typeof cfg.dicomToolsPath === 'string') {
          const p = cfg.dicomToolsPath.trim();
          if (p) configuredExePath = /\\DicomTools\.exe$/i.test(p) ? p : (p.replace(/[\\\/]+$/, '') + '\\DicomTools.exe');
        }
      }
    } catch {}
    
    const batchContent = `@echo off
title DICOM Tools - Port Listening Test
color 0C

echo ===============================================
echo   DICOM Tools - Port Listening Test
echo   Testing DicomTools port binding capability
echo   Port: ${listeningPort}
echo   Timestamp: ${timestamp}
echo ===============================================
echo.

REM Resolve DicomTools.exe path
set "_DT_EXE=${configuredExePath ? configuredExePath.replace(/"/g, '""') : '%~dp0DicomTools.exe'}"
if not exist "%_DT_EXE%" (
    echo [ERROR] DicomTools.exe not found at "%_DT_EXE%"
    echo Configure path in Settings -> DICOM Server Settings -> DicomTools Path
    pause
    exit /b 1
)

echo [TEST 1] Current Port Usage
echo [INFO] Checking what's currently using port ${listeningPort}...
netstat -aon | findstr ":${listeningPort}"
if %ERRORLEVEL% EQU 0 (
    echo [WARN] Port ${listeningPort} is already in use
    echo [INFO] DicomTools may not be able to bind to this port
) else (
    echo [PASS] Port ${listeningPort} is available
)
echo.

echo [TEST 2] Administrator Privileges Check
net session >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [PASS] Running with administrator privileges
    echo [INFO] Can bind to port ${listeningPort} (including ports below 1024)
) else (
    if ${listeningPort} LSS 1024 (
        echo [WARN] Not running as administrator
        echo [INFO] Port ${listeningPort} requires administrator privileges
        echo [INFO] Either run as admin or use a port above 1024 (e.g., 11112)
    ) else (
        echo [PASS] Non-administrator mode OK for port ${listeningPort}
    )
)
echo.

echo [TEST 3] DicomTools Port Binding Test
echo [INFO] Starting DicomTools briefly to test port binding...
echo [INFO] This will start DicomTools with a dummy command for 10 seconds...

REM Start DicomTools in background with a timeout
start /B "DicomTools Port Test" cmd /c "timeout /t 10 /nobreak >nul 2>nul && taskkill /f /im DicomTools.exe >nul 2>nul"
start /B "DicomTools Test" "%_DT_EXE%" retrieve --patientId "port_test" --path "C:\\temp\\port_test" --hostName "127.0.0.1" --hostPort "9999" --callingAet "TEST" --calledAet "TEST"

REM Wait for DicomTools to start
echo [INFO] Waiting for DicomTools to start...
timeout /t 3 /nobreak >nul

echo [TEST 4] Verify Port Binding
echo [INFO] Checking if DicomTools successfully bound to port ${listeningPort}...
netstat -an | findstr ":${listeningPort}.*LISTENING"
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] DicomTools successfully bound to port ${listeningPort}
    echo [INFO] Port configuration is working correctly
    echo [INFO] The connection error above is expected (dummy connection test)
) else (
    echo [INFO] Checking DicomTools debug output for port binding...
    REM Look for the specific debug message that indicates successful binding
    echo [INFO] If you see "Waiting for inbound client connection to 0.0.0.0:${listeningPort}" above,
    echo [INFO] then port binding is actually SUCCESSFUL despite the connection error.
    echo [INFO] The connection error is from the test trying to connect to a non-existent host.
    echo.
    echo [ANALYSIS] DicomTools Port Binding Status:
    REM Check if we can see the binding message in output
    echo [INFO] - Look for: "Waiting for inbound client connection to 0.0.0.0:${listeningPort}"
    echo [INFO] - If present: Port binding is WORKING
    echo [INFO] - If missing: Port binding failed
)

echo.
echo [TEST 5] Cleanup
echo [INFO] Stopping DicomTools test instance...
taskkill /f /im DicomTools.exe >nul 2>nul
if exist "C:\\temp\\port_test" rmdir /s /q "C:\\temp\\port_test" >nul 2>nul

echo.
echo ===============================================
echo [RESULT INTERPRETATION]
===============================================
echo IMPORTANT: This test has two parts:
echo 1. Port Binding Test - Can DicomTools listen on port ${listeningPort}?
echo 2. Connection Test - Can DicomTools connect to a dummy host?
echo.
echo KEY INDICATORS:
echo ✓ SUCCESS: "Waiting for inbound client connection to 0.0.0.0:${listeningPort}"
echo ✓ SUCCESS: netstat shows port ${listeningPort} LISTENING
echo ✗ IGNORE: "No connection could be made" (dummy connection test)
echo.
echo If you see the "Waiting for inbound client connection" message,
echo your port configuration is WORKING CORRECTLY!
echo.
echo ===============================================
echo [TROUBLESHOOTING GUIDE]
===============================================
echo If port binding actually failed:
echo.
echo 1. Port already in use:
echo    - Use: netstat -aon | findstr ":${listeningPort}"
echo    - Stop the conflicting service or use a different port
echo.
echo 2. Permission denied (port ${listeningPort}):
if ${listeningPort} LSS 1024 (
    echo    - Run Command Prompt as Administrator
    echo    - OR use a high port like 11112 in configuration
) else (
    echo    - Check Windows Firewall settings
    echo    - Verify no other restrictions
)
echo.
echo 3. Firewall blocking:
    echo    - Add DicomTools.exe to Windows Firewall exceptions
    echo    - Check corporate firewall settings
echo.
echo COMMON CONFUSION:
echo The "connection refused" error is NORMAL and EXPECTED.
echo This occurs because the test tries to connect to a non-existent host (127.0.0.1:9999).
echo What matters is whether DicomTools can LISTEN on port ${listeningPort}.
echo ===============================================

echo.
echo Press any key to close...
pause >nul`;

    const filename = `DICOMTools_port_listening_test_${timestamp}.bat`;
    downloadBatchFile(filename, batchContent);
    showConfigMessage(`Port listening test created: ${filename}

${configuredExePath ? 'This test uses your configured DicomTools.exe path and can be run from any location.' : 'IMPORTANT: Either save this batch file in the same directory as DicomTools.exe, or set the DicomTools Path in Settings before running it.'}

This test will:
1. Check if port ${listeningPort} is available
2. Verify administrator privileges (if needed)
3. Test DicomTools port binding capability
4. Provide troubleshooting guidance

To run:
1. Double-click the batch file to execute
2. Look for "Waiting for inbound client connection" message (indicates SUCCESS)

This test helps ensure DicomTools can receive DICOM files from Eclipse.`);
  }

  function generateCompleteDignosticBatch() {
    const hostName = $('hostName').value || '172.16.0.4';
    const hostPort = $('hostPort').value || '51402';
    const callingAet = $('callingAet').value || 'AKKI';
    const calledAet = $('calledAet').value || 'DD_Eclipse';
    const patientId = $('testPatientId').value || 'test1';
    const listeningPort = $('listeningPort').value || '104';
    const timestamp = generateTimestamp();
    // Resolve configured DicomTools.exe path if available
    let configuredExePath = '';
    try {
      const raw = localStorage.getItem(CONFIG_STORAGE_KEY);
      if (raw) {
        const cfg = JSON.parse(raw);
        if (cfg && typeof cfg.dicomToolsPath === 'string') {
          const p = cfg.dicomToolsPath.trim();
          if (p) configuredExePath = /\\DicomTools\.exe$/i.test(p) ? p : (p.replace(/[\\\/]+$/, '') + '\\DicomTools.exe');
        }
      }
    } catch {}
    
    const batchContent = `@echo off
title DICOM Tools - Complete Diagnostic Suite
color 0F

echo ===============================================
echo   DICOM Tools - Complete Diagnostic Suite
echo   Comprehensive connectivity and configuration test
echo   Timestamp: ${timestamp}
echo ===============================================
echo.

REM Resolve DicomTools.exe path
set "_DT_EXE=${configuredExePath ? configuredExePath.replace(/"/g, '""') : '%~dp0DicomTools.exe'}"
if not exist "%_DT_EXE%" (
    echo [ERROR] DicomTools.exe not found at "%_DT_EXE%"
    echo Configure path in Settings -> DICOM Server Settings -> DicomTools Path
    pause
    exit /b 1
)

REM Set test parameters
set HOST_NAME=${hostName}
set HOST_PORT=${hostPort}
set CALLING_AET=${callingAet}
set CALLED_AET=${calledAet}
set PATIENT_ID=${patientId}
set LISTENING_PORT=${listeningPort}

echo [INFO] Test Configuration:
echo [INFO] Eclipse Host: %HOST_NAME%:%HOST_PORT%
echo [INFO] AET Mapping: %CALLING_AET% → %CALLED_AET%
echo [INFO] Test Patient: %PATIENT_ID%
echo [INFO] DicomTools Port: %LISTENING_PORT%
echo [INFO] Test Directory: C:\\temp\\complete_diagnostic_%date:~-4,4%%date:~-10,2%%date:~-7,2%
echo.

REM Create test directory
set TEST_DIR=C:\\temp\\complete_diagnostic_%date:~-4,4%%date:~-10,2%%date:~-7,2%
if not exist "%TEST_DIR%" mkdir "%TEST_DIR%"

echo ===============================================
echo [PHASE 1] SYSTEM CHECKS
echo ===============================================

echo [1.1] DicomTools Executable Check
if exist "DicomTools.exe" (
    echo [PASS] DicomTools.exe found
    for %%A in (DicomTools.exe) do echo [INFO] Size: %%~zA bytes, Modified: %%~tA
) else (
    echo [FAIL] DicomTools.exe not found in current directory
    echo [ERROR] Cannot proceed without DicomTools.exe
    goto :cleanup
)

echo.
echo [1.2] Configuration File Check
if exist "appsettings.json" (
    echo [PASS] appsettings.json found
    findstr "PortNumber" appsettings.json | findstr "%LISTENING_PORT%" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [PASS] Listening port %LISTENING_PORT% configured correctly
    ) else (
        echo [WARN] appsettings.json may not have port %LISTENING_PORT%
        echo [INFO] Current configuration:
        findstr "PortNumber" appsettings.json
    )
) else (
    echo [WARN] appsettings.json not found - using defaults
)

echo.
echo ===============================================
echo [PHASE 2] NETWORK CONNECTIVITY
echo ===============================================

echo [2.1] Basic Network Test
ping -n 2 %HOST_NAME% >nul
if %ERRORLEVEL% EQU 0 (
    echo [PASS] Host %HOST_NAME% is reachable
) else (
    echo [FAIL] Host %HOST_NAME% is not responding
    echo [WARN] This will prevent DICOM communication
)

echo.
echo [2.2] DICOM Port Accessibility
powershell -Command "try { $tcp = New-Object System.Net.Sockets.TcpClient; $tcp.ReceiveTimeout = 3000; $tcp.SendTimeout = 3000; $tcp.Connect('%HOST_NAME%', %HOST_PORT%); $tcp.Close(); Write-Host '[PASS] DICOM port %HOST_PORT% is accessible' -ForegroundColor Green } catch { Write-Host '[FAIL] Cannot connect to %HOST_NAME%:%HOST_PORT%' -ForegroundColor Red }"

echo.
echo [2.3] Local Port Availability
netstat -an | findstr ":%LISTENING_PORT% " >nul
if %ERRORLEVEL% EQU 0 (
    echo [WARN] Port %LISTENING_PORT% is already in use
    echo [INFO] This may prevent DicomTools from starting
    netstat -aon | findstr ":%LISTENING_PORT%"
) else (
    echo [PASS] Port %LISTENING_PORT% is available
)

echo.
echo ===============================================
echo [PHASE 3] DICOM COMMUNICATION TEST
echo ===============================================

echo [3.1] DICOM Query Test
echo [INFO] Testing DICOM query capability...
echo [CMD] "%_DT_EXE%" retrieve --patientId "%PATIENT_ID%" --path "%TEST_DIR%" --hostName "%HOST_NAME%" --hostPort "%HOST_PORT%" --callingAet "%CALLING_AET%" --calledAet "%CALLED_AET%"

"%_DT_EXE%" retrieve --patientId "%PATIENT_ID%" --path "%TEST_DIR%" --hostName "%HOST_NAME%" --hostPort "%HOST_PORT%" --callingAet "%CALLING_AET%" --calledAet "%CALLED_AET%"

set DICOM_RESULT=%ERRORLEVEL%

echo.
echo [3.2] Results Analysis
if %DICOM_RESULT% EQU 0 (
    echo [PASS] DICOM operation completed successfully
    
    REM Check if files were actually retrieved
    set /a FILE_COUNT=0
    for /f %%i in ('dir /b "%TEST_DIR%" 2^>nul ^| find /c /v ""') do set FILE_COUNT=%%i
    
    if %FILE_COUNT% GTR 0 (
        echo [SUCCESS] Retrieved %FILE_COUNT% files
        echo [INFO] Full DICOM retrieve workflow is working correctly
        dir "%TEST_DIR%" /B
    ) else (
        echo [PARTIAL] Query succeeded but no files retrieved
        echo [ISSUE] Eclipse found the study but didn't send files back
        echo [CHECK] Eclipse configuration for AET: %CALLING_AET%
        echo [CHECK] Eclipse return IP/port configuration
    )
) else (
    echo [FAIL] DICOM operation failed with exit code %DICOM_RESULT%
    echo [ISSUE] Could not establish DICOM communication with Eclipse
)

echo.
echo ===============================================
echo [PHASE 4] SYSTEM INFORMATION
echo ===============================================

echo [4.1] Network Configuration
ipconfig | findstr /i "IPv4"

echo.
echo [4.2] Firewall Status
netsh advfirewall show currentprofile | findstr "State"

echo.
echo [4.3] Current User Context
whoami
net session >nul 2>&1 && echo [INFO] Running with administrator privileges || echo [INFO] Running with standard user privileges

echo.
echo ===============================================
echo [DIAGNOSTIC SUMMARY]
echo ===============================================

if %DICOM_RESULT% EQU 0 (
    if %FILE_COUNT% GTR 0 (
        echo [OVERALL] ✓ COMPLETE SUCCESS - All systems working
        echo [STATUS] DicomTools can successfully retrieve files from Eclipse
    ) else (
        echo [OVERALL] ⚠ PARTIAL SUCCESS - Communication works, file transfer doesn't
        echo [ACTION] Check Eclipse configuration for sending files to %CALLING_AET%
        echo [ACTION] Verify Eclipse knows to send to this machine's IP and port %LISTENING_PORT%
    )
) else (
    echo [OVERALL] ✗ CONNECTION FAILED
    echo [ACTION] Review network connectivity and DICOM configuration
    echo [ACTION] Check Eclipse DICOM service status
    echo [ACTION] Verify AET names are configured in Eclipse
)

echo.
echo [RECOMMENDATIONS]
if %DICOM_RESULT% NEQ 0 (
    echo 1. Verify Eclipse DICOM service is running
    echo 2. Check AET configuration in Eclipse for '%CALLING_AET%'
    echo 3. Test network connectivity to %HOST_NAME%:%HOST_PORT%
    echo 4. Review Eclipse logs for connection attempts
) else if %FILE_COUNT% EQU 0 (
    echo 1. Check Eclipse return IP configuration
    echo 2. Verify port %LISTENING_PORT% is correct for your setup
    echo 3. Test with Eclipse administrator
) else (
    echo 1. Configuration is working correctly
    echo 2. You can proceed with normal DICOM operations
)

:cleanup
echo.
echo [CLEANUP] Removing test directory...
if exist "%TEST_DIR%" rmdir /s /q "%TEST_DIR%" 2>nul

echo.
echo ===============================================
echo Diagnostic complete. Press any key to close...
pause >nul`;

    const filename = `DICOMTools_complete_diagnostic_${timestamp}.bat`;
    downloadBatchFile(filename, batchContent);
    showConfigMessage(`Complete diagnostic suite created: ${filename}

${configuredExePath ? 'This test uses your configured DicomTools.exe path and can be run from any location.' : 'IMPORTANT: Either save this batch file in the same directory as DicomTools.exe, or set the DicomTools Path in Settings before running it.'}

This comprehensive test includes:

PHASE 1 - System Checks:
• DicomTools.exe verification
• Configuration file validation

PHASE 2 - Network Connectivity:
• Host reachability test
• Port accessibility check
• Local port availability

PHASE 3 - DICOM Communication:
• Full retrieve operation test
• File transfer verification
• Detailed result analysis

PHASE 4 - System Information:
• Network configuration
• Security context
• Firewall status

To run:
1. Double-click the batch file to execute
2. Review the comprehensive diagnostic results

This is the most thorough diagnostic tool - use this for comprehensive troubleshooting!`);
  }

  function downloadBatchFile(filename, content) {
    try {
      const blob = new Blob([content], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.display = 'none';
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error) {
      showMessage('Error: Failed to create test file. ' + error.message);
    }
  }

  // Navigation
  function switchOperation(operation) {
    currentOperation = operation;
    
    const panels = ['panelRetrieve', 'panelStore', 'panelSearch', 'panelShow', 'panelPing', 'panelHelp', 'panelConfig'];
    panels.forEach(id => {
      const panel = $(id);
      if (panel) panel.style.display = 'none';
    });
    
    let panelId;
    switch(operation) {
      case 'export': panelId = 'panelRetrieve'; break;
      case 'import': panelId = 'panelStore'; break;
      case 'search': panelId = 'panelSearch'; break;
      case 'show': panelId = 'panelShow'; break;
      case 'help': panelId = 'panelHelp'; break;
      case 'config': panelId = 'panelConfig'; break;
    }
    
    const activePanel = $(panelId);
    if (activePanel) activePanel.style.display = 'block';
    
    // Auto-populate test parameters when switching to config
    if (operation === 'config') {
      loadTestParameters();
    }
    
    updateNavigationUI(operation);
  }

  function updateNavigationUI(operation) {
    document.querySelectorAll('.dropdown-item').forEach(item => {
      item.classList.remove('active');
      if (item.dataset.operation === operation) {
        item.classList.add('active');
      }
    });
    
    // Update navHelp and navConfig active state
    const navHelp = $('navHelp');
    if (navHelp) {
      navHelp.classList.remove('active');
      if (operation === 'help') {
        navHelp.classList.add('active');
      }
    }
    
    const navConfig = $('navConfig');
    if (navConfig) {
      navConfig.classList.remove('active');
      if (operation === 'config') {
        navConfig.classList.add('active');
      }
    }
    
    const dropdownBtn = $('navDicomOps');
    if (dropdownBtn && ['export', 'import', 'search', 'show'].includes(operation)) {
      const operationNames = { export: 'Export', import: 'Import', search: 'Search', show: 'Show' };
      dropdownBtn.textContent = operationNames[operation] + ' ▼';
    }
  }

  // Setup event handlers
  function setupEventHandlers() {
    document.addEventListener('input', autoSaveForm);
    document.addEventListener('change', autoSaveForm);

    // Anonymization checkbox toggle
    const anonymizeCheckbox = $('anonymize');
    const anonymizationFields = $('anonymizationFields');
    if (anonymizeCheckbox && anonymizationFields) {
      anonymizeCheckbox.addEventListener('change', () => {
        anonymizationFields.style.display = anonymizeCheckbox.checked ? 'block' : 'none';
      });
    }

    // Navigation
    document.querySelectorAll('.dropdown-item').forEach(item => {
      item.addEventListener('click', (e) => {
        const operation = e.target.dataset.operation;
        if (operation) {
          switchOperation(operation);
          const dropdown = document.querySelector('.dropdown-menu');
          if (dropdown) dropdown.classList.remove('show');
        }
      });
    });

  const dropdownBtn = $('navDicomOps');
  const dropdownMenu = $('dicomOpsMenu');
  if (dropdownBtn && dropdownMenu) {
    dropdownBtn.addEventListener('click', (e) => {
      e.stopPropagation();
        dropdownMenu.classList.toggle('show');
      });
      document.addEventListener('click', () => dropdownMenu.classList.remove('show'));
    }

    const navHelp = $('navHelp');
    if (navHelp) navHelp.addEventListener('click', () => switchOperation('help'));

    const navConfig = $('navConfig');
    if (navConfig) navConfig.addEventListener('click', () => switchOperation('config'));

    // Execute buttons
    if ($('btnRetrieve')) $('btnRetrieve').addEventListener('click', executeRetrieve);
    if ($('btnStore')) $('btnStore').addEventListener('click', executeStore);
    if ($('btnSearch')) $('btnSearch').addEventListener('click', executeSearch);
    if ($('btnShow')) $('btnShow').addEventListener('click', executeShow);
    // Ping functionality removed

    // Test DicomTools button removed

    // Clear buttons
    document.querySelectorAll('#btnClear').forEach(btn => {
      btn.addEventListener('click', () => {
        const activePanel = document.querySelector('.panel:not([style*="display: none"])');
        if (activePanel) {
          activePanel.querySelectorAll('input').forEach(input => {
            if (input.type === 'checkbox') {
              input.checked = false;
      } else {
              input.value = '';
            }
          });
          autoSaveForm();
        }
      });
    });

    // Save buttons
    document.querySelectorAll('[id^="btnSave"]').forEach(btn => {
      btn.addEventListener('click', () => {
        autoSaveForm();
        showToast('Settings saved locally!');
      });
    });

    // Configuration buttons
    if ($('btnUpdatePort')) $('btnUpdatePort').addEventListener('click', updatePortConfiguration);
    if ($('btnReloadPort')) $('btnReloadPort').addEventListener('click', loadCurrentConfig);
    if ($('btnUpdateLogging')) $('btnUpdateLogging').addEventListener('click', updatePortConfiguration);
    if ($('btnBackupConfig')) $('btnBackupConfig').addEventListener('click', createConfigBackup);
    if ($('btnResetConfig')) $('btnResetConfig').addEventListener('click', resetConfigToDefaults);
    if ($('btnSaveServerSettings')) $('btnSaveServerSettings').addEventListener('click', () => {
      const config = saveCurrentConfig();
      if (config) {
        showToast('Server settings saved!');
      } else {
        showMessage('Error: Failed to save server settings.');
      }
    });

    // JSON import button
    if ($('btnImportJson')) $('btnImportJson').addEventListener('click', importPatientJson);

    // Connectivity test buttons
    if ($('btnTestNetworkConnectivity')) $('btnTestNetworkConnectivity').addEventListener('click', generateNetworkTestBatch);
    if ($('btnTestDicomConnection')) $('btnTestDicomConnection').addEventListener('click', generateDicomConnectionTestBatch);
    if ($('btnTestPortListening')) $('btnTestPortListening').addEventListener('click', generatePortListeningTestBatch);
    if ($('btnTestComplete')) $('btnTestComplete').addEventListener('click', generateCompleteDignosticBatch);

    // Remove Load Results buttons since we don't need them anymore
    document.querySelectorAll('button').forEach(btn => {
      if (btn.textContent === 'Load Results') {
        btn.remove();
      }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
      if (e.ctrlKey && e.shiftKey && e.key === 'X') {
        if (confirm('Clear all saved settings?')) {
          localStorage.removeItem(STORAGE_KEY);
          location.reload();
        }
      }
    });
  }


  // Setup dynamic machine mapping functionality
  function setupMachineMapping() {
    const container = $('machineMappingContainer');
    const addButton = $('addMachineMapping');
    
    if (!container || !addButton) return;
    
    // Add new machine mapping row
    addButton.addEventListener('click', () => {
      createMachineMappingRow(container);
      autoSaveForm(); // Save after adding new row
    });
    
    // Add event listeners to existing remove buttons
    container.addEventListener('click', (e) => {
      if (e.target.classList.contains('remove-mapping')) {
        if (container.children.length > 1) {
          e.target.closest('.machine-mapping-item').remove();
          autoSaveForm(); // Save after removal
        }
      }
    });
    
    // Auto-save when inputs change
    container.addEventListener('input', autoSaveForm);
  }
  
  // Get machine mappings as array for individual command line parameters
  function getMachineMappings() {
    const container = $('machineMappingContainer');
    if (!container) return [];
    
    const mappings = [];
    const items = container.querySelectorAll('.machine-mapping-item');
    
    items.forEach(item => {
      const fromInput = item.querySelector('.machine-from');
      const toInput = item.querySelector('.machine-to');
      
      if (fromInput && toInput && fromInput.value.trim() && toInput.value.trim()) {
        mappings.push(`${fromInput.value.trim()}=${toInput.value.trim()}`);
      }
    });
    
    return mappings;
  }
  
  // Load machine mappings from saved data
  function loadMachineMappings(savedMappings) {
    const container = $('machineMappingContainer');
    if (!container || !savedMappings || !Array.isArray(savedMappings)) return;
    
    // Clear existing mappings
    container.innerHTML = '';
    
    // If no saved mappings, create one empty row
    if (savedMappings.length === 0) {
      createMachineMappingRow(container);
      return;
    }
    
    // Create rows for each saved mapping
    savedMappings.forEach(mapping => {
      const parts = mapping.split('=');
      if (parts.length === 2) {
        createMachineMappingRow(container, parts[0].trim(), parts[1].trim());
      }
    });
  }
  
  // Create a machine mapping row
  function createMachineMappingRow(container, fromValue = '', toValue = '') {
    const newItem = document.createElement('div');
    newItem.className = 'machine-mapping-item';
    newItem.innerHTML = `
      <input type="text" class="machine-from" placeholder="From (e.g., A)" value="${fromValue}" />
      <span>=</span>
      <input type="text" class="machine-to" placeholder="To (e.g., B)" value="${toValue}" />
      <button type="button" class="remove-mapping" style="background: var(--error); color: white; border: none; padding: 4px 8px; margin-left: 8px; border-radius: 3px; cursor: pointer;">−</button>
    `;
    container.appendChild(newItem);
    
    // Add event listeners to the new inputs
    const inputs = newItem.querySelectorAll('input');
    inputs.forEach(input => {
      input.addEventListener('input', autoSaveForm);
    });
    
    // Add event listener to the remove button
    const removeBtn = newItem.querySelector('.remove-mapping');
    removeBtn.addEventListener('click', () => {
      if (container.children.length > 1) {
        newItem.remove();
        autoSaveForm(); // Save after removal
      }
    });
  }

  // Initialize
  function init() {
    console.log('🚀 DICOM Tools - Batch File Generation Mode');
    
    autoLoadForm();
    loadCurrentConfig();
    setupEventHandlers();
    setupMachineMapping();
    // Attempt to populate via injected globals, then URL, then handoff files
    tryLoadFromGlobal();
    tryLoadFromUrlParams();
    tryLoadHandoffData();
    restoreImportedPatientData();
    
    // Initialize anonymization fields visibility based on checkbox state
    const anonymizeCheckbox = $('anonymize');
    const anonymizationFields = $('anonymizationFields');
    if (anonymizeCheckbox && anonymizationFields) {
      anonymizationFields.style.display = anonymizeCheckbox.checked ? 'block' : 'none';
    }
    
    switchOperation('export');
  }

  function tryLoadFromGlobal() {
    try {
      // Check for handoff data from Patient List Builder (via handoff_patients.js)
      console.log('tryLoadFromGlobal: checking for window.HANDOFF_FULL...', typeof window.HANDOFF_FULL);
      
      if (typeof window !== 'undefined' && typeof window.HANDOFF_FULL === 'object' && window.HANDOFF_FULL !== null) {
        const handoffJson = window.HANDOFF_FULL;
        console.log('✅ Found window.HANDOFF_FULL:', handoffJson);
        
        // Extract patient IDs from the full handoff data
        try {
          const extractedIds = extractPatientIds(handoffJson);
          console.log('Extracted patient IDs:', extractedIds);
          
          if (extractedIds.length > 0) {
            updatePatientIdField(extractedIds, handoffJson);
            console.log('✅ Updated patient ID field and should show message');
            autoSaveForm();
          } else {
            console.warn('No patient IDs extracted from handoff data');
          }
        } catch (e) {
          console.error('Error extracting data from HANDOFF_FULL:', e);
        }
      } else {
        console.log('No window.HANDOFF_FULL found (this is normal if not launched from Patient List Builder)');
      }
    } catch (e) {
      console.error('Error in tryLoadFromGlobal:', e);
    }
  }

  function tryLoadFromUrlParams() {
    try {
      const usp = new URLSearchParams(window.location.search);
      const ids = (usp.get('patientIds') || '').trim();
      if (ids) {
        const el = $('patientId');
        if (el) el.value = ids;
        showMessage(`Loaded ${ids.split(';').filter(x=>x.trim()).length} patient ID(s) from URL.`);
        autoSaveForm();
        const patientIds = ids.split(';').map(p=>p.trim()).filter(Boolean);
        if (patientIds.length > 0) {
          updatePatientDisplayUI(patientIds, {});
        }
      }
    } catch (e) {
      // ignore
    }
  }

  async function tryLoadHandoffData() {
    // When launched via file://, fetching local files is blocked by CORS; rely on window.HANDOFF_FULL instead
    try {
      if (typeof window !== 'undefined' && window.location && window.location.protocol === 'file:') {
        return;
      }
    } catch {}

    // Load handoff JSON (if available via HTTP)
    try {
      const jsonResp = await fetch('handoff_patients.json', { cache: 'no-store' });
      if (jsonResp && jsonResp.ok) {
        const jsonData = await jsonResp.json();
        try { sessionStorage.setItem('importedPatientData', JSON.stringify(jsonData)); } catch {}
        const ids = extractPatientIds(jsonData);
        if (ids && ids.length > 0) {
          updatePatientIdField(ids, jsonData);
          showMessage(`Loaded ${ids.length} patient(s) from handoff data.`);
          autoSaveForm();
        }
      }
    } catch (e) {
      // Handoff file not present or fetch failed - this is normal if not launched from Patient List Builder
    }
  }
  
  function restoreImportedPatientData() {
    const importedData = sessionStorage.getItem('importedPatientData');
    if (importedData) {
      try {
        const jsonData = JSON.parse(importedData);
        const patientIds = extractPatientIds(jsonData);
        
        if (patientIds.length > 0) {
          updatePatientDisplayUI(patientIds, jsonData);
        }
      } catch (error) {
        console.warn('Could not restore imported patient data:', error);
        sessionStorage.removeItem('importedPatientData');
      }
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();