using Castle.Core.Resource;
using EasyFarm.Classes;
using EasyFarm.Infrastructure;
using EasyFarm.UserSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EasyFarm.ViewModels
{
    public class PullBehaviourViewModel : ViewModelBase, IViewModel
    {
        public PullBehaviourViewModel()
        {
            ViewName = "PullBehavior";
            AppServices.RegisterEvent<Events.ConfigLoadedEvent>(this, x => RefreshViewModel());
        }
        private void RefreshViewModel()
        {
            RaisePropertyChanged(nameof(PullFallback));
            RaisePropertyChanged(nameof(PullLockTime));
        }

        public PullFallbackType PullFallback
        {
            get
            {
                return Config.Instance.PullFallback;
            }
            set
            {
                Set(ref Config.Instance.PullFallback, value);
                AppServices.InformUser(string.Format("Pulling fallback set to: {0}.", value));
            }
        }
        public int PullLockTime
        {
            get {
                return Config.Instance.PullLockTime;
            }
            set
            {
                Set(ref Config.Instance.PullLockTime, value);
                AppServices.InformUser(string.Format("Pull lockout time set to: {0}.", value));
            }
        }
    }
}
