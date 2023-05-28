// ///////////////////////////////////////////////////////////////////
// This file is a part of EasyFarm for Final Fantasy XI
// Copyright (C) 2013 Mykezero
//  
// EasyFarm is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// EasyFarm is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// If not, see <http://www.gnu.org/licenses/>.
// ///////////////////////////////////////////////////////////////////
using System.Windows.Input;
using EasyFarm.Classes;
using EasyFarm.Infrastructure;
using EasyFarm.UserSettings;
using GalaSoft.MvvmLight.Command;

namespace EasyFarm.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
            ViewName = "Settings";
            AppServices.RegisterEvent<Events.ConfigLoadedEvent>(this, x => RefreshViewModel());
        }

        private void RefreshViewModel()
        {
            RaisePropertyChanged(nameof(ShouldEngage));
            RaisePropertyChanged(nameof(ShouldApproach));
            RaisePropertyChanged(nameof(AllowAutoTarget));
            RaisePropertyChanged(nameof(DetectionDistance));
            RaisePropertyChanged(nameof(HeightThreshold));
            RaisePropertyChanged(nameof(MeleeDistance));
            RaisePropertyChanged(nameof(WanderDistance));
            RaisePropertyChanged(nameof(RouteLimitTargets));
            RaisePropertyChanged(nameof(RouteTetherPlayer));
            RaisePropertyChanged(nameof(RouteTolerance));
            RaisePropertyChanged(nameof(RouteNavMeshTolerance));
            RaisePropertyChanged(nameof(GlobalCooldown));
            RaisePropertyChanged(nameof(AvoidObjects));
            RaisePropertyChanged(nameof(EnableTabTargeting));
            RaisePropertyChanged(nameof(HomePointOnDeath));
            RaisePropertyChanged(nameof(TrustPartySize));
            RaisePropertyChanged(nameof(TargetUpperHealth));
            RaisePropertyChanged(nameof(TargetLowerHealth));
        }

        public ICommand RestoreDefaultsCommand { get; set; }

        public bool ShouldEngage
        {
            get { return Config.Instance.IsEngageEnabled; }
            set { Set(ref Config.Instance.IsEngageEnabled, value); }
        }

        public bool ShouldApproach
        {
            get { return Config.Instance.IsApproachEnabled; }
            set { Set(ref Config.Instance.IsApproachEnabled, value); }
        }

        public bool AllowAutoTarget
        {
            get { return Config.Instance.AllowAutoTarget; }
            set { Set(ref Config.Instance.AllowAutoTarget, value); }
        }

        public double DetectionDistance
        {
            get { return Config.Instance.DetectionDistance; }
            set
            {
                Set(ref Config.Instance.DetectionDistance, (int) value);
                AppServices.InformUser("Detection Distance Set: {0}.", (int) value);
            }
        }

        public double HeightThreshold
        {
            get { return Config.Instance.HeightThreshold; }
            set
            {
                Set(ref Config.Instance.HeightThreshold, value);
                AppServices.InformUser("Height Threshold Set: {0:F1}.", value);
            }
        }

        public double MeleeDistance
        {
            get { return Config.Instance.MeleeDistance; }
            set
            {
                Set(ref Config.Instance.MeleeDistance, value);
                AppServices.InformUser("Melee Distance Set: {0:F1}.", value);
            }
        }

        public double WanderDistance
        {
            get { return Config.Instance.WanderDistance; }
            set
            {
                Set(ref Config.Instance.WanderDistance, (int) value);
                AppServices.InformUser("Wander Distance Set: {0}.", (int) value);
            }
        }

        public bool RouteLimitTargets
        {
            get { return Config.Instance.RouteLimitTargets; }
            set
            {
                Set(ref Config.Instance.RouteLimitTargets, value);
                if (value)
                {
                    AppServices.InformUser("Targetting now restricted.", value);
                }
                else
                {
                    AppServices.InformUser("Targetting no longer restricted.", value);
                }
            }
        }
        public bool RouteTetherPlayer
        {
            get { return Config.Instance.RouteTetherPlayer; }
            set
            {
                Set(ref Config.Instance.RouteTetherPlayer, value);
                if (value)
                {
                    AppServices.InformUser("Player now tethered.", value);
                }
                else
                {
                    AppServices.InformUser("Player no longer tethered.", value);
                }
            }
        }

        public double RouteTolerance
        {
            get { return Config.Instance.RouteTolerance; }
            set
            {
                Set(ref Config.Instance.RouteTolerance, value);
                AppServices.InformUser("Route Tolerance Set: {0}.", value);
            }
        }
        public double RouteNavMeshTolerance
        {
            get { return Config.Instance.RouteNavMeshTolerance; }
            set
            {
                Set(ref Config.Instance.RouteNavMeshTolerance, value);
                AppServices.InformUser("Route Mesh Tolerance Set: {0}.", value);
            }
        }

        public int GlobalCooldown
        {
            get { return Config.Instance.GlobalCooldown; }
            set
            {
                Set(ref Config.Instance.GlobalCooldown, value);
                AppServices.InformUser("Global Cooldown Set: {0}.", value);
            }
        }

        public bool AvoidObjects
        {
            get { return Config.Instance.IsObjectAvoidanceEnabled; }
            set
            {
                Set(ref Config.Instance.IsObjectAvoidanceEnabled, value);
            }
        }

        public bool EnableTabTargeting
        {
            get { return Config.Instance.EnableTabTargeting; }
            set
            {
                Set(ref Config.Instance.EnableTabTargeting, value);
            }
        }

        public bool HomePointOnDeath
        {
            get { return Config.Instance.HomePointOnDeath; }
            set
            {
                Set(ref Config.Instance.HomePointOnDeath, value);
            }
        }

        public int TrustPartySize
        {
            get { return Config.Instance.TrustPartySize; }
            set
            {
                Set(ref Config.Instance.TrustPartySize, value);
            }
        }

        public int TargetUpperHealth
        {
            get { return Config.Instance.TargetUpperHealth; }
            set
            {
                Set(ref Config.Instance.TargetUpperHealth, value);
                AppServices.InformUser("Global Upper Target HP target Set: {0}.", value);
            }
        }

        public int TargetLowerHealth
        {
            get { return Config.Instance.TargetLowerHealth; }
            set
            {
                Set(ref Config.Instance.TargetLowerHealth, value);
                AppServices.InformUser("Global Lower Target HP target Set: {0}.", value);
            }
        }

        private void RestoreDefaults()
        {
            DetectionDistance = Constants.DetectionDistance;
            HeightThreshold = Constants.HeightThreshold;
            MeleeDistance = Constants.MeleeDistance;
            WanderDistance = Constants.DetectionDistance;
            GlobalCooldown = Constants.GlobalSpellCooldown;
            EnableTabTargeting = false;
            AvoidObjects = false;
            ShouldApproach = true;
            ShouldEngage = true;
            HomePointOnDeath = false;
            AllowAutoTarget = true;
            RouteTetherPlayer = false;
            RouteLimitTargets = false;
            TrustPartySize = Constants.TrustPartySize;
            AppServices.InformUser("Defaults have been restored.");
        }        
    }
}