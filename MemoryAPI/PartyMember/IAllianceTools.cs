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
using MemoryAPI.Navigation;

namespace MemoryAPI
{
    public interface IAllianceTools
    {
        int AllianceLeaderId { get; }

        int Party0LeaderId { get; }

        int Party1LeaderId { get; } 

        int Party2LeaderId { get; }

        byte Party0Visible { get; }

        byte Party1Visible { get; }

        byte Party2Visible { get; }

        byte Party0Count { get; }

        byte Party1Count { get; }

        byte Party2Count { get; }

        byte Invited { get; }
    }
}