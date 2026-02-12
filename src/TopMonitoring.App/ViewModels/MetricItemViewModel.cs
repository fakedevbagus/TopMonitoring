using CommunityToolkit.Mvvm.ComponentModel;

namespace TopMonitoring.App.ViewModels
{
    public sealed partial class MetricItemViewModel : ObservableObject
    {
        public string Id { get; }

        [ObservableProperty]
        private string text = string.Empty;

        [ObservableProperty]
        private bool isAlert;

        public MetricItemViewModel(string id)
        {
            Id = id;
        }
    }
}
