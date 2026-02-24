using System.Globalization;

namespace Shared.Services;

public interface ICultureService
{
    CultureInfo CurrentCulture { get; }
    IEnumerable<CultureInfo> SupportedCultures { get; }

    Task Init(bool reload);
    Task SetCurrentCulture(CultureInfo cultureInfo);
}
