using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TopMonitoring.App.ViewModels
{
    public sealed class MainViewModel
    {
        private readonly Dictionary<string, MetricItemViewModel> _map = new();

        public ObservableCollection<MetricItemViewModel> Metrics { get; } = new();

        public MetricItemViewModel GetOrCreate(string id)
        {
            if (_map.TryGetValue(id, out var vm)) return vm;
            vm = new MetricItemViewModel(id);
            _map[id] = vm;
            return vm;
        }

        public void ApplyOrder(IEnumerable<string> order, ISet<string> enabled)
        {
            Metrics.Clear();
            foreach (var id in order)
            {
                if (!enabled.Contains(id)) continue;
                Metrics.Add(GetOrCreate(id));
            }
        }

        public void SetText(string id, string text)
        {
            GetOrCreate(id).Text = text;
        }

        public void SetAlert(string id, bool isAlert)
        {
            GetOrCreate(id).IsAlert = isAlert;
        }
    }
}
