using ADOGenerator;
using ADOGenerator.Models;
using ADOGenerator.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();

Console.WriteLine("Welcome to Azure DevOps Demo Generator! This tool will help you generate a demo environment for Azure DevOps.");

string id = Guid.NewGuid().ToString().Split('-')[0];
do
{

    id.AddMessage("Template Datails");
    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TemplateSetting.json");

    if (!File.Exists(templatePath))
    {
        id.ErrorId().AddMessage("TemplateSettings.json file not found.");
        break;
    }

    var templateSettings = File.ReadAllText(templatePath);
    var json = JObject.Parse(templateSettings);
    var groupwiseTemplates = json["GroupwiseTemplates"];

    if (groupwiseTemplates == null)
    {
        id.ErrorId().AddMessage("No templates found.");
        break;
    }

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
                Console.WriteLine($"  └─ {templateName}");
            }
        }
    }

    id.AddMessage("Enter the template name from the list of templates above:");
    var selectedTemplateName = Console.ReadLine();
    selectedTemplateName = selectedTemplateName.Trim();
    if (!TryGetTemplateDetails(groupwiseTemplates, selectedTemplateName, out var templateFolder, out var confirmedExtension))
    {
        id.AddMessage($"Template '{selectedTemplateName}' not found in the list.");
        id.AddMessage("Would you like to try again or exit? (type 'retry' to try again or 'exit' to quit):");
        var userChoice = Console.ReadLine();
        if (userChoice?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
        {
            id.AddMessage("Exiting the application.");
            return;
        }
        continue;
    }

    id.AddMessage("Enter your Azure DevOps organization name:");
    var organizationName = Console.ReadLine();

    id.AddMessage("Enter your Azure DevOps personal access token:");
    var patToken = ReadSecret();

    string projectName = "";
    do
    {
        id.AddMessage("Enter the new project name:");
        projectName = Console.ReadLine();
        if (!CheckProjectName(projectName))
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
            continue;
        }
    } while (string.IsNullOrWhiteSpace(projectName));

    if (string.IsNullOrWhiteSpace(organizationName) || string.IsNullOrWhiteSpace(patToken) || string.IsNullOrWhiteSpace(projectName))
    {
        id.ErrorId().AddMessage("Validation error: All inputs must be provided. Exiting..");
        Environment.Exit(1);
    }

    var project = new Project
    {
        id = id,
        accessToken = patToken,
        accountName = organizationName,
        ProjectName = projectName,
        TemplateName = selectedTemplateName,
        selectedTemplateFolder = templateFolder,
        SelectedTemplate = templateFolder,
        isExtensionNeeded = confirmedExtension,
        isAgreeTerms = confirmedExtension
    };

    CreateProjectEnvironment(project);
} while (true);

bool TryGetTemplateDetails(JToken groupwiseTemplates, string selectedTemplateName, out string templateFolder, out bool confirmedExtension)
{
    templateFolder = string.Empty;
    confirmedExtension = false;

    foreach (var group in groupwiseTemplates)
    {
        var templates = group["Template"];
        if (templates == null) continue;

        foreach (var template in templates)
        {
            if (template["Name"]?.ToString().Equals(selectedTemplateName, StringComparison.OrdinalIgnoreCase) != true) continue;

            templateFolder = template["TemplateFolder"]?.ToString();
            var templateFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolder);
            var extensionsFilePath = Path.Combine(templateFolderPath, "Extensions.json");

            if (!Directory.Exists(templateFolderPath))
            {
                id.ErrorId().AddMessage($"Template '{selectedTemplateName}' is not found.");
                return false;
            }

            id.AddMessage($"Template '{selectedTemplateName}' is present.");
            id.AddMessage("Are you sure want to create a new project with the selected template? (yes/no): press enter to confirm");
            var confirm = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(confirm) && !confirm.Equals("yes", StringComparison.OrdinalIgnoreCase) && !confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (File.Exists(extensionsFilePath))
            {
                var extensionsFile = File.ReadAllText(extensionsFilePath);
                var extensionjson = JObject.Parse(extensionsFile);
                var extensions = extensionjson["Extensions"];
                if (extensions != null)
                {
                    foreach (var extension in extensions)
                    {
                        var extensionName = extension["extensionName"]?.ToString();
                        var link = extension["link"]?.ToString();
                        var licenseLink = extension["License"]?.ToString();

                        var href = ExtractHref(link);
                        var licenseHref = ExtractHref(licenseLink);

                        Console.WriteLine($"Extension Name: {extensionName}");
                        Console.WriteLine($"Link: {href}");
                        Console.WriteLine($"License: {licenseHref}");
                        Console.WriteLine();
                    }

                    if (extensions.HasValues)
                    {
                        id.AddMessage("Do you want to proceed with this extension? (yes/no):");
                        var userConfirmation = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(userConfirmation) && (userConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || userConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            id.AddMessage("Agreed for license? (yes/no):");
                            Console.ResetColor();
                            var licenseConfirmation = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(licenseConfirmation) && (licenseConfirmation.Equals("yes", StringComparison.OrdinalIgnoreCase) || licenseConfirmation.Equals("y", StringComparison.OrdinalIgnoreCase)))
                            {
                                confirmedExtension = true;
                                id.AddMessage("Confirmed Extension installation");
                            }
                        }
                        else
                        {
                            id.AddMessage("Extension installation is not confirmed.");
                        }
                    }
                }
            }

            return true;
        }
    }

    return false;
}

string ExtractHref(string link)
{
    var startIndex = link.IndexOf("href='") + 6;
    var endIndex = link.IndexOf("'", startIndex);
    return link.Substring(startIndex, endIndex - startIndex);
}

string ReadSecret()
{
    var secret = string.Empty;
    ConsoleKey key;
    do
    {
        var keyInfo = Console.ReadKey(intercept: true);
        key = keyInfo.Key;

        if (key == ConsoleKey.Backspace && secret.Length > 0)
        {
            Console.Write("\b \b");
            secret = secret[0..^1];
        }
        else if (!char.IsControl(keyInfo.KeyChar))
        {
            Console.Write("*");
            secret += keyInfo.KeyChar;
        }
    } while (key != ConsoleKey.Enter);
    Console.WriteLine();
    return secret;
}

void CreateProjectEnvironment(Project model)
{
    Console.WriteLine($"Creating project '{model.ProjectName}' in organization '{model.accountName}' using template from '{model.TemplateName}'...");
    var projectService = new ProjectService(configuration);
    var result = projectService.CreateProjectEnvironment(model);
    Console.WriteLine("Project created successfully.");
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
        model.id.AddMessage("Exiting the application.");
        Environment.Exit(0);
    }
}

void PrintErrorMessage(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {message}");
    Console.ResetColor();
}

bool CheckProjectName(string name)
{
    try
    {
        List<string> reservedNames = new List<string>
            {
                "AUX", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10",
                "CON", "DefaultCollection", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
                "NUL", "PRN", "SERVER", "SignalR", "Web", "WEB",
                "App_Browsers", "App_code", "App_Data", "App_GlobalResources", "App_LocalResources", "App_Themes", "App_WebResources", "bin", "web.config"
            };

        if (name.Length > 64)
        {
            return false;
        }
        if (reservedNames.Contains(name))
        {
            return false;
        }
        if (Regex.IsMatch(name, @"^_|^\.") || Regex.IsMatch(name, @"\.$"))
        {
            return false;
        }
        if (Regex.IsMatch(name, @"[\x00-\x1F\x7F-\x9F\uD800-\uDFFF\\/:*?""<>;#$*{},+=\[\]|.@%&~`]"))
        {
            return false;
        }
        return true;
    }
    catch (Exception ex)
    {
        PrintErrorMessage(ex.Message);
        return false;
    }
}