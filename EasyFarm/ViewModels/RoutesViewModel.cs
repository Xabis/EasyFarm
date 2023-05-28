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
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Input;
using EasyFarm.Classes;
using EasyFarm.Infrastructure;
using EasyFarm.Persistence;
using EasyFarm.States;
using EasyFarm.UserSettings;
using GalaSoft.MvvmLight.Command;
using MemoryAPI;
using MemoryAPI.Navigation;

namespace EasyFarm.ViewModels
{
    public class RoutesViewModel : ViewModelBase
    {        
        private readonly SettingsManager _settings;

        private string _recordHeader;

        public RoutesViewModel()
        {            
            _settings = new SettingsManager("ewl", "EasyFarm Waypoint List");            

            ClearCommand = new RelayCommand(ClearRoute);
            RecordCommand = new RelayCommand(Record);
            SaveCommand = new RelayCommand(Save);
            LoadCommand = new RelayCommand(Load);
            AddCommand = new RelayCommand(AddWaypoinnt);
            ResetNavigatorCommand = new RelayCommand(ResetNavigator);

            RecordHeader = "Record";
            ViewName = "Routes";
            AppServices.RegisterEvent<Events.ConfigLoadedEvent>(this, x => RefreshViewModel());
        }

        private void RefreshViewModel()
        {
            RaisePropertyChanged(nameof(StraightRoute));
            RaisePropertyChanged(nameof(Route));
        }

        private void PathRecorder_OnPositionAdded(Position position)
        {
            Application.Current.Dispatcher.Invoke(() => Route.Add(position));
        }

        public string RecordHeader
        {
            get { return _recordHeader; }
            set { Set(ref _recordHeader, value); }
        }

        public bool DebugMode
        {
            get { return Config.Instance.DebugRoutes; }
            set { Set(ref Config.Instance.DebugRoutes, value); }
        }

        public bool StraightRoute
        {
            get
            {
                return Config.Instance.StraightRoute;
            }
            set
            {
                Set(ref Config.Instance.StraightRoute, value);
            }
        }

        /// <summary>
        ///     Exposes the list of waypoints to the user.
        /// </summary>
        public ObservableCollection<Position> Route
        {
            get { return Config.Instance.Route.Waypoints; }
            set { Set(ref Config.Instance.Route.Waypoints, value); }
        }

        /// <summary>
        ///     Binding for the record command for the GUI.
        /// </summary>
        public ICommand RecordCommand { get; set; }

        /// <summary>
        ///     Binding for the clear command for the GUI.
        /// </summary>
        public ICommand ClearCommand { get; set; }

        /// <summary>
        ///     Binding for the save command for the GUI.
        /// </summary>
        public ICommand SaveCommand { get; set; }

        /// <summary>
        ///     Binding for the load command for the GUI.
        /// </summary>
        public ICommand LoadCommand { get; set; }

        public ICommand AddCommand { get; set; }

        /// <summary>
        /// A command to stop the player from running navigator 
        /// throwing an error. 
        /// </summary>
        public ICommand ResetNavigatorCommand { get; set; }

        /// <summary>
        ///     Clears the waypoint list.
        /// </summary>
        private void ClearRoute()
        {
            Application.Current.Dispatcher.Invoke(() => Config.Instance.Route.Reset());
        }

        /// <summary>
        ///     Pauses and resumes the path recorder based on
        ///     its current state.
        /// </summary>
        private void Record()
        {            
            // Return when the user has not selected a process. 
            if (FFACE == null)
            {
                AppServices.InformUser("No process has been selected.");
                return;
            }

            if (FFACE.Player.Zone != Config.Instance.Route.Zone && Config.Instance.Route.Zone != Zone.Unknown)
            {
                AppServices.InformUser("Cannot record waypoints from a different zone.");
                return;
            }

            Config.Instance.Route.Zone = FFACE.Player.Zone;

            if (!PathRecorder.IsRecording)
            {
                PathRecorder.OnPositionAdded += PathRecorder_OnPositionAdded;
                PathRecorder.Start();
                RecordHeader = "Recording!";
            }
            else
            {
                PathRecorder.OnPositionAdded -= PathRecorder_OnPositionAdded;
                PathRecorder.Stop();
                RecordHeader = "Record";
            }
        }

        /// <summary>
        ///     Saves the route data.
        /// </summary>
        private void Save()
        {
            AppServices.InformUser(_settings.TrySave(Config.Instance.Route) ? "Path has been saved." : "Failed to save path.");
        }

        /// <summary>
        ///     Loads the route data.
        /// </summary>
        private void Load()
        {
            var route = _settings.TryLoad<Route>();

            var isRouteLoaded = route != null;

            if (isRouteLoaded)
            {
                Config.Instance.Route = route;
                AppServices.InformUser("Path has been loaded.");
            }
            else
            {
                AppServices.InformUser("Failed to load the path.");
            }
        }

        private void AddWaypoinnt()
        {
            // Return when the user has not selected a process. 
            if (FFACE == null)
            {
                AppServices.InformUser("No process has been selected.");
                return;
            }

            if (FFACE.Player.Zone != Config.Instance.Route.Zone && Config.Instance.Route.Zone != Zone.Unknown)
            {
                AppServices.InformUser("Cannot record waypoints from a different zone.");
                return;
            }

            Config.Instance.Route.Zone = FFACE.Player.Zone;

            var position = new Position
            {
                X = FFACE.Player.Position.X,
                Y = FFACE.Player.Position.Y,
                Z = FFACE.Player.Position.Z,
                H = FFACE.Player.Position.H
            };

            Application.Current.Dispatcher.Invoke(() => Route.Add(position));
            AppServices.InformUser("Waypoint added!");
        }

        /// <summary>
        /// Stops the player from running continously. 
        /// </summary>
        private void ResetNavigator()
        {
            // Return when the user has not selected a process. 
            if (FFACE == null)
            {
                AppServices.InformUser("No process has been selected.");
                return;
            }

            FFACE.Navigator.Reset();
            FFACE.Follow.Reset();
        }
    }
}