using ADOGenerator.Models;

namespace ADOGenerator.IServices
{
    public interface ITemplateService
    {
        bool AnalyzeProject(Project model);
        (bool,string,string) GenerateTemplateArtifacts(Project model);
    }
}
