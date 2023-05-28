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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Castle.Components.DictionaryAdapter.Xml;
using EasyFarm.Classes;
using EasyFarm.Context;
using EasyFarm.UserSettings;
using EasyFarm.ViewModels;
using MahApps.Metro.Controls;
using MemoryAPI;
using static Recast;
using Player = EasyFarm.Classes.Player;

namespace EasyFarm.States
{
    /// <summary>
    ///     Handles the end of battle situation.
    ///     Fires off the end list, sets FightStart to true so other
    ///     lists can fire and replaces targets that are dead, null,
    ///     empty or invalid.
    /// </summary>
    public class EndState : BaseState
    {
        private Regex rAutoTarget = new Regex("Auto-targeting the");

        public override bool Check(IGameContext context)
        {
            // Prevent making the player stand up from resting.
            if (new RestState().Check(context)) return false;

            // Creature is unkillable and does not meets the
            // user's criteria for valid mobs defined in MobFilters.
            return !context.Target.IsValid;
        }

        /// <summary>
        ///     Force player when changing targets.
        /// </summary>
        public override void Enter(IGameContext context)
        {
            context.API.Navigator.Reset();
            context.API.Follow.Reset();
            context.Memory.NeedsTethered = false;

            // If tethering, check if the player is out of range of the route
            var config = context.Config;
            if (config.RouteTetherPlayer && config.Route.IsPathSet)
            {
                if (!config.Route.IsWithinDistance(context.API.Player.Position, config.WanderDistance))
                {
                    //This serves as a state cache, so that settarget doesnt run the measurements until needed (perf)
                    context.Memory.NeedsTethered = true;
                    LogViewModel.Write("Player has strayed too far from the path and needs to be tethered.");
                }
            }

            //Reset backup mode, if active
            if (context.Memory.IsBackingUp)
            {
                context.API.Windower.SendKeyUp(EliteMMO.API.Keys.NUMPAD2);
                context.Memory.IsBackingUp = false;
            }

            //Check if auto targetted
            if (context.Config.AllowAutoTarget)
            {
                var startTime = DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 2));
                var hasAutoTargtted = context.API.Chat.ChatEntries
                    .Where(
                        x => x.Timestamp > startTime &&
                        rAutoTarget.IsMatch(x.Text)
                    ).Any();
                if (hasAutoTargtted && context.API.Target.ID != context.Target.Id)
                {
                    var newTarget = context.Memory.UnitService.GetUnitById(context.API.Target.ID);
                    if (newTarget.IsValid)
                    {
                        context.Target = newTarget;
                        LogViewModel.Write("Auto targetting " + context.Target.Name + " : " + context.Target.Id);
                        return;
                    }
                }
            }

            //Disengage and begin again
            while (context.API.Player.Status == Status.Fighting) Player.Disengage(context.API);
        }

        public override void Run(IGameContext context)
        {
            // Execute moves.
            var usable = context.Config.BattleLists["End"].Actions
                .Where(x => ActionFilters.BuffingFilter(context.API, x));

            context.Memory.Executor.UseBuffingActions(usable);

            // Reset all usage data to begin a new battle.
            foreach (var action in context.Config.BattleLists.Actions) action.Usages = 0;
        }
    }
}