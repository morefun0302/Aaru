﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : AODOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commodore file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the AO-DOS file system and shows information.
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.DiscImages;
using Schemas;

namespace DiscImageChef.Filesystems
{
    // Information has been extracted looking at available disk images
    // This may be missing fields, or not, I don't know russian so any help is appreciated
    public class AODOS : IFilesystem
    {
        readonly byte[] AODOSIdentifier = {0x20, 0x41, 0x4F, 0x2D, 0x44, 0x4F, 0x53, 0x20};
        Encoding currentEncoding;
        FileSystemType xmlFsType;
        public virtual FileSystemType XmlFsType => xmlFsType;
        public virtual string Name => "Alexander Osipov DOS file system";
        public virtual Guid Id => new Guid("668E5039-9DDD-442A-BE1B-A315D6E38E26");
        public virtual Encoding Encoding => currentEncoding;

        public virtual bool Identify(IMediaImage imagePlugin, Partition partition)
        {
            // Does AO-DOS support hard disks?
            if(partition.Start > 0) return false;

            // How is it really?
            if(imagePlugin.Info.SectorSize != 512) return false;

            // Does AO-DOS support any other kind of disk?
            if(imagePlugin.Info.Sectors != 800 && imagePlugin.Info.Sectors != 1600) return false;

            byte[] sector = imagePlugin.ReadSector(0);
            AODOS_BootBlock bb = new AODOS_BootBlock();
            IntPtr bbPtr = Marshal.AllocHGlobal(Marshal.SizeOf(bb));
            Marshal.Copy(sector, 0, bbPtr, Marshal.SizeOf(bb));
            bb = (AODOS_BootBlock)Marshal.PtrToStructure(bbPtr, typeof(AODOS_BootBlock));
            Marshal.FreeHGlobal(bbPtr);

            return bb.identifier.SequenceEqual(AODOSIdentifier);
        }

        public virtual void GetInformation(IMediaImage imagePlugin, Partition partition, out string information, Encoding encoding)
        {
            currentEncoding = Encoding.GetEncoding("koi8-r");
            byte[] sector = imagePlugin.ReadSector(0);
            AODOS_BootBlock bb = new AODOS_BootBlock();
            IntPtr bbPtr = Marshal.AllocHGlobal(Marshal.SizeOf(bb));
            Marshal.Copy(sector, 0, bbPtr, Marshal.SizeOf(bb));
            bb = (AODOS_BootBlock)Marshal.PtrToStructure(bbPtr, typeof(AODOS_BootBlock));
            Marshal.FreeHGlobal(bbPtr);

            StringBuilder sbInformation = new StringBuilder();

            sbInformation.AppendLine("Alexander Osipov DOS file system");

            xmlFsType = new FileSystemType
            {
                Type = "Alexander Osipov DOS file system",
                Clusters = (long)imagePlugin.Info.Sectors,
                ClusterSize = (int)imagePlugin.Info.SectorSize,
                Files = bb.files,
                FilesSpecified = true,
                FreeClusters = (long)(imagePlugin.Info.Sectors - bb.usedSectors),
                FreeClustersSpecified = true,
                VolumeName = StringHandlers.SpacePaddedToString(bb.volumeLabel, currentEncoding),
                Bootable = true
            };

            sbInformation.AppendFormat("{0} files on volume", bb.files).AppendLine();
            sbInformation.AppendFormat("{0} used sectors on volume", bb.usedSectors).AppendLine();
            sbInformation.AppendFormat("Disk name: {0}", StringHandlers.CToString(bb.volumeLabel, currentEncoding))
                         .AppendLine();

            information = sbInformation.ToString();
        }

        public virtual Errno Mount(IMediaImage imagePlugin, Partition partition, Encoding encoding, bool debug)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno Unmount()
        {
            return Errno.NotImplemented;
        }

        public virtual Errno MapBlock(string path, long fileBlock, ref long deviceBlock)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno GetAttributes(string path, ref FileAttributes attributes)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno ListXAttr(string path, ref List<string> xattrs)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno GetXattr(string path, string xattr, ref byte[] buf)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno Read(string path, long offset, long size, ref byte[] buf)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno ReadDir(string path, ref List<string> contents)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno StatFs(ref FileSystemInfo stat)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno Stat(string path, ref FileEntryInfo stat)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno ReadLink(string path, ref string dest)
        {
            return Errno.NotImplemented;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct AODOS_BootBlock
        {
            /// <summary>
            ///     A NOP opcode
            /// </summary>
            public byte nop;
            /// <summary>
            ///     A branch to real bootloader
            /// </summary>
            public ushort branch;
            /// <summary>
            ///     Unused
            /// </summary>
            public byte unused;
            /// <summary>
            ///     " AO-DOS "
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public byte[] identifier;
            /// <summary>
            ///     Volume label
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)] public byte[] volumeLabel;
            /// <summary>
            ///     How many files are present in disk
            /// </summary>
            public ushort files;
            /// <summary>
            ///     How many sectors are used
            /// </summary>
            public ushort usedSectors;
        }
    }
}