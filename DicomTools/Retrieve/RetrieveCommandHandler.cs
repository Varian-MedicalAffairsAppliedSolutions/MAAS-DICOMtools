using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.CommandLineExtensions;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    public class RetrieveCommandHandler : ICommandOptionsHandler<RetrieveOptions>
    {
        public RetrieveCommandHandler(ILogger<RetrieveCommandHandler> logger,
            IConsole console,
            IConfirmationService confirmationService,
            StoreService storeService,
            RetrieveOptions globalRetrieveOptions)
        {
            m_logger = logger;
            m_console = console;
            m_confirmationService = confirmationService;
            m_storeService = storeService;
            m_globalRetrieveOptions = globalRetrieveOptions;
        }

        public async Task<int> HandleAsync(RetrieveOptions options, CancellationToken cancellationToken)
        {
            try
            {
                // Copy RetrieveOptions to "global"
                options.CopyTo(m_globalRetrieveOptions);

                if (Directory.Exists(options.Path))
                {
                    if (!options.Force && !m_confirmationService.Confirm($"Directory {options.Path} exist. Do you want to continue (y=yes)?"))
                        return 1;
                }

                var useGet = options.UseGet;
                var patientId = options.PatientId;
                var queryRetrieve = new DicomQueryRetrieve(m_logger, m_storeService, options);
                var studies = await queryRetrieve.FindStudies(patientId);

                // Determine if selective retrieval should be used
                var shouldUseSelectiveRetrieval = !string.IsNullOrEmpty(options.PlanId) ||
                                                   options.OnlyApprovedPlans;

                if (shouldUseSelectiveRetrieval)
                {
                    m_console.Out.WriteLine("⚡ Using selective retrieval - filtering plans before downloading data");
                    await PerformSelectiveRetrieval(studies, queryRetrieve, useGet);
                }
                else
                {
                    // Full retrieval - get everything
                    foreach (var study in studies)
                    {
                        m_console.Out.WriteLine($"Found study: {study}");

                        if (useGet)
                        {
                            var seriesList = await queryRetrieve.FindSeries(study);
                            foreach (var series in seriesList)
                            {
                                await queryRetrieve.Get(series);
                            }
                        }
                        else
                            await queryRetrieve.Move(study);
                    }
                }

                // Process collected series: filter by course/plan and move to final destination
                m_storeService.ProcessCollectedSeries();

                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex.Message);
                return await Task.FromResult(1);
            }
        }

        private async Task PerformSelectiveRetrieval(List<DataModel.Study> studies, DicomQueryRetrieve queryRetrieve, bool useGet)
        {
            foreach (var study in studies)
            {
                m_console.Out.WriteLine($"Found study: {study}");

                // Step 1: Query for RTPLAN series only
                m_console.Out.WriteLine("  Step 1/4: Querying for RT-Plan series...");
                var planSeries = await queryRetrieve.FindSeries(study, "RTPLAN");
                m_console.Out.WriteLine($"  Found {planSeries.Count} RT-Plan series");

                if (planSeries.Count == 0)
                {
                    m_console.Out.WriteLine("  No RT-Plan series found - skipping study");
                    continue;
                }

                // Step 2: Retrieve and filter RT-Plan instances
                m_console.Out.WriteLine("  Step 2/4: Retrieving and filtering RT-Plans...");
                var matchingPlanCount = await RetrieveAndFilterPlans(planSeries, queryRetrieve, useGet);
                
                if (matchingPlanCount == 0)
                {
                    m_console.Out.WriteLine("  ✗ No plans matched filters - skipping study");
                    continue;
                }

                m_console.Out.WriteLine($"  ✓ {matchingPlanCount} plan(s) matched filters");

                // Step 3: Retrieve all remaining data in the study
                m_console.Out.WriteLine("  Step 3/4: Retrieving all study data (structure sets, doses, images)...");
                m_console.Out.WriteLine("    Using full study retrieval to ensure complete reference chain...");
                
                if (useGet)
                    await queryRetrieve.Get(study);
                else
                    await queryRetrieve.Move(study);

                m_console.Out.WriteLine("  ✓ Study retrieval complete");
                
                // Step 4: Filter and move only the data related to matching plans
                m_console.Out.WriteLine("  Step 4/4: Filtering and exporting data for matched plans...");
                m_storeService.ProcessCollectedSeries();
                m_console.Out.WriteLine("  ✓ Export complete");
            }
        }

        private async Task<int> RetrieveAndFilterPlans(List<DataModel.Series> planSeries, DicomQueryRetrieve queryRetrieve, bool useGet)
        {
            m_console.Out.WriteLine($"    Retrieving {planSeries.Count} RT-Plan series for filtering...");

            foreach (var series in planSeries)
            {
                // Get all instance UIDs in this RT-Plan series
                var instanceUids = await queryRetrieve.FindInstancesInSeries(series);
                m_console.Out.WriteLine($"    Found {instanceUids.Count} plan instance(s) in series");
                
                // Retrieve each RT-Plan instance individually
                foreach (var instanceUid in instanceUids)
                {
                    if (useGet)
                        await queryRetrieve.GetInstance(series, instanceUid);
                    else
                        await queryRetrieve.MoveInstance(series, instanceUid);
                }
            }

            // Let StoreService parse and filter the plans
            m_console.Out.WriteLine($"    Applying filters (Plan ID: '{m_globalRetrieveOptions.PlanId}', Only Approved: {m_globalRetrieveOptions.OnlyApprovedPlans})...");
            var matchingPlans = m_storeService.FilterAndGetMatchingPlans();
            
            if (matchingPlans.Count > 0)
            {
                foreach (var plan in matchingPlans)
                {
                    m_console.Out.WriteLine($"      ✓ Matched plan: {plan.Label} (Approval: {plan.ApprovalStatus})");
                }
            }
            
            // Return the count of matching plans
            return matchingPlans.Count;
        }


        private readonly ILogger<RetrieveCommandHandler> m_logger;

        private readonly IConsole m_console;

        private readonly IConfirmationService m_confirmationService;

        private readonly StoreService m_storeService;

        private readonly RetrieveOptions m_globalRetrieveOptions;
    }
}
