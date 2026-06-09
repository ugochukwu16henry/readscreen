using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IAppSettings
{
    AppSettings Current { get; }
    void Save();
    void Reload();
    event Action? SettingsChanged;
}
