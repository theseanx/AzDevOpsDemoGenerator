using ADOGenerator.IServices;
using ADOGenerator.Models;
using Microsoft.Extensions.Configuration;
using RestAPI;
using RestAPI.Extractor;
using RestAPI.ProjectsAndTeams;
using System.Configuration;

namespace ADOGenerator.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly IConfiguration _config;
        IExtractorService extractorService;
        public TemplateService(IConfiguration config) {
            _config = config;
            extractorService = new ExtractorService(_config);
        }

        public bool AnalyzeProject(Project model)
        {
            string defaultHost = _config["AppSettings:DefaultHost"];
            string ProjectPropertyVersion = _config["AppSettings:ProjectPropertyVersion"];
            //ProjectPropertyVersion = _config["AppSettings:ProjectPropertyVersion"];
            ADOConfiguration config = new ADOConfiguration() { AccountName = model.accountName, PersonalAccessToken = model.accessToken, UriString = defaultHost + model.accountName, VersionNumber = ProjectPropertyVersion, ProjectId = model.ProjectId, _adoAuthScheme = model.adoAuthScheme};

            ProjectProperties.Properties load = new ProjectProperties.Properties();
            Projects projects = new Projects(config);
            load = projects.GetProjectProperties();
            model.ProcessTemplate = load.value[4].value;
            //model.ProjectId = load.value[0].value;
            ExtractorService es = new ExtractorService(_config);
            ExtractorAnalysis analysis = new ExtractorAnalysis();
            ProjectConfigurations appConfig = extractorService.ProjectConfiguration(model);
            analysis.teamCount = extractorService.GetTeamsCount(appConfig);
            analysis.IterationCount = extractorService.GetIterationsCount(appConfig);
            analysis.WorkItemCounts = extractorService.GetWorkItemsCount(appConfig);
            analysis.BuildDefCount = extractorService.GetBuildDefinitionCount(appConfig);
            analysis.ReleaseDefCount = extractorService.GetReleaseDefinitionCount(appConfig);
            analysis.ErrorMessages = es.errorMessages;

            Console.WriteLine("Analysis of the project");
            Console.WriteLine("Project Name: " + model.ProjectName);
            Console.WriteLine("Processtemplate Type: " + model.ProcessTemplate);
            Console.WriteLine("Teams Count: " + analysis.teamCount);
            Console.WriteLine("Iterations Count: " + analysis.IterationCount);
            Console.WriteLine("Work Items Count: ");
            foreach (var item in analysis.WorkItemCounts)
            {
                Console.WriteLine(item.Key + " : " + item.Value);
            }
            Console.WriteLine("Build Definitions Count: " + analysis.BuildDefCount);
            Console.WriteLine("Release Definitions Count: " + analysis.ReleaseDefCount);
            Console.WriteLine("Errors: ");
            foreach (var item in analysis.ErrorMessages)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Do you want to create artifacts yes/no:");
            string response = Console.ReadLine();
            if (response == "yes")
            {
                return StartEnvironmentSetupProcess(model);
            }
            else
            {
                return false;
            }
        }

        public bool StartEnvironmentSetupProcess(Project model)
        {
            extractorService.GenerateTemplateArifacts(model);
            return true;
        }        
    }
}
