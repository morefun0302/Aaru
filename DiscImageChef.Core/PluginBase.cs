// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : PluginBase.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Class to hold all installed plugins.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2018 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DiscImageChef.Console;
using DiscImageChef.DiscImages;
using DiscImageChef.Filesystems;
using DiscImageChef.Partitions;

namespace DiscImageChef.Core
{
    /// <summary>
    ///     Contain all plugins (filesystem, partition and image)
    /// </summary>
    public class PluginBase
    {
        /// <summary>
        ///     List of all media image plugins
        /// </summary>
        public SortedDictionary<string, IMediaImage> ImagePluginsList;
        /// <summary>
        ///     List of all partition plugins
        /// </summary>
        public SortedDictionary<string, IPartition> PartPluginsList;
        /// <summary>
        ///     List of all filesystem plugins
        /// </summary>
        public SortedDictionary<string, IFilesystem> PluginsList;

        /// <summary>
        ///     Initializes the plugins lists
        /// </summary>
        public PluginBase()
        {
            PluginsList = new SortedDictionary<string, IFilesystem>();
            PartPluginsList = new SortedDictionary<string, IPartition>();
            ImagePluginsList = new SortedDictionary<string, IMediaImage>();
        }

        /// <summary>
        ///     Fills the plugins lists
        /// </summary>
        public void RegisterAllPlugins()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(IMediaImage));

            foreach(Type type in assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IMediaImage))))
                try
                {
                    IMediaImage plugin = (IMediaImage)type.GetConstructor(Type.EmptyTypes)?.Invoke(new object[] { });
                    RegisterImagePlugin(plugin);
                }
                catch(Exception exception) { DicConsole.ErrorWriteLine("Exception {0}", exception); }

            assembly = Assembly.GetAssembly(typeof(IPartition));

            foreach(Type type in assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IPartition))))
                try
                {
                    IPartition plugin =
                        (IPartition)type.GetConstructor(Type.EmptyTypes)?.Invoke(new object[] { });
                    RegisterPartPlugin(plugin);
                }
                catch(Exception exception) { DicConsole.ErrorWriteLine("Exception {0}", exception); }

            assembly = Assembly.GetAssembly(typeof(IFilesystem));

            foreach(Type type in assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IFilesystem))))
                try
                {
                    IFilesystem plugin = (IFilesystem)type.GetConstructor(Type.EmptyTypes)?.Invoke(new object[] { });
                    RegisterPlugin(plugin);
                }
                catch(Exception exception) { DicConsole.ErrorWriteLine("Exception {0}", exception); }
        }

        void RegisterImagePlugin(IMediaImage plugin)
        {
            if(!ImagePluginsList.ContainsKey(plugin.Name.ToLower()))
                ImagePluginsList.Add(plugin.Name.ToLower(), plugin);
        }

        void RegisterPlugin(IFilesystem plugin)
        {
            if(!PluginsList.ContainsKey(plugin.Name.ToLower())) PluginsList.Add(plugin.Name.ToLower(), plugin);
        }

        void RegisterPartPlugin(IPartition partplugin)
        {
            if(!PartPluginsList.ContainsKey(partplugin.Name.ToLower()))
                PartPluginsList.Add(partplugin.Name.ToLower(), partplugin);
        }
    }
}