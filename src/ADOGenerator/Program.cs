using ADOGenerator;
using ADOGenerator.IServices;
using ADOGenerator.Models;
using ADOGenerator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestAPI;
using System.Text.RegularExpressions;

var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();

Console.WriteLine("Welcome to Azure DevOps Demo Generator! This tool will help you generate a demo environment for Azure DevOps.");
(string accessToken, string organizationName, string authScheme)? authenticationDetails = null;
string authChoice = string.Empty;
do
{
    string id = Guid.NewGuid().ToString().Split('-')[0];
    Console.WriteLine("Do you want to create a new template or create a new project using the demo generator project template?");
    Console.WriteLine("1. Create a new project using the demo generator project template");
    Console.WriteLine("2. Generate a new Artifacts using existing project.");
    id.AddMessage("Enter the option number from the list of options above:");
    var userChoiceTemplate = Console.ReadLine();

    if (userChoiceTemplate == "1")
    {
        HandleNewProjectCreation(configuration,id);
    }
    else if (userChoiceTemplate == "2")
    {
        (bool isArtifactsGenerated,string template) = HandleArtifactGeneration(configuration,id);
        if (isArtifactsGenerated)
        {
            id.AddMessage("Do you want to update the template settings? (yes/no): press enter to confirm");
            var updateTemplateSettings = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(updateTemplateSettings) || updateTemplateSettings.Equals("yes", StringComparison.OrdinalIgnoreCase) || updateTemplateSettings.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                id.AddMessage("Updating template settings...");
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TemplateSetting.json");
                if (!File.Exists(templatePath))
                {
                    id.ErrorId().AddMessage("TemplateSettings.json file not found.");
                }
                else
                {
                    var templateSettings = File.ReadAllText(templatePath);
                    var json = JObject.Parse(templateSettings);
                    var groups = json["Groups"] as JArray ?? new JArray(json["Groups"]);
                    if (!groups.Any(g => g.ToString().Equals("Custom Templates", StringComparison.OrdinalIgnoreCase)))
                    {
                        var customTemplates = new JArray { "Custom Templates" };
                        foreach (var item in customTemplates)
                        {
                            groups.Add(item);
                        }
                        json["Groups"] = groups;
                    }
                    else
                    {
                        id.AddMessage("Custom Templates group already exists.");
                    }
                    var groupwiseTemplates = json["GroupwiseTemplates"] as JArray ?? new JArray(json["GroupwiseTemplates"]);
                    var newCustomTemplate = JObject.Parse(template);
                    var groupwiseCustomTemplates = newCustomTemplate["GroupwiseTemplates"];
                    if (groupwiseTemplates != null && groupwiseCustomTemplates != null)
                    {
                        foreach (var group in groupwiseCustomTemplates)
                        {
                            var groupName = group["Groups"]?.ToString();
                            var existingGroup = groupwiseTemplates.FirstOrDefault(g => g["Groups"]?.ToString().Equals(groupName, StringComparison.OrdinalIgnoreCase) == true);
                            if (existingGroup != null)
                            {
                                var existingTemplates = existingGroup["Template"] as JArray ?? new JArray(existingGroup["Template"]);
                                var newTemplates = group["Template"] as JArray ?? new JArray(group["Template"]);
                                var existingTemplateNames = existingTemplates.Select(t => t["Name"]?.ToString()).ToList();
                                var newTemplateNames = newTemplates.Select(t => t["Name"]?.ToString()).ToList();
                                var duplicateTemplates = newTemplateNames.Where(t => existingTemplateNames.Contains(t)).ToList();
                                if (duplicateTemplates.Any())
                                {
                                    id.AddMessage($"Duplicate templates found: {string.Join(", ", duplicateTemplates)}. Skipping these templates.");
                                    newTemplates = new JArray(newTemplates.Where(t => !duplicateTemplates.Contains(t["Name"]?.ToString())));
                                }
                                else
                                {
                                    id.AddMessage($"No duplicate templates found. Adding new templates.");
                                }
                                if (newTemplates != null)
                                {
                                    foreach (var templateCustom in newTemplates)
                                    {
                                        existingTemplates.Add(templateCustom);
                                    }
                                }
                            }
                            else
                            {
                                groupwiseTemplates.Add(group);
                            }
                        }
                    }
                    File.WriteAllText(templatePath, JsonConvert.SerializeObject(json,Formatting.Indented));
                }
            }
            else
            {
                Console.WriteLine("Copy the generated template json and update the template settings manually.");
                id.AddMessage("Template settings update skipped.");
            }
            
        }
        else
        {
            id.ErrorId().AddMessage("Artifacts generation failed.");
        }
    }
    else
    {
        Console.WriteLine("Invalid choice. Please select either 1 or 2.");
        continue;
    }
    if (authenticationDetails.HasValue)
    {
        authenticationDetails = (authenticationDetails.Value.accessToken, null, authenticationDetails.Value.authScheme);
    }
    Console.WriteLine("Do you want to create another project? (yes/no): press enter to confirm");
    var createAnotherProject = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(createAnotherProject) || createAnotherProject.Equals("yes", StringComparison.OrdinalIgnoreCase) || createAnotherProject.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        createAnotherProject = "yes";
    }
    else
    {
        createAnotherProject = "no";
    }
    Console.WriteLine();
    if (createAnotherProject.Equals("no", StringComparison.OrdinalIgnoreCase))
    {
        id.AddMessage("Exiting the application.");
        Environment.Exit(0);
    }
} while (true);
return 0;

void HandleNewProjectCreation(IConfiguration configuration, string id)
{
    Init init = new Init();
    id.AddMessage("Template Details");

    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TemplateSetting.json");
    if (!File.Exists(templatePath))
    {
        id.ErrorId().AddMessage("TemplateSettings.json file not found.");
        return;
    }

    var groupwiseTemplates = LoadTemplates(templatePath, id);
    if (groupwiseTemplates == null) return;

    var selectedTemplateName = SelectTemplate(groupwiseTemplates, id);
    if (string.IsNullOrEmpty(selectedTemplateName)) return;

    var templateFolder = string.Empty;
    var confirmedExtension = false;
    if (!TryGetTemplateDetails(groupwiseTemplates, selectedTemplateName, out templateFolder, out confirmedExtension, id))
    {
        return;
    }

    ValidateExtensions(templateFolder, id);

    var (accessToken, organizationName, authScheme) = AuthenticateUser(init, id);
    if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(organizationName)) return;

    var projectName = GetValidProjectName(init, id);
    if (string.IsNullOrWhiteSpace(projectName)) return;

    var project = new Project
    {
        id = id,
        accessToken = accessToken,
        accountName = organizationName,
        ProjectName = projectName,
        TemplateName = selectedTemplateName,
        selectedTemplateFolder = templateFolder,
        SelectedTemplate = templateFolder,
        isExtensionNeeded = confirmedExtension,
        isAgreeTerms = confirmedExtension,
        adoAuthScheme = authScheme
    };

    CreateProjectEnvironment(project);
}

(bool,string) HandleArtifactGeneration(IConfiguration configuration, string id)
{
    Init init = new Init();
    id.AddMessage("Template Details");

    var (accessToken, organizationName, authScheme) = AuthenticateUser(init, id);
    if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(organizationName)) return (false, string.Empty);

    IProjectService projService = new ProjectService(configuration);
    var projects = projService.GetProjects(organizationName, accessToken, authScheme);
    var projectDetails = projService.SelectProject(accessToken, projects).Result;

    var projectName = projectDetails[1];
    if (string.IsNullOrWhiteSpace(projectName)) return (false,string.Empty);

    var model = new Project
    {
        accountName = organizationName,
        ProjectName = projectName,
        ProjectId = projectDetails[0],
        accessToken = accessToken,
        adoAuthScheme = authScheme,
        id = id
    };

    ITemplateService templateService = new TemplateService(configuration);
    var analyzed = templateService.AnalyzeProject(model);
    if (analyzed)
    {
        model.id.AddMessage("Artifacts analyzed successfully.");
    }
    else
    {
        model.id.ErrorId().AddMessage("Artifacts analysis failed.");
    }
    Console.WriteLine("Do you want to create artifacts yes/no:");
    string response = Console.ReadLine();
    if (response == "yes")
    {
        (bool isArtifactsGenerated, string template) = templateService.GenerateTemplateArtifacts(model);
        if(isArtifactsGenerated)
        {
            model.id.AddMessage("Artifacts generated successfully.");
            return (true, template);
        }
        else
        {
            model.id.ErrorId().AddMessage("Artifacts generation failed.");
        }
    }
    else
    {
        model.id.AddMessage("Artifacts generation skipped.");
    }
    return (false, string.Empty);
}

JToken LoadTemplates(string templatePath, string id)
{
    var templateSettings = File.ReadAllText(templatePath);
    var json = JObject.Parse(templateSettings);
    var groupwiseTemplates = json["GroupwiseTemplates"];

    if (groupwiseTemplates == null)
    {
        id.ErrorId().AddMessage("No templates found.");
        return null;
    }

    return groupwiseTemplates;
}

string SelectTemplate(JToken groupwiseTemplates, string id)
{
    int templateIndex = 1;
    var templateDictionary = new Dictionary<int, string>();

    foreach (var group in groupwiseTemplates)
    {
        var groupName = group["Groups"]?.ToString();
        Console.WriteLine(groupName);

        var templates = group["Template"];
        if (templates != null)
        {
            foreach (var template in templates)
            {
                var templateName = template["Name"]?.ToString();
                Console.WriteLine($"  {templateIndex}. {templateName}");
                templateDictionary.Add(templateIndex, templateName);
                templateIndex++;
            }
        }
    }

    id.AddMessage("Enter the template number from the list of templates above:");
    if (!int.TryParse(Console.ReadLine(), out var selectedTemplateNumber) || !templateDictionary.TryGetValue(selectedTemplateNumber, out var selectedTemplateName))
    {
        id.AddMessage("Invalid template number entered.");
        return null;
    }

    return selectedTemplateName.Trim();
}
(string accessToken, string organizationName, string authScheme) AuthenticateUser(Init init, string id)
{
    string organizationName = string.Empty;
    if (authenticationDetails.HasValue)
    {
        if (string.IsNullOrWhiteSpace(authenticationDetails.Value.organizationName))
        {
            if (authChoice == "1")
            {
                AuthService authService = new AuthService();
                var memberId = authService.GetProfileInfoAsync(authenticationDetails.Value.accessToken).Result;
                var organizations = authService.GetOrganizationsAsync(authenticationDetails.Value.accessToken, memberId).Result;
                organizationName = authService.SelectOrganization(authenticationDetails.Value.accessToken, organizations).Result;
            }
            else if(authChoice == "2")
            {
                id.AddMessage("Enter your Azure DevOps organization name:");
                organizationName = Console.ReadLine();
            }
            authenticationDetails = (authenticationDetails.Value.accessToken, organizationName, authenticationDetails.Value.authScheme);
        }
        return authenticationDetails.Value;
    }
    id.AddMessage("Choose authentication method: 1. Device Login using AD auth 2. Personal Access Token (PAT)");
    authChoice = Console.ReadLine();

    string accessToken = string.Empty;
    string authScheme = string.Empty;

    if (authChoice == "1")
    {
        var app = PublicClientApplicationBuilder.Create(AuthService.clientId)
            .WithAuthority(AuthService.authority)
            .WithDefaultRedirectUri()
            .Build();
        AuthService authService = new AuthService();

        var accounts = app.GetAccountsAsync().Result;
        if (accounts.Any())
        {
            try
            {
                var result = app.AcquireTokenSilent(AuthService.scopes, accounts.FirstOrDefault())
                                .WithForceRefresh(true)
                                .ExecuteAsync().Result;
                accessToken = result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                var result = authService.AcquireTokenAsync(app).Result;
                accessToken = result.AccessToken;
            }
        }
        else
        {
            var result = authService.AcquireTokenAsync(app).Result;
            accessToken = result.AccessToken;
        }

        var memberId = authService.GetProfileInfoAsync(accessToken).Result;
        var organizations = authService.GetOrganizationsAsync(accessToken, memberId).Result;
        organizationName = authService.SelectOrganization(accessToken, organizations).Result;
        authScheme = "Bearer";
    }
    else if (authChoice == "2")
    {
        id.AddMessage("Enter your Azure DevOps organization name:");
        organizationName = Console.ReadLine();

        id.AddMessage("Enter your Azure DevOps personal access token:");
        accessToken = init.ReadSecret();

        authScheme = "Basic";
    }

    authenticationDetails = (accessToken, organizationName, authScheme);
    return authenticationDetails.Value;
}

string GetValidProjectName(Init init, string id)
{
    string projectName = "";
    do
    {
        id.AddMessage("Enter the new project name:");
        projectName = Console.ReadLine();
        if (!init.CheckProjectName(projectName))
        {
            id.ErrorId().AddMessage("Validation error: Project name is not valid.");
            id.AddMessage("Do you want to try with a valid project name or exit? (type 'retry' to try again or 'exit' to quit):");
            var userChoice = Console.ReadLine();
            if (userChoice?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
            {
                id.AddMessage("Exiting the application.");
                Environment.Exit(1);
            }
            projectName = "";
        }
    } while (string.IsNullOrWhiteSpace(projectName));

    return projectName;
}

bool TryGetTemplateDetails(JToken groupwiseTemplates, string selectedTemplateName, out string templateFolder, out bool confirmedExtension, string id)
{
    templateFolder = string.Empty;
    confirmedExtension = false;

    foreach (var group in groupwiseTemplates)
    {
        var templates = group["Template"];
        if (templates == null) continue;

        foreach (var template in templates)
        {
            if (template["Name"]?.ToString().Equals(selectedTemplateName, StringComparison.OrdinalIgnoreCase) != true)
                continue;
            else
            {
                templateFolder = template["TemplateFolder"]?.ToString();
                var templateFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolder);

                if (!Directory.Exists(templateFolderPath))
                {
                    id.ErrorId().AddMessage($"Template '{selectedTemplateName}' is not found.");
                    return false;
                }
                id.AddMessage($"Template '{selectedTemplateName}' is present.");
                return true;
            }

        }
    }
    return false;
}

bool ValidateExtensions(string templateFolderPath, string id)
{
    Init init = new Init();

    var extensionsFilePath = Path.Combine(Directory.GetCurrentDirectory(),"Templates",templateFolderPath, "Extensions.json");
    if (!File.Exists(extensionsFilePath))
    {
        return false;
    }

    var extensionsFile = File.ReadAllText(extensionsFilePath);
    var extensionjson = JObject.Parse(extensionsFile);
    var extensions = extensionjson["Extensions"];
    if (extensions == null)
    {
        return false;
    }

    foreach (var extension in extensions)
    {
        var extensionName = extension["extensionName"]?.ToString();
        var link = extension["link"]?.ToString();
        var licenseLink = extension["License"]?.ToString();

        var href = init.ExtractHref(link);
        var licenseHref = init.ExtractHref(licenseLink);

        Console.WriteLine($"Extension Name: {extensionName}");
        Console.WriteLine($"Link: {href}");
        Console.WriteLine($"License: {licenseHref}");
        Console.WriteLine();
    }

    if (extensions.HasValues)
    {
        id.AddMessage("Do you want to proceed with this extension? (yes/No): press enter to confirm");
        var userConfirmation = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userConfirmation) || (userConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || userConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            id.AddMessage("Agreed for license? (yes/no): press enter to confirm");
            Console.ResetColor();
            var licenseConfirmation = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(licenseConfirmation) || (licenseConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || licenseConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
            {
                id.AddMessage("Confirmed Extension installation");
                return true;
            }
        }
        else
        {
            id.AddMessage("Extension installation is not confirmed.");
        }
    }

    return false;
}

void CreateProjectEnvironment(Project model)
{
    Console.WriteLine($"Creating project '{model.ProjectName}' in organization '{model.accountName}' using template from '{model.TemplateName}'...");
    var projectService = new ProjectService(configuration);
    var result = projectService.CreateProjectEnvironment(model);
    if (result)
    {
        Console.WriteLine("Project created successfully.");
    }
    else
    {
        Console.WriteLine("Project creation failed.");
    }
}