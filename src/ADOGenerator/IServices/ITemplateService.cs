using ADOGenerator.Models;

namespace ADOGenerator.IServices
{
    public interface ITemplateService
    {
        bool AnalyzeProject(Project model);
        (bool,string) GenerateTemplateArtifacts(Project model);
    }
}
