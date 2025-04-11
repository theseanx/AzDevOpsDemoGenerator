using ADOGenerator.IServices;
using ADOGenerator.Models;
using ADOGenerator.Services;
using Microsoft.Extensions.Configuration;
using RestAPI.Extractor;
using RestAPI.ProjectsAndTeams;
using RestAPI;

public class TemplateService : ITemplateService
{
    private readonly IConfiguration _config;
    private readonly IExtractorService extractorService;

    public TemplateService(IConfiguration config)
    {
        _config = config;
        extractorService = new ExtractorService(_config);
    }

    public bool AnalyzeProject(Project model)
    {
        try
        {
            string defaultHost = _config["AppSettings:DefaultHost"];
            string projectPropertyVersion = _config["AppSettings:ProjectPropertyVersion"];

            ADOConfiguration config = new ADOConfiguration
            {
                AccountName = model.accountName,
                PersonalAccessToken = model.accessToken,
                UriString = defaultHost + model.accountName,
                VersionNumber = projectPropertyVersion,
                ProjectId = model.ProjectId,
                _adoAuthScheme = model.adoAuthScheme
            };

            Projects projects = new Projects(config);
            ProjectProperties.Properties load = projects.GetProjectProperties();
            model.ProcessTemplate = load.value[4].value;
            ExtractorService es = new ExtractorService(_config);
            ExtractorAnalysis analysis = new ExtractorAnalysis();
            ProjectConfigurations appConfig = extractorService.ProjectConfiguration(model);
            analysis.teamCount = extractorService.GetTeamsCount(appConfig);
            analysis.IterationCount = extractorService.GetIterationsCount(appConfig);
            analysis.WorkItemCounts = extractorService.GetWorkItemsCount(appConfig);
            analysis.BuildDefCount = extractorService.GetBuildDefinitionCount(appConfig);
            analysis.ReleaseDefCount = extractorService.GetReleaseDefinitionCount(appConfig);
            analysis.ErrorMessages = es.errorMessages;

            LogAnalysisResults(model, analysis);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during project analysis: " + ex.Message);
            return false;
        }
    }

    public (bool,string) GenerateTemplateArtifacts(Project model)
    {
        try
        {
            string[] createdTemplate = extractorService.GenerateTemplateArifacts(model);
            if (createdTemplate == null || createdTemplate.Length == 0)
            {
                Console.WriteLine("No artifacts were generated.");
                return (false, string.Empty); // No artifacts generated
            }
            string template = createdTemplate[1];
            return (true,template); // Artifact generation completed successfully
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during artifact generation: " + ex.Message);
            return (false,string.Empty); // Artifact generation failed
        }
    }

    private void LogAnalysisResults(Project model, ExtractorAnalysis analysis)
    {
        Console.WriteLine("Analysis of the project");
        Console.WriteLine("Project Name: " + model.ProjectName);
        Console.WriteLine("Process Template Type: " + model.ProcessTemplate);
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
    }
}
