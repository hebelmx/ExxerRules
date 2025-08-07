using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ExxeRules.CSharp.Design.InconsistentAccessibility
{
    public interface InconsistentAccessibilityInfoProvider
    {
        Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken);
    }
}
