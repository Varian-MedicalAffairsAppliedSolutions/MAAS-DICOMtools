using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ESAPIPatientBrowser.Models;
using ESAPIPatientBrowser.Services;
using ESAPIPatientBrowser.Views;
using ESAPIApp = VMS.TPS.Common.Model.API.Application;

namespace ESAPIPatientBrowser.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PatientSearchService _searchService;
        private readonly JsonExportService _jsonExportService;
        private readonly CombinedAppService _combinedAppService;
        private readonly AdvancedSearchService _deepSearchService;
        private readonly ESAPIApp _esapiApp;
        
        private string _searchText;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private bool _isSearching;
        private string _statusMessage;
        private PatientInfo _selectedPatient;
        private ObservableCollection<PatientInfo> _patients;
        private PatientPlanCollection _currentCollection;
        private ObservableCollection<PatientInfo> _searchSuggestions;
        private bool _isSearchSuggestionsVisible;
        private PatientInfo _selectedSuggestion;
        private ObservableCollection<string> _filteredPatientIds;
        private bool _isPatientDropdownOpen;
        private bool _isInitialized = false; // Flag to prevent early ESAPI calls
        private bool _esapiBusy = false; // Guard to prevent concurrent ESAPI access
        private double _searchProgress = 0;
        private bool _showOnlyPatientsWithPlans = true;
        private List<PatientInfo> _allPatients = new List<PatientInfo>(); // Unfiltered list for filtering
        private CancellationTokenSource _searchCancellationTokenSource;

        public MainViewModel(ESAPIApp esapiApplication, string initialPatientId = null)
        {
            _esapiApp = esapiApplication;
            
            // Handle deferred ESAPI initialization
            if (esapiApplication != null)
            {
                _searchService = new PatientSearchService(esapiApplication);
                _deepSearchService = new AdvancedSearchService(esapiApplication);
            }
            
            _jsonExportService = new JsonExportService();
            _combinedAppService = new CombinedAppService(_jsonExportService);

            Patients = new ObservableCollection<PatientInfo>();
            SearchSuggestions = new ObservableCollection<PatientInfo>();
            FilteredPatientIds = new ObservableCollection<string>();
            _currentCollection = new PatientPlanCollection();

            // Initialize commands
            SearchCommand = new RelayCommand(async () => await SearchPatientsAsync(), () => !IsSearching);
            RefreshCommand = new RelayCommand(async () => await RefreshCurrentSearchAsync(), () => !IsSearching);
            AdvancedSearchCommand = new RelayCommand(async () => await PerformAdvancedSearchAsync(), () => !IsSearching && _deepSearchService != null);
            CancelSearchCommand = new RelayCommand(CancelSearch, () => IsSearching);
            SelectAllPatientsCommand = new RelayCommand(SelectAllPatients);
            ClearSelectionCommand = new RelayCommand(ClearSelection);
            ClearPatientListCommand = new RelayCommand(ClearPatientList);
            LoadPatientPlansCommand = new RelayCommand<PatientInfo>(async (patient) => await LoadPatientPlansAsync(patient));
            ExportToJsonCommand = new RelayCommand(ExportToJson, CanExportToJson);
            ImportFromJsonCommand = new RelayCommand(ImportFromJson);
            SendToCombinedAppCommand = new RelayCommand(async () => await SendToCombinedAppAsync(), CanSendToCombinedApp);
            LaunchCombinedAppCommand = new RelayCommand(async () => await LaunchCombinedAppAsync());
            OpenCombinedAppIndexCommand = new RelayCommand(OpenCombinedAppIndex);

            // Plan selection commands (operate on SelectedPatient)
            LoadAllPlansForSelectedPatientCommand = new RelayCommand(async () =>
            {
                if (SelectedPatient != null)
                {
                    await LoadPatientPlansAsync(SelectedPatient);
                }
            }, () => SelectedPatient != null && !IsSearching);

            SelectAllPlansForSelectedPatientCommand = new RelayCommand(() =>
            {
                if (SelectedPatient?.Plans != null)
                {
                    foreach (var plan in SelectedPatient.Plans)
                    {
                        plan.IsSelected = true;
                    }
                    OnPropertyChanged(nameof(SelectionSummary));
                }
            }, () => SelectedPatient?.Plans?.Any() == true);

            SelectApprovedPlansForSelectedPatientCommand = new RelayCommand(() =>
            {
                if (SelectedPatient?.Plans != null)
                {
                    foreach (var plan in SelectedPatient.Plans)
                    {
                        plan.IsSelected = IsPlanningApprovedStatus(plan.ApprovalStatus);
                    }
                    OnPropertyChanged(nameof(SelectionSummary));
                }
            }, () => SelectedPatient?.Plans?.Any() == true);

            SelectMostRecentPlanForSelectedPatientCommand = new RelayCommand(() =>
            {
                if (SelectedPatient?.Plans != null && SelectedPatient.Plans.Count > 0)
                {
                    var mostRecent = SelectedPatient.Plans
                        .OrderByDescending(p => p.CreationDate ?? DateTime.MinValue)
                        .FirstOrDefault();
                    foreach (var plan in SelectedPatient.Plans)
                    {
                        plan.IsSelected = plan == mostRecent;
                    }
                    OnPropertyChanged(nameof(SelectionSummary));
                }
            }, () => SelectedPatient?.Plans?.Any() == true);

            // Operate across all patients in the list
            LoadAllPlansForListCommand = new RelayCommand(async () =>
            {
                var total = Patients?.Count ?? 0;
                if (total == 0) return;
                int loaded = 0;
                StatusMessage = $"Loading plans for {total} patients...";
                foreach (var patient in Patients.ToList())
                {
                    await LoadPatientPlansAsync(patient);
                    loaded++;
                    StatusMessage = $"Loaded plans {loaded}/{total}";
                }
                StatusMessage = $"Loaded plans for {total} patients.";
            }, () => (Patients?.Any() ?? false) && !IsSearching);

            SelectAllPlansForListCommand = new RelayCommand(() =>
            {
                if (Patients == null) return;
                foreach (var patient in Patients)
                {
                    if (patient.Plans == null) continue;
                    foreach (var plan in patient.Plans)
                    {
                        plan.IsSelected = true;
                    }
                }
                OnPropertyChanged(nameof(SelectionSummary));
            }, () => Patients?.Any(p => p.Plans != null && p.Plans.Any()) == true);

            SelectApprovedPlansForListCommand = new RelayCommand(() =>
            {
                if (Patients == null) return;
                foreach (var patient in Patients)
                {
                    if (patient.Plans == null) continue;
                    foreach (var plan in patient.Plans)
                    {
                        plan.IsSelected = IsPlanningApprovedStatus(plan.ApprovalStatus);
                    }
                }
                OnPropertyChanged(nameof(SelectionSummary));
            }, () => Patients?.Any(p => p.Plans != null && p.Plans.Any()) == true);

            SelectMostRecentPlanForListCommand = new RelayCommand(() =>
            {
                if (Patients == null) return;
                foreach (var patient in Patients)
                {
                    if (patient.Plans == null || patient.Plans.Count == 0) continue;
                    var mostRecent = patient.Plans
                        .OrderByDescending(pl => pl.CreationDate ?? DateTime.MinValue)
                        .FirstOrDefault();
                    foreach (var plan in patient.Plans)
                    {
                        plan.IsSelected = plan == mostRecent;
                    }
                }
                OnPropertyChanged(nameof(SelectionSummary));
            }, () => Patients?.Any(p => p.Plans != null && p.Plans.Any()) == true);

            // Leave date range empty by default - user can specify if needed
            ToDate = null;
            FromDate = null;

            // Always start with empty search - don't auto-populate from Eclipse context
            _searchText = string.Empty;

            StatusMessage = "Ready to search patients";
            
            // Mark as initialized after everything is set up
            _isInitialized = true;
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(); 
                
                // Only trigger ESAPI calls after initialization is complete
                if (_isInitialized && !_esapiBusy)
                {
                    _ = UpdateSearchSuggestionsAsync();
                    UpdateFilteredPatientIds();
                }
            }
        }

        public ObservableCollection<PatientInfo> SearchSuggestions
        {
            get => _searchSuggestions;
            set { _searchSuggestions = value; OnPropertyChanged(); }
        }

        public bool IsSearchSuggestionsVisible
        {
            get => _isSearchSuggestionsVisible;
            set { _isSearchSuggestionsVisible = value; OnPropertyChanged(); }
        }

        public PatientInfo SelectedSuggestion
        {
            get => _selectedSuggestion;
            set 
            { 
                _selectedSuggestion = value; 
                OnPropertyChanged();
                if (value != null)
                {
                    SelectPatientFromSuggestion(value);
                }
            }
        }

        public ObservableCollection<string> FilteredPatientIds
        {
            get => _filteredPatientIds;
            set { _filteredPatientIds = value; OnPropertyChanged(); }
        }

        public bool IsPatientDropdownOpen
        {
            get => _isPatientDropdownOpen;
            set { _isPatientDropdownOpen = value; OnPropertyChanged(); }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set 
            { 
                _isSearching = value; 
                OnPropertyChanged(); 
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public double SearchProgress
        {
            get => _searchProgress;
            set 
            { 
                _searchProgress = value; 
                OnPropertyChanged(); 
            }
        }

        public bool ShowOnlyPatientsWithPlans
        {
            get => _showOnlyPatientsWithPlans;
            set 
            { 
                _showOnlyPatientsWithPlans = value; 
                OnPropertyChanged();
                ApplyPatientFilter();
            }
        }

        public PatientInfo SelectedPatient
        {
            get => _selectedPatient;
            set 
            { 
                _selectedPatient = value; 
                OnPropertyChanged();
                // Update status but don't auto-load plans to avoid crashes
                if (value != null)
                {
                    StatusMessage = $"Selected: {value.FullName} (ID: {value.PatientId}). Use 'Select Plans' button to view treatment plans safely.";
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set 
            { 
                // Unsubscribe from old collection
                if (_patients != null)
                {
                    _patients.CollectionChanged -= OnPatientsCollectionChanged;
                }
                
                _patients = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectionSummary));
                
                // Subscribe to new collection
                if (_patients != null)
                {
                    _patients.CollectionChanged += OnPatientsCollectionChanged;
                }
                
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        private void OnPatientsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SelectionSummary));
            CommandManager.InvalidateRequerySuggested();
        }

        public string SelectionSummary
        {
            get
            {
                var selectedPatients = Patients.Where(p => p.IsSelected).ToList();
                var selectedPlans = selectedPatients.SelectMany(p => p.Plans.Where(pl => pl.IsSelected)).ToList();
                return $"Selected: {selectedPatients.Count} patients, {selectedPlans.Count} plans";
            }
        }

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AdvancedSearchCommand { get; }
        public ICommand CancelSearchCommand { get; }
        public ICommand SelectAllPatientsCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand ClearPatientListCommand { get; }
        public ICommand LoadPatientPlansCommand { get; }
        public ICommand ExportToJsonCommand { get; }
        public ICommand ImportFromJsonCommand { get; }
        public ICommand SendToCombinedAppCommand { get; }
        public ICommand LaunchCombinedAppCommand { get; }
        public ICommand OpenCombinedAppIndexCommand { get; }
        public ICommand LoadAllPlansForSelectedPatientCommand { get; }
        public ICommand SelectAllPlansForSelectedPatientCommand { get; }
        public ICommand SelectApprovedPlansForSelectedPatientCommand { get; }
        public ICommand SelectMostRecentPlanForSelectedPatientCommand { get; }
        public ICommand LoadAllPlansForListCommand { get; }
        public ICommand SelectAllPlansForListCommand { get; }
        public ICommand SelectApprovedPlansForListCommand { get; }
        public ICommand SelectMostRecentPlanForListCommand { get; }

        #endregion

        #region Command Implementations

        private async Task SearchPatientsAsync()
        {
            if (IsSearching) return;

            // Guard: avoid broad search from Add Patient; require likely Patient ID
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "Enter a Patient ID, or use Search.";
                ThemedMessageBox.Show("Please type a Patient ID, or use the Search button to search by name/date filters.",
                                "Add Patient", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!IsLikelyPatientId(SearchText))
            {
                StatusMessage = "Use Search for non-ID queries.";
                ThemedMessageBox.Show("The text entered does not look like a Patient ID. Use the Search button to search by name or date filters.",
                                "Add Patient", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Proceed with specific patient add
            await AddSpecificPatientAsync(SearchText.Trim());
        }

        private async Task AddSpecificPatientAsync(string patientId)
        {
            if (IsSearching) return;

            try
            {
                _esapiBusy = true;
                IsSearching = true;
                StatusMessage = $"Adding patient {patientId}...";

                // Check if patient already exists in the list
                if (Patients.Any(p => p.PatientId.Equals(patientId, StringComparison.OrdinalIgnoreCase)))
                {
                    StatusMessage = $"Patient {patientId} is already in the list";
                    return;
                }

                // First check if patient exists
                if (!_searchService.DoesPatientExist(patientId))
                {
                    StatusMessage = $"Patient {patientId} not found";
                    ThemedMessageBox.Show($"Patient '{patientId}' was not found in the database.", "Patient Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get basic patient info (without plans for faster loading)
                System.Diagnostics.Debug.WriteLine("AddSpecificPatientAsync: calling GetPatientBasicInfo on UI thread");
                var patientInfo = _searchService.GetPatientBasicInfo(patientId);
                
                if (patientInfo != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Subscribe to property changes for selection updates
                        patientInfo.PropertyChanged += OnPatientPropertyChanged;
                        
                        // Add to the existing list (don't clear)
                        Patients.Add(patientInfo);
                        _allPatients.Add(patientInfo);
                        
                        // Clear search text for next search
                        SearchText = "";
                    });

                    StatusMessage = $"Added patient {patientId} to the list";
                    // Auto-load plans for the newly added patient
                    await LoadPatientPlansAsync(patientInfo, allowDuringSearch: true, silent: false);
                    
                    // Apply filter after plans are loaded
                    ApplyPatientFilter();
                }
                else
                {
                    StatusMessage = $"Could not load details for patient {patientId}";
                    ThemedMessageBox.Show($"Could not load details for patient '{patientId}'.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding patient: {ex.Message}";
                ThemedMessageBox.Show($"Error adding patient {patientId}:\n{ex.Message}", "Add Patient Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
                _esapiBusy = false;
            }
        }

        private async Task PerformBroadSearchAsync()
        {
            if (IsSearching) return;

            // Warn user if searching without any text (searches entire database)
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var result = ThemedMessageBox.Show(
                    "No search text entered.\n\n" +
                    "This will search the entire Eclipse database, which may take a long time.\n\n" +
                    "Do you want to continue?",
                    "Search Entire Database",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                {
                    return; // User chose not to continue
                }
            }

            try
            {
                // Create cancellation token source for this search
                _searchCancellationTokenSource = new CancellationTokenSource();
                
                IsSearching = true;
                SearchProgress = 0;
                StatusMessage = "Searching patients...";

                System.Diagnostics.Debug.WriteLine("PerformBroadSearchAsync: calling _searchService.SearchPatients on UI thread");
                var results = _searchService.SearchPatients(SearchText, FromDate, ToDate);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var oldPatient in Patients)
                    {
                        oldPatient.PropertyChanged -= OnPatientPropertyChanged;
                    }
                    Patients.Clear();
                    _allPatients.Clear();
                    foreach (var patient in results)
                    {
                        patient.PropertyChanged += OnPatientPropertyChanged;
                        Patients.Add(patient);
                        _allPatients.Add(patient);
                    }
                });

                // Auto-load plans for all patients in the list sequentially with progress tracking
                var patientList = Patients.ToList();
                int total = patientList.Count;
                int current = 0;
                
                foreach (var p in patientList)
                {
                    // Check for cancellation
                    _searchCancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    current++;
                    SearchProgress = (double)current / total * 100.0;
                    StatusMessage = $"Loading plans for patient {current}/{total}: {p.PatientId}";
                    
                    await LoadPatientPlansAsync(p, allowDuringSearch: true, silent: true);
                    
                    // Yield control to UI every patient to keep responsive
                    await Task.Delay(1, _searchCancellationTokenSource.Token);
                }

                SearchProgress = 100;

                // Apply filter after plans are loaded
                ApplyPatientFilter();

                // Update command states so plan selection buttons become enabled
                CommandManager.InvalidateRequerySuggested();

                StatusMessage = $"Found {results.Count} patients and loaded plans";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Search cancelled by user.";
                System.Diagnostics.Debug.WriteLine("Regular search was cancelled.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search error: {ex.Message}";
                ThemedMessageBox.Show($"Error searching patients:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
                SearchProgress = 0;
                _searchCancellationTokenSource?.Dispose();
                _searchCancellationTokenSource = null;
            }
        }

        private bool IsLikelyPatientId(string searchText)
        {
            // Consider it a patient ID if:
            // - No spaces (patient IDs are typically single tokens)
            // - Not too long (patient IDs are usually short)
            // - No date range specified (specific patient lookup)
            return !searchText.Contains(' ') && 
                   searchText.Length <= 20 && 
                   FromDate == null && 
                   ToDate == null;
        }

        private async Task RefreshCurrentSearchAsync()
        {
            await PerformBroadSearchAsync();
        }

        private async Task<List<PatientInfo>> PerformAdvancedSearchOnUIThreadAsync(DateTime fromDate, DateTime toDate, AdvancedSearchCriteria criteria, string searchText = "", bool filterByHasPlans = false, CancellationToken cancellationToken = default, int? patientLimit = null)
        {
            var results = new List<PatientInfo>();
            
            // Reset progress
            SearchProgress = 0;
            
            System.Diagnostics.Debug.WriteLine($"=== Advanced Search Started ===");
            System.Diagnostics.Debug.WriteLine($"Date range: {fromDate:MM/dd/yyyy} to {toDate:MM/dd/yyyy}");
            System.Diagnostics.Debug.WriteLine($"Search text: '{searchText}'");
            System.Diagnostics.Debug.WriteLine($"Filter by has plans: {filterByHasPlans}");
            System.Diagnostics.Debug.WriteLine($"Patient limit: {(patientLimit.HasValue ? patientLimit.Value.ToString() : "None")}");
            System.Diagnostics.Debug.WriteLine($"Criteria is empty: {criteria?.IsEmpty() ?? true}");
            
            // Get patients in date range
            var patientSummaries = _esapiApp.PatientSummaries
                .Where(ps => ps.CreationDateTime >= fromDate && ps.CreationDateTime <= toDate)
                .OrderBy(ps => ps.Id)
                .ToList();

            // Apply patient search filter if specified
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchUpper = searchText.ToUpper();
                patientSummaries = patientSummaries
                    .Where(ps => ps.Id.ToUpper().Contains(searchUpper) || 
                                 ps.FirstName.ToUpper().Contains(searchUpper) || 
                                 ps.LastName.ToUpper().Contains(searchUpper))
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"After patient search filter: {patientSummaries.Count} patients");
            }

            // PRE-FILTER by patient attributes to avoid opening patients unnecessarily
            int preFilterInitialCount = patientSummaries.Count;
            int preFilterCount = patientSummaries.Count;
            
            // Pre-filter by patient sex if specified
            if (!string.IsNullOrWhiteSpace(criteria.PatientSexContains))
            {
                var sexUpper = criteria.PatientSexContains.ToUpper();
                patientSummaries = patientSummaries
                    .Where(ps => !string.IsNullOrWhiteSpace(ps.Sex) && ps.Sex.ToUpper().Contains(sexUpper))
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"Pre-filter by sex '{criteria.PatientSexContains}': {preFilterCount} → {patientSummaries.Count} patients");
                preFilterCount = patientSummaries.Count;
            }

            // Log pre-filtering results
            if (preFilterCount != preFilterInitialCount)
            {
                StatusMessage = $"Pre-filtered to {patientSummaries.Count} patients based on search criteria (reduced from {preFilterInitialCount})";
                await Task.Delay(500, cancellationToken); // Brief pause to show message
            }

            int total = patientSummaries.Count;
            int current = 0;
            int patientsOpened = 0;
            int patientsWithErrors = 0;
            int patientsSkipped = 0;
            
            System.Diagnostics.Debug.WriteLine($"Found {total} patient summaries to scan");
            System.Diagnostics.Debug.WriteLine($"Criteria: {(criteria.IsEmpty() ? "Empty (match all)" : "Has filters")}");

            foreach (var summary in patientSummaries)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                current++;
                
                // Update progress (0-100)
                SearchProgress = (double)current / total * 100.0;
                StatusMessage = $"Scanning patient {current}/{total}: {summary.Id}";

                // Yield control to UI before each patient to keep UI responsive
                await Task.Delay(1, cancellationToken); // Allows UI to update and handle cancel requests

                VMS.TPS.Common.Model.API.Patient patient = null;
                
                try
                {
                    // Smart error handling: Try to open patient with timeout awareness
                    try
                    {
                        patient = _esapiApp.OpenPatient(summary);
                    }
                    catch (Exception openEx)
                    {
                        // Log specific open errors and skip patient
                        System.Diagnostics.Debug.WriteLine($"Failed to open patient {summary.Id}: {openEx.Message}");
                        StatusMessage = $"Skipping patient {summary.Id} (cannot open): {openEx.Message}";
                        patientsSkipped++;
                        continue;
                    }
                    
                    if (patient == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Patient {summary.Id} returned null");
                        patientsSkipped++;
                        continue;
                    }
                    
                    patientsOpened++;

                    var patientInfo = new PatientInfo
                    {
                        PatientId = patient.Id,
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        DateOfBirth = patient.DateOfBirth,
                        CreationDate = summary.CreationDateTime
                    };

                    // Collect matching plans in a temporary list
                    var matchingPlans = new List<PlanInfo>();
                    int totalPlansScanned = 0;

                    // Scan all courses and plans with per-plan error handling
                    foreach (var course in patient.Courses)
                    {
                        try
                        {
                            // Skip empty or inaccessible courses
                            if (course == null || course.PlanSetups == null || !course.PlanSetups.Any())
                                continue;
                                
                            foreach (var planSetup in course.PlanSetups)
                            {
                                try
                                {
                                    totalPlansScanned++;
                                    var planInfo = _deepSearchService.ExtractDetailedPlanInfo(planSetup);
                                    
                                    // Check if plan matches criteria
                                    if (_deepSearchService.PlanMatchesCriteria(planInfo, criteria))
                                    {
                                        matchingPlans.Add(planInfo);
                                    }
                                }
                                catch (Exception planEx)
                                {
                                    // Log and skip problematic plans without failing entire patient
                                    System.Diagnostics.Debug.WriteLine($"Error processing plan {planSetup?.Id} for patient {patient.Id}: {planEx.Message}");
                                    // Continue to next plan
                                }
                            }
                        }
                        catch (Exception courseEx)
                        {
                            // Log and skip problematic courses
                            System.Diagnostics.Debug.WriteLine($"Error processing course {course?.Id} for patient {patient.Id}: {courseEx.Message}");
                            // Continue to next course
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Patient {patient.Id}: Scanned {totalPlansScanned} plans, {matchingPlans.Count} matched");

                    // Only add patient if they have matching plans
                    if (matchingPlans.Count > 0)
                    {
                        // Set Plans property once to trigger proper event subscription
                        patientInfo.Plans = matchingPlans;
                        results.Add(patientInfo);
                        System.Diagnostics.Debug.WriteLine($"Added patient {patient.Id} with {matchingPlans.Count} plans to results");
                        
                        // Check if we've reached the patient limit
                        if (patientLimit.HasValue && results.Count >= patientLimit.Value)
                        {
                            System.Diagnostics.Debug.WriteLine($"Patient limit of {patientLimit.Value} reached. Stopping search.");
                            StatusMessage = $"Patient limit reached ({patientLimit.Value} patients found). Stopping search...";
                            _esapiApp.ClosePatient();
                            break; // Exit the foreach loop
                        }
                    }

                    _esapiApp.ClosePatient();
                }
                catch (Exception ex)
                {
                    patientsWithErrors++;
                    StatusMessage = $"Error scanning patient {summary.Id}: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Exception scanning patient {summary.Id}: {ex.Message}\n{ex.StackTrace}");
                    try { _esapiApp.ClosePatient(); } catch { }
                }
            }

            // Set to 100% when complete
            SearchProgress = 100;
            
            // Apply "Has plans" filter if specified
            if (filterByHasPlans)
            {
                var originalCount = results.Count;
                results = results.Where(p => p.Plans != null && p.Plans.Count > 0).ToList();
                System.Diagnostics.Debug.WriteLine($"After 'Has plans' filter: {results.Count} patients (filtered out {originalCount - results.Count})");
            }
            
            int totalPlansInResults = results.Sum(p => p.Plans?.Count ?? 0);
            System.Diagnostics.Debug.WriteLine($"=== Advanced Search Complete ===");
            System.Diagnostics.Debug.WriteLine($"Scanned {patientsOpened} patients, {patientsSkipped} skipped, {patientsWithErrors} errors");
            System.Diagnostics.Debug.WriteLine($"Returning {results.Count} patients with {totalPlansInResults} total plans");
            
            // Build detailed status message
            string scanDetails = $"Scanned {patientsOpened}/{total} patients";
            if (patientsSkipped > 0 || patientsWithErrors > 0)
            {
                var issues = new List<string>();
                if (patientsSkipped > 0) issues.Add($"{patientsSkipped} skipped");
                if (patientsWithErrors > 0) issues.Add($"{patientsWithErrors} errors");
                scanDetails += $" ({string.Join(", ", issues)})";
            }
            
            if (patientLimit.HasValue && results.Count >= patientLimit.Value)
            {
                StatusMessage = $"Advanced search complete. Found {results.Count} patients (limit reached) with {totalPlansInResults} matching plans. {scanDetails}.";
            }
            else
            {
                StatusMessage = $"Advanced search complete. Found {results.Count} patients with {totalPlansInResults} matching plans. {scanDetails}.";
            }
            return results;
        }

        private async Task PerformAdvancedSearchAsync()
        {
            // Show advanced search dialog with current search text, date range, and has plans filter
            var deepSearchWindow = new Views.AdvancedSearchWindow(
                searchText: SearchText ?? "",
                fromDate: FromDate,
                toDate: ToDate,
                showOnlyPatientsWithPlans: ShowOnlyPatientsWithPlans);
            deepSearchWindow.Owner = Application.Current.MainWindow;
            
            if (deepSearchWindow.ShowDialog() != true || !deepSearchWindow.SearchInitiated)
            {
                return; // User cancelled
            }

            // Sync edited patient filter values back to main UI
            SearchText = deepSearchWindow.PatientSearchText;
            FromDate = deepSearchWindow.FromDate;
            ToDate = deepSearchWindow.ToDate;
            ShowOnlyPatientsWithPlans = deepSearchWindow.ShowOnlyPatientsWithPlans;

            var criteria = deepSearchWindow.SearchCriteria;
            var patientLimit = deepSearchWindow.PatientLimit;

            // Use date range if specified, otherwise search all patients
            var fromDate = FromDate ?? DateTime.MinValue;
            var toDate = ToDate ?? DateTime.MaxValue;

            try
            {
                // Create cancellation token source for this search
                _searchCancellationTokenSource = new CancellationTokenSource();
                
                IsSearching = true;
                StatusMessage = FromDate.HasValue && ToDate.HasValue 
                    ? "Performing advanced search..." 
                    : "Performing advanced search (all dates)...";
                
                if (patientLimit.HasValue)
                {
                    StatusMessage += $" (limited to {patientLimit.Value} patients)";
                }

                // IMPORTANT: ESAPI must run on UI thread - cannot use Task.Run()
                // Perform deep search on UI thread with periodic yield for responsiveness
                var results = await PerformAdvancedSearchOnUIThreadAsync(
                    fromDate, 
                    toDate, 
                    criteria, 
                    SearchText ?? "", 
                    ShowOnlyPatientsWithPlans,
                    _searchCancellationTokenSource.Token,
                    patientLimit);

                // Clear existing patients and add results
                Patients.Clear();
                _allPatients.Clear();
                foreach (var patient in results)
                {
                    Patients.Add(patient);
                    _allPatients.Add(patient);
                    patient.PropertyChanged += OnPatientPropertyChanged;
                    
                    // Subscribe to plan changes
                    foreach (var plan in patient.Plans)
                    {
                        plan.PropertyChanged += OnPlanPropertyChanged;
                    }
                }

                // Apply filter after results are loaded
                ApplyPatientFilter();

                // Update command states so plan selection buttons become enabled
                CommandManager.InvalidateRequerySuggested();

                StatusMessage = $"Advanced search complete. Found {results.Count} patients with {results.Sum(p => p.PlanCount)} matching plans.";
                
                if (results.Count == 0)
                {
                    ThemedMessageBox.Show("No patients found matching the specified criteria.", 
                        "No Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Advanced search cancelled by user.";
                System.Diagnostics.Debug.WriteLine("Advanced search was cancelled.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Advanced search error: {ex.Message}";
                ThemedMessageBox.Show($"Error during advanced search:\n{ex.Message}", 
                    "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
                SearchProgress = 0; // Reset progress bar
                _searchCancellationTokenSource?.Dispose();
                _searchCancellationTokenSource = null;
            }
        }

        private void CancelSearch()
        {
            if (_searchCancellationTokenSource != null && !_searchCancellationTokenSource.IsCancellationRequested)
            {
                _searchCancellationTokenSource.Cancel();
                StatusMessage = "Cancelling search...";
                System.Diagnostics.Debug.WriteLine("Search cancellation requested by user.");
            }
        }

        private void SelectAllPatients()
        {
            foreach (var patient in Patients)
            {
                patient.IsSelected = true;
            }
            OnPropertyChanged(nameof(SelectionSummary));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ClearSelection()
        {
            foreach (var patient in Patients)
            {
                patient.IsSelected = false;
                foreach (var plan in patient.Plans)
                {
                    plan.IsSelected = false;
                }
            }
            OnPropertyChanged(nameof(SelectionSummary));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ClearPatientList()
        {
            // Unsubscribe from all patients
            foreach (var patient in Patients)
            {
                patient.PropertyChanged -= OnPatientPropertyChanged;
                foreach (var plan in patient.Plans)
                {
                    plan.PropertyChanged -= OnPlanPropertyChanged;
                }
            }
            
            Patients.Clear();
            _allPatients.Clear();
            OnPropertyChanged(nameof(SelectionSummary));
            StatusMessage = "Patient list cleared";
        }

        private Task LoadPatientPlansAsync(PatientInfo patient, bool allowDuringSearch = false, bool silent = false)
        {
            if (patient == null || (IsSearching && !allowDuringSearch)) return Task.CompletedTask;

            try
            {
                _esapiBusy = true;
                if (!silent)
                {
                    IsSearching = true;
                    StatusMessage = $"Loading plans for {patient.PatientId}...";
                }

                // Unsubscribe from existing plans first to avoid memory leaks
                // Capture previously selected plans to preserve selection after reload
                var previouslySelectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var existingPlan in patient.Plans)
                {
                    if (existingPlan.IsSelected)
                    {
                        var key = $"{existingPlan.CourseId}|{existingPlan.PlanId}";
                        previouslySelectedKeys.Add(key);
                    }
                    existingPlan.PropertyChanged -= OnPlanPropertyChanged;
                }

                System.Diagnostics.Debug.WriteLine("LoadPatientPlansAsync: calling GetPatientDetails on UI thread");
                var detailedPatient = _searchService.GetPatientDetails(patient.PatientId);
                
                if (detailedPatient != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Subscribe to plan property changes for selection updates
                        foreach (var plan in detailedPatient.Plans)
                        {
                            plan.PropertyChanged += OnPlanPropertyChanged;
                        }
                        
                        // Replace the plans with the newly loaded ones
                        patient.Plans = detailedPatient.Plans;

                        // Re-apply previous selections if any
                        if (previouslySelectedKeys.Count > 0 && patient.Plans != null)
                        {
                            foreach (var plan in patient.Plans)
                            {
                                var key = $"{plan.CourseId}|{plan.PlanId}";
                                if (previouslySelectedKeys.Contains(key))
                                {
                                    plan.IsSelected = true;
                                }
                            }
                        }
                        OnPropertyChanged(nameof(SelectionSummary));
                    });

                    if (!silent)
                    {
                        StatusMessage = $"Loaded {detailedPatient.Plans.Count} plans for {patient.PatientId}";
                    }
                    
                    // Update command states after loading plans
                    CommandManager.InvalidateRequerySuggested();
                }
                else
                {
                    StatusMessage = $"Could not load plans for {patient.PatientId}";
                    ThemedMessageBox.Show($"Could not load plans for patient '{patient.PatientId}'. The patient may not have any treatment plans or there was an access error.", "Select Plans Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading plans: {ex.Message}";
                ThemedMessageBox.Show($"Error loading plans for {patient.PatientId}:\n{ex.Message}\n\nThis may be due to access restrictions or the patient not having treatment plans.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!silent)
                {
                    IsSearching = false;
                }
                _esapiBusy = false;
            }
            
            return Task.CompletedTask;
        }

        private void ExportToJson()
        {
            try
            {
                UpdateCurrentCollection();
                _jsonExportService.ExportToFile(_currentCollection);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error exporting to JSON:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExportToJson()
        {
            return Patients.Any(p => p.IsSelected);
        }

        private void ImportFromJson()
        {
            try
            {
                var collection = _jsonExportService.ImportFromFile();
                if (collection != null)
                {
                    LoadImportedCollection(collection);
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error importing from JSON:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendToCombinedAppAsync()
        {
            try
            {
                UpdateCurrentCollection();
                await _combinedAppService.LaunchWithPatientListAsync(_currentCollection);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error sending to CombinedApp:\n{ex.Message}", "Send Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSendToCombinedApp()
        {
            return Patients.Any(p => p.IsSelected);
        }

        private Task LaunchCombinedAppAsync()
        {
            try
            {
                // Create handoff collection from current selection (or all if none selected)
                UpdateCurrentCollection();
                // Open the UI and drop handoff files next to index.html for auto-read
                _combinedAppService.OpenIndexHtml(_currentCollection);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error launching CombinedApp:\n{ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return Task.CompletedTask;
        }

        private void OpenCombinedAppIndex()
        {
            try
            {
                // Check if any patients or plans are selected
                bool hasSelectedPatients = Patients.Any(p => p.IsSelected);
                bool hasSelectedPlans = Patients.Any(p => p.IsSelected && p.Plans != null && p.Plans.Any(pl => pl.IsSelected));
                
                if (!hasSelectedPatients || !hasSelectedPlans)
                {
                    // Show confirmation dialog
                    var result = ThemedMessageBox.Show(
                        "No patients or plans are currently selected.\n\n" +
                        "Opening DICOMTools without selections will not pass any patient data.\n\n" +
                        "Do you want to continue?",
                        "No Selections Made",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return; // User chose not to continue
                    }
                }
                
                // Build handoff from current selection (or all if none selected)
                UpdateCurrentCollection();
                _combinedAppService.OpenIndexHtml(_currentCollection);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error opening CombinedApp UI:\n{ex.Message}", "Open UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Smart Search Methods

        private Task UpdateSearchSuggestionsAsync()
        {
            if (!_isInitialized || _esapiBusy || _searchService == null || string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchSuggestions.Clear();
                    IsSearchSuggestionsVisible = false;
                });
                return Task.CompletedTask;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateSearchSuggestionsAsync: querying suggestions on UI thread");
                var suggestions = _searchService.GetSmartSearchSuggestions(SearchText, maxResults: 8);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchSuggestions.Clear();
                    foreach (var suggestion in suggestions)
                    {
                        SearchSuggestions.Add(suggestion);
                    }
                    IsSearchSuggestionsVisible = SearchSuggestions.Count > 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSearchSuggestionsAsync: error {ex.Message}");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchSuggestions.Clear();
                    IsSearchSuggestionsVisible = false;
                });
            }
            
            return Task.CompletedTask;
        }


        private void UpdateFilteredPatientIds()
        {
            if (_isInitialized && _searchService != null && !string.IsNullOrEmpty(SearchText))
            {
                try
                {
                    var filteredIds = _searchService.GetFilteredPatientIds(SearchText, maxResults: 20);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        FilteredPatientIds.Clear();
                        foreach (var id in filteredIds)
                        {
                            FilteredPatientIds.Add(id);
                        }
                        
                        // Open dropdown automatically if we have results (AOS pattern)
                        IsPatientDropdownOpen = FilteredPatientIds.Count > 0;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error filtering patient IDs: {ex.Message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        FilteredPatientIds.Clear();
                        IsPatientDropdownOpen = false;
                    });
                }
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredPatientIds.Clear();
                    IsPatientDropdownOpen = false;
                });
            }
        }

        private void SelectPatientFromSuggestion(PatientInfo selectedPatient)
        {
            if (selectedPatient != null)
            {
                System.Diagnostics.Debug.WriteLine($"SelectPatientFromSuggestion: {selectedPatient.PatientId}");
                _ = AddSpecificPatientAsync(selectedPatient.PatientId);
                IsSearchSuggestionsVisible = false;
                SearchText = "";
            }
        }

        #endregion

        #region Event Handlers

        private void OnPatientPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PatientInfo.IsSelected))
            {
                // Update selection summary when patient selection changes
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }

        private void OnPlanPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlanInfo.IsSelected))
            {
                // Update selection summary when plan selection changes
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }

        #endregion

        #region Helper Methods

        private static bool IsPlanningApprovedStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            // Normalize: remove spaces, make case-insensitive comparisons
            var normalized = new string(status.Where(char.IsLetter).ToArray()).ToLowerInvariant();
            // Accept both PlanningApproved and TreatmentApproved
            return normalized == "planningapproved" || normalized == "treatmentapproved";
        }

        private void UpdateCurrentCollection()
        {
            // Build a new collection containing only selected patients and only their selected plans
            _currentCollection = new PatientPlanCollection
            {
                Patients = Patients
                    .Where(p => p.IsSelected)
                    .Select(p => new PatientInfo
                    {
                        PatientId = p.PatientId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        DateOfBirth = p.DateOfBirth,
                        CreationDate = p.CreationDate,
                        IsSelected = true,
                        Plans = (p.Plans ?? new List<PlanInfo>())
                            .Where(pl => pl.IsSelected)
                            .Select(pl => new PlanInfo
                            {
                                PatientId = pl.PatientId,
                                CourseId = pl.CourseId,
                                PlanId = pl.PlanId,
                                CreationDate = pl.CreationDate,
                                ApprovalStatus = pl.ApprovalStatus,
                                TotalDose = pl.TotalDose,
                                NumberOfFractions = pl.NumberOfFractions,
                                StructureSetId = pl.StructureSetId,
                                IsSelected = true
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        private void LoadImportedCollection(PatientPlanCollection collection)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Patients.Clear();
                _allPatients.Clear();
                foreach (var patient in collection.Patients)
                {
                    Patients.Add(patient);
                    _allPatients.Add(patient);
                }
                
                // Apply filter after importing
                ApplyPatientFilter();
                
                _currentCollection = collection;
                StatusMessage = $"Imported {collection.TotalPatients} patients and {collection.TotalPlans} plans";
                OnPropertyChanged(nameof(SelectionSummary));
            });
        }

        /// <summary>
        /// Applies the patient filter based on the ShowOnlyPatientsWithPlans checkbox
        /// </summary>
        private void ApplyPatientFilter()
        {
            // Store current patients in _allPatients if it's empty (first time)
            if (_allPatients.Count == 0 && Patients.Count > 0)
            {
                _allPatients = Patients.ToList();
            }

            // Apply filter
            if (ShowOnlyPatientsWithPlans)
            {
                // Show only patients with at least one plan
                var patientsToRemove = Patients.Where(p => p.PlanCount == 0).ToList();
                foreach (var patient in patientsToRemove)
                {
                    Patients.Remove(patient);
                }
            }
            else
            {
                // Show all patients - restore from _allPatients
                var patientsToAdd = _allPatients.Where(p => !Patients.Contains(p)).ToList();
                foreach (var patient in patientsToAdd)
                {
                    Patients.Add(patient);
                }
            }

            OnPropertyChanged(nameof(SelectionSummary));
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
}
