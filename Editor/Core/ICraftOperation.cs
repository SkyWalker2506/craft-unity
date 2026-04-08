using SkyWalker.Craft.Editor.Models;

namespace SkyWalker.Craft.Editor.Core
{
    public interface ICraftOperation
    {
        string Type { get; }
        ValidationResult Validate(CraftOperation op);
        OperationResult Execute(CraftOperation op);
    }
}
