﻿// ///////////////////////////////////////////////////////////////////
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
using MemoryAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EasyFarm.UserSettings;
using MemoryAPI.Navigation;
using System.Diagnostics;
using System.Configuration;

namespace EasyFarm.Classes
{
    /// <summary>
    /// Helper functions for filtering units.
    /// </summary>
    public class UnitFilters : IUnitFilters
    {
        #region MOBFilter

        /// <summary>
        /// Returns true if a mob is attackable by the player based on the various settings in the
        /// Config class.
        /// </summary>
        /// <param name="fface"></param>
        /// <param name="mob"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool MobFilter(IMemoryAPI fface, IUnit mob, IConfig config)
        {
            // Function to use to filter surrounding mobs by. General Mob Filtering Criteria
            if (fface == null) return false;
            if (mob == null) return false;

            // Mob not active
            if (!mob.IsActive) return false;

            // INFO: fixes trying to attack dead mob problem. Mob is dead
            if (mob.IsDead) return false;

            // Mob not rendered on screen.
            if (!mob.IsRendered) return false;

            // Mob cannot be targetted
            if (!Config.Instance.UntargettableFilter && !mob.IsTargetable) return false;

            // Type is not mob
            if (!mob.NpcType.Equals(NpcType.Mob)) return false;

            // Target HP Checks Enabled. This is prioritized over aggro, since this is a niche check tht relies on other party members
            if (config.TargetLowerHealth != 0 || config.TargetUpperHealth != 0)
            {
                // Target Upper Health Check
                if (mob.HppCurrent > config.TargetUpperHealth) return false;

                // Target Lower Health Check
                if (mob.HppCurrent < config.TargetLowerHealth) return false;
            }

            // Always target enemies directly aggro on the player
            if (mob.HasAggroed) return true;

            // If party aggro is checked, then always target enemies directly aggro on any party member
            if (config.PartyAggroFilter && mob.HasAggroedParty) return true;

            // A target lockout has been applied. This should not take precendent over party aggro.
            if (mob.isLocked && config.PullFallback == PullFallbackType.Lock) return false;

            // Mob is out of range
            if (!(mob.Distance < config.DetectionDistance)) return false;

            // NM Huntinng
            if (Config.Instance.IsNMHunting)
            {
                if (mob.Name == Config.Instance.NotoriousMonsterName) return true;

                if (Config.Instance.PlaceholderIDs.Any())
                {
                    var placeholderIds = Config.Instance.PlaceholderIDs.Select(x => Convert.ToInt32(x, 16));
                    return placeholderIds.Where(x => mob.Id == x).Any();
                }
            }

            if (mob.IsPet) return false;

            // If any unit is within the wander distance then the
            if (config.RouteLimitTargets && config.Route.IsPathSet)
            {
                if (!config.Route.IsWithinDistance(mob.Position, config.WanderDistance)) return false;
            }

            // Mob too high out of reach.
            if (mob.YDifference > config.HeightThreshold) return false;

            // User Specific Filtering

            // Performs a case insensitve match on the mob's name. If any part of the mob's name is
            // in the ignored list, we will not attack it.
            if (MatchAny(mob.Name, config.IgnoredMobs,
                RegexOptions.IgnoreCase)) return false;

            // There is a target's list but the mob is not on it.
            if (!MatchAny(mob.Name, config.TargetedMobs, RegexOptions.IgnoreCase) &&
                config.TargetedMobs.Any())
                return false;

            // Kill the creature if it is claimed by party and party is checked.
            if (mob.PartyClaim && config.PartyFilter) return true;

            // Ignore if the aggro filter is off, and the creature is aggro
            if (!config.AggroFilter && mob.Status == Status.Fighting)
                return false;

            // Kill the creature if it's not claimed and unclaimed is checked.
            if (!mob.IsClaimed && config.UnclaimedFilter) return true;

            // Kill the creature if it's claimed and we we don't have claim but
            // claim is checked.
            //FIX: Temporary fix until player.serverid is fixed.
            if (mob.IsClaimed && config.ClaimedFilter) return true;

            // Kill only mobs that we have claim on. 
            return mob.ClaimedId == fface.PartyMember[0].ServerID;
        }

        /// <summary>
        /// Return the 2-D distance between the unit and a position. 
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="waypoint"></param>
        /// <returns></returns>
        private double Distance(IUnit mob, Position waypoint)
        {
            return Math.Sqrt(Math.Pow(waypoint.X - mob.PosX, 2) + Math.Pow(waypoint.Z - mob.PosZ, 2));
        }

        /// <summary>
        /// Check multiple patterns for a match.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="patterns"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool MatchAny(string input, IList<string> patterns, RegexOptions options)
        {
            return patterns
                .Select(pattern => new Regex(pattern, options))
                .Any(matcher => matcher.IsMatch(input));
        }

        #endregion MOBFilter
    }
}