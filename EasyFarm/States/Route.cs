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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;
using System.Linq;
using System.Numerics;
using EasyFarm.Classes;
using EasyFarm.Context;
using EasyFarm.UserSettings;
using MemoryAPI;
using MemoryAPI.Navigation;

namespace EasyFarm.States
{
    public class Route
    {
        private int _goal = -1;
        private List<Position> _nodes = new List<Position>();
        public ObservableCollection<Position> Waypoints = new ObservableCollection<Position>();

        public Zone Zone { get; set; }

        public bool IsPathSet => Waypoints.Any();

        public void ResetCurrentWaypoint()
        {
            _goal = -1;
        }

        public Position GetCurrentPosition(Position playerPosition)
        {
            _nodes = Waypoints.ToList();

            if (_nodes.Count < 2)
            {
                return _nodes.FirstOrDefault();
            }

            if (_goal == -1)
            {
                return GetNextPosition(playerPosition);
            }

            if (_goal >= _nodes.Count)
            {
                return null;
            }

            return _nodes[_goal];
        }

        public Position GetNextPosition(Position playerPosition)
        {
            // Refresh Node List
            _nodes = Waypoints.ToList();

            if (_nodes.Count < 2)
                return _nodes.FirstOrDefault();


            if (_goal == -1)
            {
                var closestNodes = _nodes.OrderBy(x => Distance(playerPosition, x));
                var closest = closestNodes.FirstOrDefault();
                var first = _nodes.FirstOrDefault();
                var last = _nodes.LastOrDefault();
                if (Distance(closest, first) > Distance(closest, last))
                {
                    Waypoints = new ObservableCollection<Position>(Waypoints.Reverse());
                    _nodes.Reverse();
                }

                _goal = _nodes.IndexOf(closest);
                if (Config.Instance.DebugRoutes)
                    EasyFarm.ViewModels.LogViewModel.Write("Navigating to waypoint (" + _goal + ") " + closest.ToString());

                return _nodes[_goal];
            }
            else if (_nodes.Count < 2)
            {
                _goal = 0;
                return _nodes[_goal];
            }
            else if (_nodes.Count < 3)
            {
                _goal = (_goal == 0) ? 1 : 0;
                return _nodes[_goal];
            }
            else
            {
                // Immediate increment to next goal node
                _goal++;

                // Check if goal is out of range to reset/reverse
                if (_goal >= _nodes.Count)
                {
                    // Reverse if Straight
                    if (Config.Instance.StraightRoute)
                    {
                        Waypoints = new ObservableCollection<Position>(Waypoints.Reverse());
                        _nodes.Reverse();
                    }
                    _goal = 0;
                }

                var node = _nodes[_goal];
                if (Config.Instance.DebugRoutes)
                    EasyFarm.ViewModels.LogViewModel.Write("Navigating to waypoint (" + _goal + ") " + node.ToString());

                return _nodes[_goal];
            }
        }

        private double Distance(Position one, Position other)
        {
            return Math.Sqrt(Math.Pow(one.X - other.X, 2) + Math.Pow(one.Z - other.Z, 2));
        }

        public void Reset()
        {
            Waypoints.Clear();
            Zone = Zone.Unknown;
            _goal = -1;
            _nodes.Clear();
        }

        public bool IsWithinDistance(Position position, double distance)
        {
            //No Route
            if (Waypoints.Count == 0)
                return false;

            //Single point
            Position p1 = Waypoints.First();
            if (Waypoints.Count == 1)
                return Distance(position, p1) <= distance;

            //Process path as line segments
            int cnt = Waypoints.Count;
            if (!Config.Instance.StraightRoute)
                cnt++; //loops

            for (int i = 1; i < cnt; i++)
            {
                //Project point to the path segment and measure the distance
                Position p2 = Waypoints[i % Waypoints.Count];
                Position projected = Position.ProjectClosestPoint(p1, p2, position);
                double projdist = Distance(position, projected);
                if (projdist <= distance)
                    return true; //early bail out
                p1 = p2;
            }

            return false;
        }

        public bool IsPathUnreachable(IGameContext context)
        {
            if (Zone != context.Player.Zone)
            {
                return false;
            }
            else
            {
                Position nextPos = GetNextPosition(context.API.Player.Position);
                if (nextPos != null)
                {
                    return (Distance(context.API.Player.Position, nextPos) < 0.5
                           || context.NavMesh.FindPathBetween(context.API.Player.Position, GetNextPosition(context.API.Player.Position)).Count > 0
                           );
                }
                else
                {
                    return false;
                }
            }
        }

        public static void NavigateTo(IGameContext context, Position pos)
        {
            float deltaX = pos.X - context.API.Player.Position.X;
            float deltaY = pos.Y - context.API.Player.Position.Y;
            float deltaZ = pos.Z - context.API.Player.Position.Z;
            context.API.Follow.SetFollowCoords(deltaX, deltaY, deltaZ);
        }
    }
}
