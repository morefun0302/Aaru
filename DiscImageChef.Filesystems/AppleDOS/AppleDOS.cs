// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : AppleDOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple DOS filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Constructors and common variables for the Apple DOS filesystem plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2018 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using Claunia.Encoding;
using DiscImageChef.CommonTypes;
using DiscImageChef.DiscImages;
using Schemas;
using Encoding = System.Text.Encoding;

namespace DiscImageChef.Filesystems.AppleDOS
{
    public partial class AppleDOS : IFilesystem
    {
        IMediaImage device;
        Encoding currentEncoding;
        bool debug;
        bool mounted;
        int sectorsPerTrack;
        ulong start;
        ulong totalFileEntries;
        bool track1UsedByFiles;
        bool track2UsedByFiles;
        int usedSectors;

        Vtoc vtoc;

        FileSystemType xmlFsType;
        public virtual FileSystemType XmlFsType => xmlFsType;
        public virtual Encoding Encoding => currentEncoding;
        public virtual string Name => "Apple DOS File System";
        public virtual Guid Id => new Guid("8658A1E9-B2E7-4BCC-9638-157A31B0A700\n");

        #region Caches
        /// <summary>Caches track/sector lists</summary>
        Dictionary<string, byte[]> extentCache;
        /// <summary>Caches files</summary>
        Dictionary<string, byte[]> fileCache;
        /// <summary>Caches catalog</summary>
        Dictionary<string, ushort> catalogCache;
        /// <summary>Caches file size</summary>
        Dictionary<string, int> fileSizeCache;
        /// <summary>Caches VTOC</summary>
        byte[] vtocBlocks;
        /// <summary>Caches catalog</summary>
        byte[] catalogBlocks;
        /// <summary>Caches boot code</summary>
        byte[] bootBlocks;
        /// <summary>Caches file type</summary>
        Dictionary<string, byte> fileTypeCache;
        /// <summary>Caches locked files</summary>
        List<string> lockedFiles;
        #endregion Caches
    }
}