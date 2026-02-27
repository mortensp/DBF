using System.Windows;
using Caliburn.Micro;

namespace DBF.ViewModels;

public class ProjectorViewModel : Screen
{
    // Reference til din singleton viewmodel for adgang til data uden at være den "owner" af vinduets livscyklus
    public ControlViewModel Host { get; }

    public ProjectorViewModel(ControlViewModel host)
    {
        Host = host;
    }

    // Hvis du vil forhindre at denne viewmodel spørger om lukning, kan du lade CanCloseAsync returnere true (eller implementere logik her).
    public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default) => await Task.FromResult(true);
}
